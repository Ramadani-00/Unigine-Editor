using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unigine;

[Component(PropertyGuid = "aae6640deda0d23634032280141c9c49773d6b39")]
public class MissileVLMICA : Component
{
    [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Atur ke KRI_RE_MARTADINATA (12 Silo) atau KRI_BRAWIJAYA (16 Silo) sesuai dropdown Delphi")]
    public string ownerShipID = "KRI_RE_MARTADINATA";
    [Parameter(Title = "Node Penutup (Cap)")] public Node siloCap = null;
    public float capOpenSpeed = 120.0f;
    public float capTargetAngle = -90.0f;
    [Parameter(Title = "Jeda Setelah Tutup Buka (Detik)")] public float postCapOpenDelay = 1.3f;
    public float exitSiloSpeed = 0.5f;
    public float midAscentSpeed = 50.0f;
    public float maxSpeed = 350.0f;
    public float accelerationRate = 120.0f;
    public float popUpDuration = 2.5f;
    public float verticalOnlyDuration = 0.5f;
    [Parameter(Title = "Jarak Aman (Detik)")] public float armingDelay = 0.5f;
    [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
    [Parameter(Title = "Target Node")] public Node targetNode = null;
    [Parameter(Title = "Nomor Silo Bind (1 sampai 16 / F1-F8)")] public string keyBinding = "1";
    public float tiltFactor = 0.5f;
    public float turnSmoothingRate = 3.5f;
    public float maxTurnSpeed = 8.0f;
    public Node explosionPrefab = null;
    public Node smokeTrail = null;
    public Node vfx_jet_engine = null;
    [Parameter(Title = "Sound Meluncur (Snd_Meluncur)")] public SoundSource launchSound = null;
    [Parameter(Title = "Sound Ledakan (Snd_Ledakan)")] public SoundSource explosionSound = null;

    private float currentSpeed = 0.0f;
    private float currentTurnSpeed = 1.0f;
    private bool isLaunched = false;
    private bool isExploded = false;
    private bool isSoundTriggeredAtOpen = false;
    private float sequenceTimer = 0.0f;
    private float flightTimer = 0.0f;
    private float currentCapAngle = 0.0f;
    private float postCapTimer = 0.0f;
    private enum FlightPhase { Idle, OpeningCap, Ignition, PopUp, Guidance }
    private FlightPhase currentPhase = FlightPhase.Idle;

    private Node initialParent;
    private dvec3 initialLocalPosition;
    private quat initialLocalRotation;
    private bool netTriggerLaunch = false;
    private bool netTriggerReload = false;
    private readonly object lockObject = new object();

    protected override void OnReady()
    {
        initialParent = node.Parent;
        initialLocalPosition = node.Position;
        initialLocalRotation = node.GetRotation();
        ResetAllSystems();
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
                    string targetSilo = cmdValue[1].Trim().ToUpper();
                    if (targetSilo == keyBinding.Trim().ToUpper())
                    {
                        lock (lockObject)
                        {
                            if (cmdName == "VLMICA_TEMBAK")
                            {
                                netTriggerLaunch = true;
                            }
                            else if (cmdName == "VLMICA_RELOAD")
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
            if (localNetworkLaunch && currentPhase == FlightPhase.Idle)
                currentPhase = FlightPhase.OpeningCap;

            if (currentPhase == FlightPhase.OpeningCap) UpdateCapOpening();
            else if (currentPhase == FlightPhase.Ignition) UpdateIgnitionSequence();
        }

        if (isLaunched && !isExploded)
        {
            flightTimer += dt;
            MoveVLMICA();
            CheckCollision();
        }
    }

    private void UpdateCapOpening()
    {
        if (siloCap == null) { currentPhase = FlightPhase.Ignition; return; }

        float finalTargetAngle = capTargetAngle;
        string key = keyBinding.ToUpper().Trim();

        int siloNumber = 0;
        bool isNumber = int.TryParse(key, out siloNumber);

        int midPointThreshold = 6;
        if (ownerShipID.ToUpper().Contains("BRAWIJAYA"))
        {
            midPointThreshold = 8;
        }

        if (key.Contains("F") || (isNumber && siloNumber > midPointThreshold))
        {
            finalTargetAngle = MathLib.Abs(capTargetAngle);
        }
        else
        {
            finalTargetAngle = -MathLib.Abs(capTargetAngle);
        }

        currentCapAngle = MathLib.MoveTowards(currentCapAngle, finalTargetAngle, capOpenSpeed * Game.IFps);
        siloCap.SetRotation(new quat(currentCapAngle, 0, 0));

        if (MathLib.Abs(currentCapAngle - finalTargetAngle) < 0.1f)
        {
            if (!isSoundTriggeredAtOpen && launchSound != null)
            {
                launchSound.Enabled = true;
                launchSound.Stop();
                launchSound.Play();
                isSoundTriggeredAtOpen = true;
                Log.Message($"[AUDIO] Cap opened! launchSound ACTIVATED inside VLS Silo {keyBinding} ({ownerShipID}).\n");
            }

            postCapTimer += Game.IFps;
            if (postCapTimer >= postCapOpenDelay)
            {
                currentPhase = FlightPhase.Ignition;
                sequenceTimer = 0.0f;
                if (smokeTrail != null)
                {
                    smokeTrail.Enabled = true;
                    SetEmitterState(smokeTrail, true);
                }
            }
        }
    }

    private void UpdateIgnitionSequence()
    {
        sequenceTimer += Game.IFps;
        if (sequenceTimer >= 0.2f) Launch();
    }

    private void Launch()
    {
        dvec3 worldPos = node.WorldPosition;
        quat worldRot = node.GetWorldRotation();
        node.Parent = null;
        node.WorldPosition = worldPos;
        node.SetWorldRotation(worldRot);
        isLaunched = true;
        currentPhase = FlightPhase.PopUp;
        flightTimer = 0.0f;
        sequenceTimer = 0.0f;
        currentSpeed = exitSiloSpeed;
        currentTurnSpeed = 1.0f;

        if (vfx_jet_engine != null)
        {
            vfx_jet_engine.Enabled = true;
            SetEmitterState(vfx_jet_engine, true);
        }
        Log.Message($"[VLS SYSTEM] VL-MICA: Silo {keyBinding} Launched from {ownerShipID}.\n");
    }

    private void MoveVLMICA()
    {
        float dt = Game.IFps;
        vec3 targetDir;

        if (currentPhase == FlightPhase.PopUp)
        {
            if (flightTimer < verticalOnlyDuration)
            {
                currentSpeed = MathLib.Lerp(currentSpeed, midAscentSpeed, 4.0f * dt);
                node.WorldTranslate(vec3.UP * currentSpeed * dt);
                node.SetWorldDirection(vec3.UP, vec3.FORWARD, MathLib.AXIS.Y);
                return;
            }
            else
            {
                if (targetNode != null)
                {
                    vec3 toTarget = MathLib.Normalize((vec3)targetNode.WorldPosition - (vec3)node.WorldPosition);
                    targetDir = MathLib.Normalize(MathLib.Lerp(vec3.UP, toTarget, tiltFactor));
                }
                else targetDir = vec3.UP;

                currentSpeed = MathLib.Lerp(currentSpeed, midAscentSpeed, 2.0f * dt);
            }

            if (flightTimer >= popUpDuration)
            {
                currentPhase = FlightPhase.Guidance;
                Log.Message($"[VLS MICA] Safe vertical altitude reached! Main rocket motor ignited.\n");
            }
        }
        else
        {
            currentTurnSpeed = MathLib.Lerp(currentTurnSpeed, maxTurnSpeed, turnSmoothingRate * dt);

            if (currentSpeed < maxSpeed) currentSpeed += accelerationRate * dt;

            if (targetNode != null)
            {
                vec3 targetPos = (vec3)targetNode.WorldPosition;
                targetDir = MathLib.Normalize(targetPos - (vec3)node.WorldPosition);
            }
            else targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
        }

        vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
        vec3 smoothDir = MathLib.Lerp(forward, targetDir, currentTurnSpeed * dt);
        smoothDir = MathLib.Normalize(smoothDir);
        node.WorldTranslate(smoothDir * currentSpeed * dt);
        node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
    }

    private void CheckCollision()
    {
        if (!isLaunched || isExploded) return;
        if (flightTimer < armingDelay) return;

        if (targetNode != null)
        {
            float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
            if (dist < 3.5f)
            {
                helicoptercontroller heliCtrl = targetNode.GetComponent<helicoptercontroller>() ?? targetNode.GetComponentInParent<helicoptercontroller>();


                if (heliCtrl != null)
                {
                    heliCtrl.TriggerCrash();
                    Log.Message($"[VL MICA] Hantaman telak! Memicu fase crash jatuh berputar pada helikopter: {targetNode.Name}.\n");
                }

                else
                {
                    ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();

                    if (ship != null) ship.ActivateDamage();
                }

                ExplodeAt(node.WorldPosition, targetNode);
                return;
            }
        }

        if (node.WorldPosition.z < -1.5f) ExplodeAt(node.WorldPosition, null);
    }

    private void ExplodeAt(dvec3 pos, Node hitParent)
    {
        if (isExploded) return;
        isExploded = true;
        if (launchSound != null) launchSound.Stop();
        if (explosionSound != null)
        {
            SoundSource snd = explosionSound.Clone() as SoundSource;
            snd.Parent = hitParent;
            snd.WorldPosition = pos;
            snd.Enabled = true;
            snd.Play();
        }

        if (explosionPrefab != null)
        {
            Node instance = explosionPrefab.Clone();
            instance.Parent = hitParent;
            instance.WorldPosition = pos;
            instance.Enabled = true;
        }

        StopVFXSmoothly(smokeTrail);
        StopVFXSmoothly(vfx_jet_engine);
        node.WorldPosition = new dvec3(0, 0, -10000);
    }

    public void ReloadMissile()
    {
        isLaunched = false;
        isExploded = false;
        isSoundTriggeredAtOpen = false;
        flightTimer = 0.0f;
        sequenceTimer = 0.0f;
        postCapTimer = 0.0f;
        currentCapAngle = 0.0f;
        currentSpeed = 0.0f;
        currentTurnSpeed = 1.0f;
        currentPhase = FlightPhase.Idle;
        node.Parent = initialParent;
        node.Position = (vec3)initialLocalPosition;
        node.SetRotation(initialLocalRotation);
        node.Enabled = false;
        node.Enabled = true;
        ResetAllSystems();
        Log.Message($"[VLS SYSTEM] VL-MICA: Silo {keyBinding} Reloaded on {ownerShipID}. Flight memories CLEARED.\n");
    }

    private void StopVFXSmoothly(Node vfxNode)
    {
        if (vfxNode == null) return;
        ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
        if (p != null)
        {
            p.EmitterEnabled = false;
            vfxNode.Parent = null;
        }

        else vfxNode.Enabled = false;
    }

    private void SetEmitterState(Node vfxNode, bool state)
    {
        if (vfxNode == null) return;
        ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
        if (p != null) p.EmitterEnabled = state;
    }

    private void ResetAllSystems()
    {
        currentCapAngle = 0.0f;
        isSoundTriggeredAtOpen = false;
        if (launchSound != null) launchSound.Stop();
        if (explosionSound != null)
        {
            explosionSound.Stop();
        }

        if (siloCap != null) siloCap.SetRotation(new quat(0, 0, 0));
        if (smokeTrail != null)
        {
            smokeTrail.Enabled = false;
            smokeTrail.Parent = node;
            smokeTrail.Position = vec3.ZERO;
            SetEmitterState(smokeTrail, true);
        }

        if (vfx_jet_engine != null)
        {
            vfx_jet_engine.Enabled = false;
            vfx_jet_engine.Parent = node;
            vfx_jet_engine.Position = vec3.ZERO;
            SetEmitterState(vfx_jet_engine, true);
        }
    }
}

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using Unigine;

// [Component(PropertyGuid = "aae6640deda0d23634032280141c9c49773d6b39")]
// public class MissileVLMICA : Component
// {
//     [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Atur ke KRI_RE_MARTADINATA (12 Silo) atau KRI_BRAWIJAYA (16 Silo) sesuai dropdown Delphi")]
//     public string ownerShipID = "KRI_RE_MARTADINATA";
//     [Parameter(Title = "Node Penutup (Cap)")] public Node siloCap = null;
//     public float capOpenSpeed = 120.0f;
//     public float capTargetAngle = -90.0f;
//     [Parameter(Title = "Jeda Setelah Tutup Buka (Detik)")] public float postCapOpenDelay = 1.3f;
//     public float exitSiloSpeed = 0.5f;
//     public float midAscentSpeed = 50.0f;
//     public float maxSpeed = 350.0f;
//     public float accelerationRate = 120.0f;
//     public float popUpDuration = 2.5f;
//     public float verticalOnlyDuration = 0.5f;
//     [Parameter(Title = "Jarak Aman (Detik)")] public float armingDelay = 0.5f;
//     [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
//     [Parameter(Title = "Target Node")] public Node targetNode = null;
//     [Parameter(Title = "Nomor Silo Bind (1 sampai 16 / F1-F8)")] public string keyBinding = "1";
//     public float tiltFactor = 0.5f;
//     public float turnSmoothingRate = 3.5f;
//     public float maxTurnSpeed = 8.0f;
//     public Node explosionPrefab = null;
//     public Node smokeTrail = null;
//     public Node vfx_jet_engine = null;
//     [Parameter(Title = "Sound Meluncur (Snd_Meluncur)")] public SoundSource launchSound = null;
//     [Parameter(Title = "Sound Ledakan (Snd_Ledakan)")] public SoundSource explosionSound = null;

//     private float currentSpeed = 0.0f;
//     private float currentTurnSpeed = 1.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isSoundTriggeredAtOpen = false;
//     private float sequenceTimer = 0.0f;
//     private float flightTimer = 0.0f;
//     private float currentCapAngle = 0.0f;
//     private float postCapTimer = 0.0f;
//     private enum FlightPhase { Idle, OpeningCap, Ignition, PopUp, Guidance }
//     private FlightPhase currentPhase = FlightPhase.Idle;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;
//     private bool netTriggerLaunch = false;
//     private bool netTriggerReload = false;
//     private readonly object lockObject = new object();

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();
//         ResetAllSystems();
//     }

//     public void ProcessNetworkData(string rawData)
//     {
//         // Perbaikan tanda kurung pada .Trim() agar tidak error compile
//         string[] parts = rawData.Split('|');
//         if (parts.Length == 2 && parts[0].Trim().ToUpper() == ownerShipID.ToUpper())
//         {
//             string command = parts[1].Trim().ToUpper();

//             if (command.Contains(":"))
//             {
//                 string[] cmdValue = command.Split(':');
//                 if (cmdValue.Length == 2)
//                 {
//                     string cmdName = cmdValue[0].Trim().ToUpper();
//                     string targetSilo = cmdValue[1].Trim().ToUpper();
//                     if (targetSilo == keyBinding.Trim().ToUpper())
//                     {
//                         lock (lockObject)
//                         {
//                             if (cmdName == "VLMICA_TEMBAK")
//                             {
//                                 netTriggerLaunch = true;
//                             }
//                             else if (cmdName == "VLMICA_RELOAD")
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
//         bool localNetworkLaunch = false;
//         bool localNetworkReload = false;

//         lock (lockObject)
//         {
//             localNetworkLaunch = netTriggerLaunch;
//             localNetworkReload = netTriggerReload;
//             netTriggerLaunch = false;
//             netTriggerReload = false;
//         }

//         if (localNetworkReload)
//         {
//             ReloadMissile();
//             return;
//         }

//         if (!isLaunched && !isExploded)
//         {
//             if (localNetworkLaunch && currentPhase == FlightPhase.Idle)
//                 currentPhase = FlightPhase.OpeningCap;

//             if (currentPhase == FlightPhase.OpeningCap) UpdateCapOpening();
//             else if (currentPhase == FlightPhase.Ignition) UpdateIgnitionSequence();
//         }

//         if (isLaunched && !isExploded)
//         {
//             flightTimer += dt;
//             MoveVLMICA();
//             CheckCollision();
//         }
//     }

//     private void UpdateCapOpening()
//     {
//         if (siloCap == null) { currentPhase = FlightPhase.Ignition; return; }

//         float finalTargetAngle = capTargetAngle;
//         string key = keyBinding.ToUpper().Trim();

//         int siloNumber = 0;
//         bool isNumber = int.TryParse(key, out siloNumber);

//         // LOGIKA BARU: Penentuan batas tengah jumlah Silo secara dinamis
//         int midPointThreshold = 6; // Default untuk KRI RE Martadinata (12 Silo)
//         if (ownerShipID.ToUpper().Contains("BRAWIJAYA"))
//         {
//             midPointThreshold = 8; // Khusus untuk KRI Brawijaya (16 Silo)
//         }

//         // Pembagian engsel pintu (Silo sisi kanan mekar negatif, sisi kiri mekar positif)
//         if (key.Contains("F") || (isNumber && siloNumber > midPointThreshold))
//         {
//             finalTargetAngle = MathLib.Abs(capTargetAngle);
//         }
//         else
//         {
//             finalTargetAngle = -MathLib.Abs(capTargetAngle);
//         }

//         currentCapAngle = MathLib.MoveTowards(currentCapAngle, finalTargetAngle, capOpenSpeed * Game.IFps);
//         siloCap.SetRotation(new quat(currentCapAngle, 0, 0));

//         if (MathLib.Abs(currentCapAngle - finalTargetAngle) < 0.1f)
//         {
//             if (!isSoundTriggeredAtOpen && launchSound != null)
//             {
//                 launchSound.Enabled = true;
//                 launchSound.Stop();
//                 launchSound.Play();
//                 isSoundTriggeredAtOpen = true;
//                 Log.Message($"[AUDIO] Cap opened! launchSound ACTIVATED inside VLS Silo {keyBinding} ({ownerShipID}).\n");
//             }

//             postCapTimer += Game.IFps;
//             if (postCapTimer >= postCapOpenDelay)
//             {
//                 currentPhase = FlightPhase.Ignition;
//                 sequenceTimer = 0.0f;
//                 if (smokeTrail != null)
//                 {
//                     smokeTrail.Enabled = true;
//                     SetEmitterState(smokeTrail, true);
//                 }
//             }
//         }
//     }

//     private void UpdateIgnitionSequence()
//     {
//         sequenceTimer += Game.IFps;
//         if (sequenceTimer >= 0.2f) Launch();
//     }

//     private void Launch()
//     {
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);
//         isLaunched = true;
//         currentPhase = FlightPhase.PopUp;
//         flightTimer = 0.0f;
//         sequenceTimer = 0.0f;
//         currentSpeed = exitSiloSpeed;
//         currentTurnSpeed = 1.0f;

//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = true;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//         Log.Message($"[VLS SYSTEM] VL-MICA: Silo {keyBinding} Launched from {ownerShipID}.\n");
//     }

//     private void MoveVLMICA()
//     {
//         float dt = Game.IFps;
//         vec3 targetDir;

//         // POP-UP (TVC - THRUST VECTOR CONTROL DENGAN BOOSTER BAWAH EKOR)
//         if (currentPhase == FlightPhase.PopUp)
//         {
//             // KONDISI A: Selama berada di dalam tabung silo, KUNCI MATI VERTIKAL 90 DERAJAT
//             if (flightTimer < verticalOnlyDuration)
//             {
//                 currentSpeed = MathLib.Lerp(currentSpeed, midAscentSpeed, 4.0f * dt);
//                 node.WorldTranslate(vec3.UP * currentSpeed * dt);
//                 node.SetWorldDirection(vec3.UP, vec3.FORWARD, MathLib.AXIS.Y);
//                 return;
//             }
//             else
//             {
//                 // KONDISI B: Setelah roket lolos terbang tinggi keluar tabung, Thrust Vector mulai miring
//                 if (targetNode != null)
//                 {
//                     // Perbaikan Type Casting (vec3) koordinat posisi agar sinkron
//                     vec3 toTarget = MathLib.Normalize((vec3)targetNode.WorldPosition - (vec3)node.WorldPosition);
//                     targetDir = MathLib.Normalize(MathLib.Lerp(vec3.UP, toTarget, tiltFactor));
//                 }
//                 else targetDir = vec3.UP;

//                 currentSpeed = MathLib.Lerp(currentSpeed, midAscentSpeed, 2.0f * dt);
//             }

//             if (flightTimer >= popUpDuration)
//             {
//                 currentPhase = FlightPhase.Guidance;
//                 Log.Message($"[VLS MICA] Safe vertical altitude reached! Main rocket motor ignited.\n");
//             }
//         }
//         // ACTIVE GUIDANCE HOMING (PANDUAN RADAR AKTIF SUPERSONIK)
//         else
//         {
//             currentTurnSpeed = MathLib.Lerp(currentTurnSpeed, maxTurnSpeed, turnSmoothingRate * dt);

//             if (currentSpeed < maxSpeed) currentSpeed += accelerationRate * dt;

//             if (targetNode != null)
//             {
//                 vec3 targetPos = (vec3)targetNode.WorldPosition;
//                 targetDir = MathLib.Normalize(targetPos - (vec3)node.WorldPosition);
//             }
//             else targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, currentTurnSpeed * dt);
//         smoothDir = MathLib.Normalize(smoothDir);
//         node.WorldTranslate(smoothDir * currentSpeed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (!isLaunched || isExploded) return;
//         if (flightTimer < armingDelay) return;
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 3.5f)
//             {
//                 ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
//                 if (ship != null) ship.ActivateDamage();
//                 ExplodeAt(node.WorldPosition, targetNode);
//                 return;
//             }
//         }

//         if (node.WorldPosition.z < -1.5f) ExplodeAt(node.WorldPosition, null);
//     }

//     private void ExplodeAt(dvec3 pos, Node hitParent)
//     {
//         if (isExploded) return;
//         isExploded = true;
//         if (launchSound != null) launchSound.Stop();
//         if (explosionSound != null)
//         {
//             SoundSource snd = explosionSound.Clone() as SoundSource;
//             snd.Parent = hitParent;
//             snd.WorldPosition = pos;
//             snd.Enabled = true;
//             snd.Play();
//         }

//         if (explosionPrefab != null)
//         {
//             Node instance = explosionPrefab.Clone();
//             instance.Parent = hitParent;
//             instance.WorldPosition = pos;
//             instance.Enabled = true;
//         }

//         StopVFXSmoothly(smokeTrail);
//         StopVFXSmoothly(vfx_jet_engine);
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isSoundTriggeredAtOpen = false;
//         flightTimer = 0.0f;
//         sequenceTimer = 0.0f;
//         postCapTimer = 0.0f;
//         currentCapAngle = 0.0f;
//         currentSpeed = 0.0f;
//         currentTurnSpeed = 1.0f;
//         currentPhase = FlightPhase.Idle;
//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = false;
//         node.Enabled = true;
//         ResetAllSystems();
//         Log.Message($"[VLS SYSTEM] VL-MICA: Silo {keyBinding} Reloaded on {ownerShipID}. Flight memories CLEARED.\n");
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
//         if (p != null)
//         {
//             p.EmitterEnabled = false;
//             vfxNode.Parent = null;
//         }

//         else vfxNode.Enabled = false;
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
//         if (p != null) p.EmitterEnabled = state;
//     }

//     private void ResetAllSystems()
//     {
//         currentCapAngle = 0.0f;
//         isSoundTriggeredAtOpen = false;
//         if (launchSound != null) launchSound.Stop();
//         if (explosionSound != null) explosionSound.Stop();
//         if (siloCap != null) siloCap.SetRotation(new quat(0, 0, 0));
//         if (smokeTrail != null)
//         {
//             smokeTrail.Enabled = false;
//             smokeTrail.Parent = node;
//             smokeTrail.Position = vec3.ZERO;
//             SetEmitterState(smokeTrail, true);
//         }

//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = false;
//             vfx_jet_engine.Parent = node;
//             vfx_jet_engine.Position = vec3.ZERO;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//     }
// }


// // ================================= VLMICA =================================

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using Unigine;

// [Component(PropertyGuid = "aae6640deda0d23634032280141c9c49773d6b39")]
// public class MissileVLMICA : Component
// {
//     [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama dengan ID Kapal di Delphi agar tidak tertukar saat menembak, misal KRI_RE_MARTADINATA")]
//     public string ownerShipID = "KRI_RE_MARTADINATA";
//     [Parameter(Title = "Node Penutup (Cap)")] public Node siloCap = null;
//     public float capOpenSpeed = 120.0f;
//     public float capTargetAngle = -90.0f;
//     [Parameter(Title = "Jeda Setelah Tutup Buka (Detik)")] public float postCapOpenDelay = 1.3f;
//     public float exitSiloSpeed = 0.5f;
//     public float midAscentSpeed = 50.0f;
//     public float maxSpeed = 350.0f;
//     public float accelerationRate = 120.0f;
//     public float popUpDuration = 2.5f;
//     public float verticalOnlyDuration = 0.5f;
//     [Parameter(Title = "Jarak Aman (Detik)")] public float armingDelay = 0.5f;
//     [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
//     [Parameter(Title = "Target Node")] public Node targetNode = null;
//     [Parameter(Title = "Nomor Silo Bind (1 sampai 12 / F1-F8)")] public string keyBinding = "1";
//     public float tiltFactor = 0.5f;
//     public float turnSmoothingRate = 3.5f;
//     public float maxTurnSpeed = 8.0f;
//     public Node explosionPrefab = null;
//     public Node smokeTrail = null;
//     public Node vfx_jet_engine = null;
//     [Parameter(Title = "Sound Meluncur (Snd_Meluncur)")] public SoundSource launchSound = null;
//     [Parameter(Title = "Sound Ledakan (Snd_Ledakan)")] public SoundSource explosionSound = null;
//     private float currentSpeed = 0.0f;
//     private float currentTurnSpeed = 1.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isSoundTriggeredAtOpen = false;
//     private float sequenceTimer = 0.0f;
//     private float flightTimer = 0.0f;
//     private float currentCapAngle = 0.0f;
//     private float postCapTimer = 0.0f;
//     private enum FlightPhase { Idle, OpeningCap, Ignition, PopUp, Guidance }
//     private FlightPhase currentPhase = FlightPhase.Idle;
//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;
//     private bool netTriggerLaunch = false;
//     private bool netTriggerReload = false;
//     private readonly object lockObject = new object();

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();
//         ResetAllSystems();
//     }

//     public void ProcessNetworkData(string rawData)
//     {
//         string[] parts = rawData.Split('|');
//         if (parts.Length == 2 && parts[0].Trim().ToUpper() == ownerShipID.ToUpper())
//         {
//             string command = parts[1].Trim().ToUpper();

//             if (command.Contains(":"))
//             {
//                 string[] cmdValue = command.Split(':');
//                 if (cmdValue.Length == 2)
//                 {
//                     string cmdName = cmdValue[0].Trim().ToUpper();
//                     string targetSilo = cmdValue[1].Trim().ToUpper();
//                     if (targetSilo == keyBinding.Trim().ToUpper())
//                     {
//                         lock (lockObject)
//                         {
//                             if (cmdName == "VLMICA_TEMBAK")
//                             {
//                                 netTriggerLaunch = true;
//                             }
//                             else if (cmdName == "VLMICA_RELOAD")
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
//         bool localNetworkLaunch = false;
//         bool localNetworkReload = false;

//         lock (lockObject)
//         {
//             localNetworkLaunch = netTriggerLaunch;
//             localNetworkReload = netTriggerReload;
//             netTriggerLaunch = false;
//             netTriggerReload = false;
//         }

//         if (localNetworkReload)
//         {
//             ReloadMissile();
//             return;
//         }

//         if (!isLaunched && !isExploded)
//         {
//             if (localNetworkLaunch && currentPhase == FlightPhase.Idle)
//                 currentPhase = FlightPhase.OpeningCap;

//             if (currentPhase == FlightPhase.OpeningCap) UpdateCapOpening();
//             else if (currentPhase == FlightPhase.Ignition) UpdateIgnitionSequence();
//         }

//         if (isLaunched && !isExploded)
//         {
//             flightTimer += dt;
//             MoveVLMICA();
//             CheckCollision();
//         }
//     }

//     private void UpdateCapOpening()
//     {
//         if (siloCap == null) { currentPhase = FlightPhase.Ignition; return; }

//         float finalTargetAngle = capTargetAngle;
//         string key = keyBinding.ToUpper().Trim();

//         int siloNumber = 0;
//         bool isNumber = int.TryParse(key, out siloNumber);
//         if (key.Contains("F") || (isNumber && siloNumber >= 7 && siloNumber <= 12))
//         {
//             finalTargetAngle = MathLib.Abs(capTargetAngle);
//         }
//         else
//         {
//             finalTargetAngle = -MathLib.Abs(capTargetAngle);
//         }

//         currentCapAngle = MathLib.MoveTowards(currentCapAngle, finalTargetAngle, capOpenSpeed * Game.IFps);
//         siloCap.SetRotation(new quat(currentCapAngle, 0, 0));

//         if (MathLib.Abs(currentCapAngle - finalTargetAngle) < 0.1f)
//         {
//             if (!isSoundTriggeredAtOpen && launchSound != null)
//             {
//                 launchSound.Enabled = true;
//                 launchSound.Stop();
//                 launchSound.Play();
//                 isSoundTriggeredAtOpen = true;
//                 Log.Message($"[AUDIO] Cap opened! launchSound ACTIVATED inside VLS Silo {keyBinding}.\n");
//             }

//             postCapTimer += Game.IFps;
//             if (postCapTimer >= postCapOpenDelay)
//             {
//                 currentPhase = FlightPhase.Ignition;
//                 sequenceTimer = 0.0f;
//                 if (smokeTrail != null)
//                 {
//                     smokeTrail.Enabled = true;
//                     SetEmitterState(smokeTrail, true);
//                 }
//             }
//         }
//     }

//     private void UpdateIgnitionSequence()
//     {
//         sequenceTimer += Game.IFps;
//         if (sequenceTimer >= 0.2f) Launch();
//     }

//     private void Launch()
//     {
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);
//         isLaunched = true;
//         currentPhase = FlightPhase.PopUp;
//         flightTimer = 0.0f;
//         sequenceTimer = 0.0f;
//         currentSpeed = exitSiloSpeed;
//         currentTurnSpeed = 1.0f;

