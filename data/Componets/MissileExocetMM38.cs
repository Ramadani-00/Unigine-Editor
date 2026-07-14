// // ============================== BETA 3 ========================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unigine;

[Component(PropertyGuid = "4323ce7db2d375c14f4264f62323cbcb8cf7c54e")]
public class MissileExocetMM40 : Component
{
    [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama dengan ID Kapal di Delphi agar tidak tertukar saat menembak, misal KRI_RE_MARTADINATA")]
    public string ownerShipID = "KRI_RE_MARTADINATA";
    [Parameter(Title = "Kecepatan (m/s)")]
    public float speed = 315.0f;
    [Parameter(Title = "Kecepatan Belok")]
    public float turnSpeed = 3.0f;
    [Parameter(Title = "Tinggi Sea Skimming (m)")]
    public float seaSkimmingHeight = 2.5f;
    [Parameter(Title = "Tinggi Melambung Awal (m)")]
    public float launchAscentHeight = 15.0f;
    [Parameter(Title = "Durasi Terbang Lurus (Detik)", Tooltip = "Berapa detik misil dipaksa lurus searah tabung sebelum mulai berbelok/menukik")]
    public float straightFlightDuration = 1.0f;
    [Parameter(Title = "Target (KRI Golok)")]
    public Node targetNode = null;
    [Parameter(Title = "Kapal Sendiri (KRI Belati)")]
    public Node selfShipNode = null;
    [Parameter(Title = "Nomor Tabung Bind (1 sampai 8)")]
    public string keyBinding = "1";
    [Parameter(Title = "Prefab Ledakan Target")]
    public Node explosionPrefab = null;
    [Parameter(Title = "Prefab Ledakan Air")]
    public Node waterExplosionPrefab = null;
    [Parameter(Title = "VFX Motor Roket (Terbang)")]
    public Node rocketMotorVFX = null;
    [Parameter(Title = "Asap Awal (Smoke Wisp)")]
    public Node smokeWisp = null;
    [Parameter(Title = "Asap Ekor Terbang (Smoke Missile)")]
    public Node smokeMissile = null;
    [Parameter(Title = "Efek Cahaya Api (Fire Glow)")]
    public Node fireGlow = null;
    [Parameter(Title = "Audio Meluncur (Snd_meluncur)")]
    public Node sndMeluncurNode = null;
    [Parameter(Title = "Audio Meledak (Snd_meledak)")]
    public Node sndMeledakNode = null;
    [Parameter(Title = "Jeda Luncur (Detik)")]
    public float launchDelay = 1.0f;

    private bool isPreparing = false;
    private float prepareTimer = 0.0f;
    private bool isLaunched = false;
    private bool isExploded = false;
    private float launchTimer = 0.0f;
    private Node initialParent;
    private dvec3 initialLocalPosition;
    private quat initialLocalRotation;
    private SoundSource sndMeluncur = null;
    private SoundSource sndMeledak = null;
    private enum FlightPhase { StraightRun, GuidanceHoming }
    private FlightPhase currentPhase = FlightPhase.StraightRun;
    private bool netTriggerLaunch = false;
    private bool netTriggerReload = false;
    private readonly object lockObject = new object();

    protected override void OnReady()
    {
        initialParent = node.Parent;
        initialLocalPosition = node.Position;
        initialLocalRotation = node.GetRotation();

        if (sndMeluncurNode != null) sndMeluncur = sndMeluncurNode as SoundSource;
        if (sndMeledakNode != null) sndMeledak = sndMeledakNode as SoundSource;

        ResetAllEffects();
    }

    public void ProcessNetworkData(string rawData)
    {
        string[] parts = rawData.Split('|');
        if (parts.Length == 2 && parts[0].Trim().ToUpper() == ownerShipID.ToUpper())
        {
            string command = parts[1].Trim().ToUpper();

            if (command.Contains(":"))
            {
                string[] cmdValue = command.Split(':');
                if (cmdValue.Length == 2)
                {
                    string cmdName = cmdValue[0].Trim().ToUpper();
                    string targetTabung = cmdValue[1].Trim().ToUpper();

                    if (targetTabung == keyBinding.Trim().ToUpper())
                    {
                        lock (lockObject)
                        {
                            if (cmdName == "EXOCET_TEMBAK")
                            {
                                netTriggerLaunch = true;
                            }
                            else if (cmdName == "EXOCET_RELOAD")
                            {
                                netTriggerReload = true;
                            }
                        }
                    }
                }
            }
        }
    }

    void Update()
    {
        float dt = Game.IFps;

        bool localNetworkLaunch = false;
        bool localNetworkReload = false;

        lock (lockObject)
        {
            localNetworkLaunch = netTriggerLaunch;
            localNetworkReload = netTriggerReload;
            netTriggerLaunch = false;
            netTriggerReload = false;
        }

        if (localNetworkReload)
        {
            ReloadMissile();
            return;
        }

        if (!isLaunched && !isExploded)
        {
            if (localNetworkLaunch && !isPreparing) StartIgnition();

            if (isPreparing)
            {
                prepareTimer += dt;
                if (prepareTimer >= launchDelay) Launch();
            }
        }

        if (isLaunched && !isExploded)
        {
            launchTimer += dt;

            // Evaluasi perpindahan otomatis dari fase terbang lurus ke fase pelacakan radar
            if (currentPhase == FlightPhase.StraightRun && launchTimer >= straightFlightDuration)
            {
                currentPhase = FlightPhase.GuidanceHoming;
                Log.Message($"[EXOCET] Safe distance reached! Activating booster and guidance radar lock.\n");
            }

            MoveMissile();
            CheckCollision();
        }
    }

    private void StartIgnition()
    {
        isPreparing = true;
        prepareTimer = 0.0f;

        dvec3 worldPos = node.WorldPosition;
        quat worldRot = node.GetWorldRotation();
        node.Parent = null;
        node.WorldPosition = worldPos;
        node.SetWorldRotation(worldRot);

        if (smokeWisp != null)
        {
            smokeWisp.Enabled = true;
            SetEmitterState(smokeWisp, true);
        }
        Log.Message($"[IGNITION] Exocet Tabung {keyBinding} warming up via Delphi Command...\n");
    }

    private void Launch()
    {
        isPreparing = false;
        isLaunched = true;
        launchTimer = 0.0f;
        currentPhase = FlightPhase.StraightRun;

        if (rocketMotorVFX != null) rocketMotorVFX.Enabled = true;
        if (smokeMissile != null)
        {
            smokeMissile.Enabled = true;
            SetEmitterState(smokeMissile, true);
        }
        if (fireGlow != null) fireGlow.Enabled = true;

        StopVFXSmoothly(smokeWisp);

        if (sndMeluncur != null)
        {
            sndMeluncur.Enabled = true;
            sndMeluncur.Play();
        }

        Log.Message($"[EXOCET] MM40 Block 3: Tabung {keyBinding} Launched successfully!\n");
    }

    private void MoveMissile()
    {
        float dt = Game.IFps;
        dvec3 currentPos = node.WorldPosition;
        vec3 targetDir;

        if (currentPhase == FlightPhase.StraightRun)
        {
            targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
            node.WorldPosition = currentPos + (dvec3)(targetDir * speed * dt);
            node.SetWorldDirection(targetDir, vec3.UP, MathLib.AXIS.Y);
            return;
        }

        if (targetNode != null)
        {
            vec3 targetPos = (vec3)targetNode.WorldPosition;
            float desiredHeight = seaSkimmingHeight;
            vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
            targetDir = MathLib.Normalize(adjustedTargetPos - (vec3)currentPos);
        }
        else
        {
            targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
        }

        vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
        vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
        smoothDir = MathLib.Normalize(smoothDir);

        node.WorldPosition = currentPos + (dvec3)(smoothDir * speed * dt);
        node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
    }

    private void CheckCollision()
    {
        if (targetNode != null)
        {
            float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
            if (dist < 6.5f) { Explode(false); return; }
        }

        if (selfShipNode != null && launchTimer > 1.2f)
        {
            float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
            if (distToSelf < 10.0f) { Explode(false); return; }
        }

        if (node.WorldPosition.z < -0.5f) Explode(true);
    }

    private void Explode(bool hitWater)
    {
        isExploded = true;
        Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

        if (effectPrefab != null)
        {
            Node instanceExplosion = effectPrefab.Clone();
            instanceExplosion.Parent = null;
            instanceExplosion.WorldPosition = node.WorldPosition;
            instanceExplosion.Enabled = true;
        }

        if (!hitWater && targetNode != null)
        {
            ShipDamageHandler damageHandler = targetNode.GetComponent<ShipDamageHandler>();

            if (damageHandler != null)
            {
                damageHandler.ActivateDamage();
            }
            else
            {
                Log.Warning($"[EXOCET] Menghantam {targetNode.Name}, tetapi komponen ShipDamageHandler tidak ditemukan pada objek target!\n");
            }
        }

        if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
        if (fireGlow != null) fireGlow.Enabled = false;
        StopVFXSmoothly(smokeWisp);

        // PERBAIKAN: Kunci koordinat dunia asap sebelum dilepas agar sisa asap tidak hilang mendadak masuk ke lambung kapal
        if (smokeMissile != null)
        {
            dvec3 currentSmokeWorldPos = smokeMissile.WorldPosition;
            quat currentSmokeWorldRot = smokeMissile.GetWorldRotation();

            smokeMissile.Parent = null;
            smokeMissile.WorldPosition = currentSmokeWorldPos;
            smokeMissile.SetWorldRotation(currentSmokeWorldRot);
            ObjectParticles particles = smokeMissile as ObjectParticles;
            if (particles == null && smokeMissile.NumChildren > 0) particles = smokeMissile.GetChild(0) as ObjectParticles;
            if (particles != null) particles.EmitterEnabled = false;
        }

        if (sndMeluncur != null) sndMeluncur.Stop();
        if (sndMeledak != null)
        {
            sndMeledakNode.Parent = null;
            sndMeledakNode.WorldPosition = node.WorldPosition;
            sndMeledak.Enabled = true;
            sndMeledak.Play();
        }

        Unigine.Object visualObject = node as Unigine.Object;
        if (visualObject != null)
        {
            for (int i = 0; i < visualObject.NumSurfaces; i++)
            {
                visualObject.SetEnabled(false, i);
            }
        }

        else
        {
            node.Enabled = false;
        }
    }

    public void ReloadMissile()
    {
        isLaunched = false;
        isExploded = false;
        isPreparing = false;
        launchTimer = 0.0f;
        prepareTimer = 0.0f;
        currentPhase = FlightPhase.StraightRun;
        if (sndMeledakNode != null)
        {
            sndMeledakNode.Parent = node;
            sndMeledakNode.Position = vec3.ZERO;
        }

        if (smokeMissile != null)
        {
            smokeMissile.Parent = node;
            smokeMissile.Position = vec3.ZERO;
        }

        node.Parent = initialParent;
        node.Position = (vec3)initialLocalPosition;
        node.SetRotation(initialLocalRotation);
        node.Enabled = true;

        Unigine.Object visualObject = node as Unigine.Object;
        if (visualObject != null)
        {
            for (int i = 0; i < visualObject.NumSurfaces; i++)
            {
                visualObject.SetEnabled(true, i);
            }
        }

        ResetAllEffects();
        Log.Message($"Exocet System: Tabung {keyBinding} Reloaded via Delphi Command.\n");
    }

    private void ResetAllEffects()
    {
        if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
        if (fireGlow != null) fireGlow.Enabled = false;
        if (smokeWisp != null)
        {
            smokeWisp.Enabled = false;
            SetEmitterState(smokeWisp, true);
        }

        if (smokeMissile != null)
        {
            smokeMissile.Enabled = false;
            SetEmitterState(smokeMissile, true);
        }

        if (sndMeluncur != null)
        {
            sndMeluncur.Stop();
            sndMeluncur.Enabled = false;
        }

        if (sndMeledak != null)
        {
            sndMeledak.Stop();
            sndMeledak.Enabled = false;
        }
    }

    private void StopVFXSmoothly(Node vfxNode)
    {
        if (vfxNode == null) return;
        ObjectParticles particles = vfxNode as ObjectParticles;
        if (particles == null && vfxNode.NumChildren > 0) particles = vfxNode.GetChild(0) as ObjectParticles;
        if (particles != null) particles.EmitterEnabled = false;
        else vfxNode.Enabled = false;
    }

    private void SetEmitterState(Node vfxNode, bool state)
    {
        if (vfxNode == null) return;
        ObjectParticles particles = vfxNode as ObjectParticles;
        if (particles == null && vfxNode.NumChildren > 0) particles = vfxNode.GetChild(0) as ObjectParticles;
        if (particles != null) particles.EmitterEnabled = state;
    }
}


// // ========================== Beta 2 ==========================

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using Unigine;

// [Component(PropertyGuid = "4323ce7db2d375c14f4264f62323cbcb8cf7c54e")]
// public class MissileExocetMM40 : Component
// {
//     [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama dengan ID Kapal di Delphi agar tidak tertukar saat menembak, misal KRI_RE_MARTADINATA")]
//     public string ownerShipID = "KRI_RE_MARTADINATA";

//     [Parameter(Title = "Kecepatan (m/s)")]
//     public float speed = 315.0f;
//     [Parameter(Title = "Kecepatan Belok")]
//     public float turnSpeed = 4.5f;
//     [Parameter(Title = "Tinggi Sea Skimming (m)")]
//     public float seaSkimmingHeight = 2.5f;
//     [Parameter(Title = "Tinggi Melambung Awal (m)")]
//     public float launchAscentHeight = 15.0f;

//     [Parameter(Title = "Target (KRI Golok)")]
//     public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")]
//     public Node selfShipNode = null;

//     [Parameter(Title = "Nomor Tabung Bind (1 sampai 8)")]
//     public string keyBinding = "1";

//     [Parameter(Title = "Prefab Ledakan Target")]
//     public Node explosionPrefab = null;
//     [Parameter(Title = "Prefab Ledakan Air")]
//     public Node waterExplosionPrefab = null;

//     // --- PARAMETER VFX ---
//     [Parameter(Title = "VFX Motor Roket (Terbang)")]
//     public Node rocketMotorVFX = null;
//     [Parameter(Title = "Asap Awal (Smoke Wisp)")]
//     public Node smokeWisp = null;
//     [Parameter(Title = "Asap Ekor Terbang (Smoke Missile)")]
//     public Node smokeMissile = null;
//     [Parameter(Title = "Efek Cahaya Api (Fire Glow)")]
//     public Node fireGlow = null;

//     // --- PARAMETER AUDIO ---
//     [Parameter(Title = "Audio Meluncur (Snd_meluncur)")]
//     public Node sndMeluncurNode = null;
//     [Parameter(Title = "Audio Meledak (Snd_meledak)")]
//     public Node sndMeledakNode = null;

//     [Parameter(Title = "Jeda Luncur (Detik)")]
//     public float launchDelay = 2.0f;

//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     private SoundSource sndMeluncur = null;
//     private SoundSource sndMeledak = null;

//     // Variabel pemicu dari network thread TCP Delphi
//     private bool netTriggerLaunch = false;
//     private bool netTriggerReload = false;
//     private readonly object lockObject = new object();

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         if (sndMeluncurNode != null) sndMeluncur = sndMeluncurNode as SoundSource;
//         if (sndMeledakNode != null) sndMeledak = sndMeledakNode as SoundSource;

//         ResetAllEffects();
//     }

//     // PARSING PERINTAH TCP JARINGAN UNTUK SINKRONISASI KONTROL DELPHI
//     public void ProcessNetworkData(string rawData)
//     {
//         // Pisahkan data berdasarkan karakter '|' (Format: ID_KAPAL|PERINTAH)
//         string[] parts = rawData.Split('|');
//         if (parts.Length == 2 && parts[0].Trim().ToUpper() == ownerShipID.ToUpper())
//         {
//             // Ambil bagian perintahnya saja (Contoh: EXOCET_TEMBAK:1)
//             string command = parts[1].Trim().ToUpper();

//             if (command.Contains(":"))
//             {
//                 // Pisahkan nama perintah dengan nomor tabungnya
//                 string[] cmdValue = command.Split(':');
//                 if (cmdValue.Length == 2)
//                 {
//                     // Paksa keduanya menggunakan ToUpper() agar sinkron dengan ShipTCPManager
//                     string cmdName = cmdValue[0].Trim().ToUpper();
//                     string targetTabung = cmdValue[1].Trim().ToUpper();

//                     // Pastikan nomor tabung dari Delphi cocok dengan keyBinding misil ini
//                     if (targetTabung == keyBinding.Trim().ToUpper())
//                     {
//                         lock (lockObject)
//                         {
//                             if (cmdName == "EXOCET_TEMBAK")
//                             {
//                                 netTriggerLaunch = true;
//                             }
//                             else if (cmdName == "EXOCET_RELOAD")
//                             {
//                                 netTriggerReload = true;
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//     }


//     void Update()
//     {
//         float dt = Game.IFps;

//         // Ambil status pemicu jaringan secara aman (thread-safe)
//         bool localNetworkLaunch = false;
//         bool localNetworkReload = false;

//         lock (lockObject)
//         {
//             localNetworkLaunch = netTriggerLaunch;
//             localNetworkReload = netTriggerReload;
//             netTriggerLaunch = false; // Reset trigger seketika
//             netTriggerReload = false;
//         }

//         // Jalankan reload otomatis jika ada perintah masuk dari Delphi
//         if (localNetworkReload) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             // Mengganti input keyboard lama dengan sinyal localNetworkLaunch dari Delphi
//             if (localNetworkLaunch && !isPreparing) StartIgnition();

//             if (isPreparing)
//             {
//                 prepareTimer += dt;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += dt;
//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;

//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = true;
//             SetEmitterState(smokeWisp, true);
//         }
//         Log.Message($"[IGNITION] Exocet Tabung {keyBinding} warming up via Delphi Command...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         isLaunched = true;
//         launchTimer = 0.0f;

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = true;
//         if (smokeMissile != null)
//         {
//             smokeMissile.Enabled = true;
//             SetEmitterState(smokeMissile, true);
//         }
//         if (fireGlow != null) fireGlow.Enabled = true;

//         StopVFXSmoothly(smokeWisp);

//         if (sndMeluncur != null)
//         {
//             sndMeluncur.Enabled = true;
//             sndMeluncur.Play();
//         }

//         Log.Message($"[EXOCET] MM40 Block 3: Tabung {keyBinding} Launched successfully!\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         dvec3 currentPos = node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float desiredHeight = (launchTimer < 1.5f) ? launchAscentHeight : seaSkimmingHeight;
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - (vec3)currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//         smoothDir = MathLib.Normalize(smoothDir);

//         node.WorldPosition = currentPos + (dvec3)(smoothDir * speed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 10.0f) { Explode(false); return; }
//         }

//         if (selfShipNode != null && launchTimer > 1.2f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 10.0f) { Explode(false); return; }
//         }

//         if (node.WorldPosition.z < -0.5f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
//         if (fireGlow != null) fireGlow.Enabled = false;
//         StopVFXSmoothly(smokeWisp);

//         if (smokeMissile != null)
//         {
//             smokeMissile.Parent = null;
//             ObjectParticles particles = smokeMissile as ObjectParticles;
//             if (particles == null && smokeMissile.NumChildren > 0)
//                 particles = smokeMissile.GetChild(0) as ObjectParticles;

//             if (particles != null) particles.EmitterEnabled = false;
//         }

//         if (sndMeluncur != null) sndMeluncur.Stop();

//         if (sndMeledak != null)
//         {
//             sndMeledakNode.Parent = null;
//             sndMeledakNode.WorldPosition = node.WorldPosition;
//             sndMeledak.Enabled = true;
//             sndMeledak.Play();
//         }

//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isPreparing = false;
//         launchTimer = 0.0f;
//         prepareTimer = 0.0f;

//         if (sndMeledakNode != null)
//         {
//             sndMeledakNode.Parent = node;
//             sndMeledakNode.Position = vec3.ZERO;
//         }

//         if (smokeMissile != null)
//         {
//             smokeMissile.Parent = node;
//             smokeMissile.Position = vec3.ZERO;
//         }

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         ResetAllEffects();
//         Log.Message($"Exocet System: Tabung {keyBinding} Reloaded via Delphi Command.\n");

//     }

//     private void ResetAllEffects()
//     {
//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
//         if (fireGlow != null) fireGlow.Enabled = false;

//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = false;
//             SetEmitterState(smokeWisp, true);
//         }
//         if (smokeMissile != null)
//         {
//             smokeMissile.Enabled = false;
//             SetEmitterState(smokeMissile, true);
//         }

//         if (sndMeluncur != null) { sndMeluncur.Stop(); sndMeluncur.Enabled = false; }
//         if (sndMeledak != null) { sndMeledak.Stop(); sndMeledak.Enabled = false; }
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;

//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = false;
//         else vfxNode.Enabled = false;
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }
// }