//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = true;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//         Log.Message($"[VLS SYSTEM] VL-MICA: Silo {keyBinding} Launched. Timer force locked to 0.0f.\n");
//     }

//     private void MoveVLMICA()
//     {
//         float dt = Game.IFps;
//         vec3 targetDir;

//         // POP-UP (TVC - THRUST VECTOR CONTROL DENGAN BOOSTER BAWAH EKOR)
//         if (currentPhase == FlightPhase.PopUp)
//         {
//             // KONDISI A: Selama berada di dalam tabung silo, KUNCI MATI VERTIKAL 90 DERAJAT
//             if (flightTimer < verticalOnlyDuration)
//             {
//                 // Akselerasi awal merayap keluar dari lubang pangkalan kontainer silo
//                 currentSpeed = MathLib.Lerp(currentSpeed, midAscentSpeed, 4.0f * dt);
//                 node.WorldTranslate(vec3.UP * currentSpeed * dt);
//                 node.SetWorldDirection(vec3.UP, vec3.FORWARD, MathLib.AXIS.Y);
//                 return;
//             }
//             else
//             {
//                 // KONDISI B: Setelah roket lolos terbang tinggi keluar tabung, Thrust Vector mulai miring
//                 if (targetNode != null)
//                 {
//                     vec3 toTarget = MathLib.Normalize((vec3)targetNode.WorldPosition - (vec3)node.WorldPosition);
//                     targetDir = MathLib.Normalize(MathLib.Lerp(vec3.UP, toTarget, tiltFactor));
//                 }
//                 else targetDir = vec3.UP;