// ========================== Beta 1 ==========================

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "4323ce7db2d375c14f4264f62323cbcb8cf7c54e")]
// public class MissileExocetMM40 : Component
// {
//     [Parameter(Title = "Kecepatan (m/s)")]
//     public float speed = 120.0f;
//     [Parameter(Title = "Kecepatan Belok")]
//     public float turnSpeed = 4.5f;
//     [Parameter(Title = "Tinggi Sea Skimming (m)")]
//     public float seaSkimmingHeight = 2.5f;
//     [Parameter(Title = "Tinggi Melambung Awal (m)")]
//     public float launchAscentHeight = 15.0f;

//     [Parameter(Title = "Target (KRI Golok)")]
//     public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")]
//     public Node selfShipNode = null;

//     [Parameter(Title = "Tombol Tembak (7 atau 8)")]
//     public string keyBinding = "7";

//     [Parameter(Title = "Prefab Ledakan Target")]
//     public Node explosionPrefab = null;
//     [Parameter(Title = "Prefab Ledakan Air")]
//     public Node waterExplosionPrefab = null;

//     // --- PARAMETER VFX ---
//     [Parameter(Title = "VFX Motor Roket (Terbang)")]
//     public Node rocketMotorVFX = null;
//     [Parameter(Title = "Asap Awal (Smoke Wisp)")]
//     public Node smokeWisp = null;
//     [Parameter(Title = "Asap Ekor Terbang (Smoke Missile)")]
//     public Node smokeMissile = null;
//     [Parameter(Title = "Efek Cahaya Api (Fire Glow)")]
//     public Node fireGlow = null;