//                 // Akselerasi booster melesat menjulang tinggi ke angkasa
//                 currentSpeed = MathLib.Lerp(currentSpeed, midAscentSpeed, 2.0f * dt);
//             }

//             // Pindah otomatis ke sekuens pembakaran motor roket utama (Guidance Homing)
//             if (flightTimer >= popUpDuration)
//             {
//                 currentPhase = FlightPhase.Guidance;
//                 Log.Message($"[VLS MICA] Safe vertical altitude reached! Main rocket motor ignited.\n");
//             }
//         }
//         // ACTIVE GUIDANCE HOMING (PANDUAN RADAR AKTIF SUPERSONIK)
//         else
//         {
//             // Kemudi belok dihaluskan gradual agar radius meliuk melengkung indah mengejar helikopter
//             currentTurnSpeed = MathLib.Lerp(currentTurnSpeed, maxTurnSpeed, turnSmoothingRate * dt);

//             // Pengapian roket utama mendorong akselerasi ekstrem hingga maxSpeed (350 m/s)
//             if (currentSpeed < maxSpeed) currentSpeed += accelerationRate * dt;

//             if (targetNode != null)
//             {
//                 vec3 targetPos = (vec3)targetNode.WorldPosition;
//                 targetDir = MathLib.Normalize(targetPos - (vec3)node.WorldPosition);
//             }
//             else targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         // Eksekusi pergerakan translasi dan rotasi normal untuk Fase Homing & Sisa Fase Pop-Up Atas
//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, currentTurnSpeed * dt);
//         smoothDir = MathLib.Normalize(smoothDir);