//     // --- PARAMETER AUDIO ---
//     [Parameter(Title = "Audio Meluncur (Snd_meluncur)")]
//     public Node sndMeluncurNode = null;
//     [Parameter(Title = "Audio Meledak (Snd_meledak)")]
//     public Node sndMeledakNode = null;

//     [Parameter(Title = "Jeda Luncur (Detik)")]
//     public float launchDelay = 8f;

//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     private SoundSource sndMeluncur = null;
//     private SoundSource sndMeledak = null;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         if (sndMeluncurNode != null) sndMeluncur = sndMeluncurNode as SoundSource;
//         if (sndMeledakNode != null) sndMeledak = sndMeledakNode as SoundSource;

//         ResetAllEffects();
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput() && !isPreparing) StartIgnition();

//             if (isPreparing)
//             {
//                 prepareTimer += Game.IFps;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;
//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;

//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = true;
//             SetEmitterState(smokeWisp, true);
//         }
//         Log.Message($"[IGNITION] Missile {keyBinding} warming up...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         isLaunched = true;
//         launchTimer = 0.0f;

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = true;
//         if (smokeMissile != null)
//         {
//             smokeMissile.Enabled = true;
//             SetEmitterState(smokeMissile, true);
//         }
//         if (fireGlow != null) fireGlow.Enabled = true;

//         StopVFXSmoothly(smokeWisp);

//         if (sndMeluncur != null)
//         {
//             sndMeluncur.Enabled = true;
//             sndMeluncur.Play();
//         }

//         Log.Message($"[EXOCET] MM40 Block 3: Missile {keyBinding} Launched!\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         dvec3 currentPos = node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float desiredHeight = (launchTimer < 1.5f) ? launchAscentHeight : seaSkimmingHeight;
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - (vec3)currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//         smoothDir = MathLib.Normalize(smoothDir);

//         node.WorldPosition = currentPos + (dvec3)(smoothDir * speed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 10.0f) { Explode(false); return; }
//         }

//         if (selfShipNode != null && launchTimer > 1.2f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 10.0f) { Explode(false); return; }
//         }

//         if (node.WorldPosition.z < -0.5f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         // Matikan efek api instan karena misil hancur
//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
//         if (fireGlow != null) fireGlow.Enabled = false;
//         StopVFXSmoothly(smokeWisp);

//         // --- PERBAIKAN: MEMBUAT SMOKE_MISSILE MEMUDAR ALAMI ---
//         if (smokeMissile != null)
//         {
//             // Lepas dari misil agar posisinya tertinggal di titik ledakan world koordinat
//             smokeMissile.Parent = null;