//         node.WorldTranslate(smoothDir * currentSpeed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (!isLaunched || isExploded) return;
//         if (flightTimer < armingDelay) return;
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 3.5f)
//             {
//                 ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
//                 if (ship != null) ship.ActivateDamage();

//                 ExplodeAt(node.WorldPosition, targetNode);
//                 return;
//             }
//         }

//         if (node.WorldPosition.z < -1.5f) ExplodeAt(node.WorldPosition, null);
//     }

//     private void ExplodeAt(dvec3 pos, Node hitParent)
//     {
//         if (isExploded) return;
//         isExploded = true;
//         if (launchSound != null) launchSound.Stop();
//         if (explosionSound != null)
//         {
//             SoundSource snd = explosionSound.Clone() as SoundSource;
//             snd.Parent = hitParent;
//             snd.WorldPosition = pos;
//             snd.Enabled = true;
//             snd.Play();
//         }

//         if (explosionPrefab != null)
//         {
//             Node instance = explosionPrefab.Clone();
//             instance.Parent = hitParent;
//             instance.WorldPosition = pos;
//             instance.Enabled = true;
//         }

//         StopVFXSmoothly(smokeTrail);
//         StopVFXSmoothly(vfx_jet_engine);
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadMissile()
//     {
//         // Matikan seluruh flag aktivitas penerbangan di frame pertama interupsi reload
//         isLaunched = false;
//         isExploded = false;
//         isSoundTriggeredAtOpen = false;