//             // Matikan emisi partikel baru saja, partikel lama di udara akan memudar alami mengikuti sistem Unigine
//             ObjectParticles particles = smokeMissile as ObjectParticles;
//             if (particles == null && smokeMissile.NumChildren > 0)
//                 particles = smokeMissile.GetChild(0) as ObjectParticles;

//             if (particles != null) particles.EmitterEnabled = false;
//         }

//         if (sndMeluncur != null) sndMeluncur.Stop();

//         if (sndMeledak != null)
//         {
//             sndMeledakNode.Parent = null;
//             sndMeledakNode.WorldPosition = node.WorldPosition;
//             sndMeledak.Enabled = true;
//             sndMeledak.Play();
//         }

//         // Pindahkan misil jauh ke bawah map (Asap tetap aman di tempatnya dan memudar secara halus)
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isPreparing = false;
//         launchTimer = 0.0f;
//         prepareTimer = 0.0f;

//         // Kembalikan node suara meledak ke dalam hierarki misil
//         if (sndMeledakNode != null)
//         {
//             sndMeledakNode.Parent = node;
//             sndMeledakNode.Position = vec3.ZERO;
//         }

//         // Kembalikan node asap terbang (smokeMissile) ke dalam hierarki misil saat reload
//         if (smokeMissile != null)
//         {
//             smokeMissile.Parent = node;
//             // Letakkan kembali ke posisi lokal semula (misal di bagian ekor misil)
//             smokeMissile.Position = vec3.ZERO;
//         }

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         ResetAllEffects();
//         Log.Message($"Exocet System: Missile {keyBinding} Reloaded.\n");
//     }