//         // Bersihkan total memori sisa akumulasi waktu frame masa lalu
//         flightTimer = 0.0f;
//         sequenceTimer = 0.0f;
//         postCapTimer = 0.0f;
//         currentCapAngle = 0.0f;
//         currentSpeed = 0.0f;
//         currentTurnSpeed = 1.0f;

//         // Kembalikan fase ke kondisi tidur stasioner (Idle)
//         currentPhase = FlightPhase.Idle;

//         // Reposisi fisik node masuk kembali ke sarang tabung kontainer kover kapal
//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);

//         // Paksa refresh komponen visual node agar siklus Unigine C# melakukan hard-reset
//         node.Enabled = false;
//         node.Enabled = true;

//         ResetAllSystems();
//         Log.Message($"[VLS SYSTEM] VL-MICA: Silo {keyBinding} Reloaded via Delphi. All flight memories CLEARED.\n");
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
//         if (p != null)
//         {
//             p.EmitterEnabled = false;
//             vfxNode.Parent = null;
//         }

//         else vfxNode.Enabled = false;
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
//         if (p != null) p.EmitterEnabled = state;
//     }

//     private void ResetAllSystems()
//     {
//         currentCapAngle = 0.0f;
//         isSoundTriggeredAtOpen = false;
//         if (launchSound != null) launchSound.Stop();
//         if (explosionSound != null) explosionSound.Stop();
//         if (siloCap != null) siloCap.SetRotation(new quat(0, 0, 0));
//         if (smokeTrail != null)
//         {
//             smokeTrail.Enabled = false;
//             smokeTrail.Parent = node;
//             smokeTrail.Position = vec3.ZERO;
//             SetEmitterState(smokeTrail, true);
//         }

//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = false;
//             vfx_jet_engine.Parent = node;
//             vfx_jet_engine.Position = vec3.ZERO;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//     }
// }