//     private void ResetAllEffects()
//     {
//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
//         if (fireGlow != null) fireGlow.Enabled = false;

//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = false;
//             SetEmitterState(smokeWisp, true);
//         }
//         if (smokeMissile != null)
//         {
//             smokeMissile.Enabled = false;
//             SetEmitterState(smokeMissile, true); // Aktifkan kembali emitter untuk persiapan peluncuran berikutnya
//         }

//         if (sndMeluncur != null) { sndMeluncur.Stop(); sndMeluncur.Enabled = false; }
//         if (sndMeledak != null) { sndMeledak.Stop(); sndMeledak.Enabled = false; }
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;

//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = false;
//         else vfxNode.Enabled = false;
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }

//     private bool CheckInput()
//     {
//         if (keyBinding == "7")
//             return Input.IsKeyDown(Input.KEY.DIGIT_7) || Input.IsKeyDown(Input.KEY.DIGIT_7);

//         if (keyBinding == "8")
//             return Input.IsKeyDown(Input.KEY.DIGIT_8) || Input.IsKeyDown(Input.KEY.DIGIT_8);

//         if (keyBinding == "9")
//             return Input.IsKeyDown(Input.KEY.DIGIT_9) || Input.IsKeyDown(Input.KEY.DIGIT_9);

//         if (keyBinding == "0")
//             return Input.IsKeyDown(Input.KEY.DIGIT_0) || Input.IsKeyDown(Input.KEY.DIGIT_0);

//         return false;
//     }
// }