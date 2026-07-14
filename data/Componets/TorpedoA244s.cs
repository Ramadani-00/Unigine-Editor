// // ==================== Torpedo A244s ============================

using System;
using System.Globalization;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "82b2d4c4a514c5a556667df2954307e2e05552d5")]
public class TorpedoA244s : Component
{
    [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama dengan nama ID Kapal di dropdown Delphi")]
    public string ownerShipID = "KRI_RE_MARTADINATA";
    [Parameter(Title = "Nomor Tabung Bind (1 sampai 4)")]
    public string keyBinding = "1";
    [Parameter(Title = "Node Penutup Tabung (Cover)")]
    public Node torpedoCoverNode = null;
    public float coverOpenSpeed = 110.0f;
    public float coverTargetAngle = -90.0f;
    [Parameter(Title = "Jeda Setelah Pintu Buka (Detik)")]
    public float postCoverOpenDelay = 1.2f;
    public float launchPushSpeed = 12.0f;
    public float cruiseSpeed = 100.0f;
    public float accelerationRate = 20.0f;
    public float turnSpeed = 2.5f;
    [Parameter(Title = "Kedalaman Jelajah Permukaan")]
    public float torpedoDepth = -1.0f;
    [Parameter(Title = "Durasi Maju Horizontal (Udara)")]
    public float straightAirDuration = 0.5f;
    [Parameter(Title = "Kecepatan Jatuh (Gravitasi Udara)")]
    public float dropSpeed = 7.0f;
    [Parameter(Title = "Jeda Pengunci Sensor Ledak (Detik)")]
    public float armingDelay = 4.0f;
    [Parameter(Title = "Target Node (Kapal/Selam Musuh)")]
    public Node targetNode = null;
    [Parameter(Title = "Mask Tabrakan")]
    public int hitMask = 1;
    [Parameter(Title = "Ledakan Kapal (explosion)")]
    public Node explosionShipPrefab = null;
    [Parameter(Title = "Ledakan Air (water_explosion)")]
    public Node explosionWaterPrefab = null;
    [Parameter(Title = "Efek Buih Air (Trail)")]
    public Node waterTrailVFX = null;
    [Parameter(Title = "Asap Peluncuran (Out_Smoke_Torpedo)")]
    public Node launchSmokeVFX = null;
    [Parameter(Title = "Sound Meluncur (Snd_meluncur)")]
    public SoundSource launchSound = null;
    [Parameter(Title = "Sound Masuk Air (Snd_masukair)")]
    public SoundSource entryWaterSound = null;
    [Parameter(Title = "Sound Ledakan (Snd_ledakan)")]
    public SoundSource explosionSound = null;
    private float currentSpeed = 0.0f;
    private bool isLaunched = false;
    private bool isExploded = false;
    private bool isInWater = false;
    private float flightTimer = 0.0f;
    private Node initialParent;
    private dvec3 initialLocalPosition;
    private quat initialLocalRotation;
    private enum LaunchPhase { Idle, OpeningCover, Launching }
    private LaunchPhase currentPhase = LaunchPhase.Idle;
    private float currentCoverAngle = 0.0f;
    private float postCoverTimer = 0.0f;
    private static int rightTubesFiredCount = 0;
    private static int leftTubesFiredCount = 0;
    private static bool isRightCoverOpenPermanent = false;
    private static bool isLeftCoverOpenPermanent = false;
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
                    string targetTabung = cmdValue[1].Trim().ToUpper();
                    if (targetTabung == keyBinding.Trim().ToUpper())
                    {
                        lock (lockObject)
                        {
                            if (cmdName == "TORPEDO_TEMBAK") netTriggerLaunch = true;
                            else if (cmdName == "TORPEDO_RELOAD") netTriggerReload = true;
                        }
                    }
                }
            }
        }
    }

    void Update()
    {
        float dt = Game.IFps;
        bool localNetworkLaunch, localNetworkReload;

        lock (lockObject)
        {
            localNetworkLaunch = netTriggerLaunch;
            localNetworkReload = netTriggerReload;
            netTriggerLaunch = false;
            netTriggerReload = false;
        }

        if (localNetworkReload)
        {
            ReloadTorpedo();
            return;
        }

        if (currentPhase == LaunchPhase.Idle && localNetworkLaunch)
        {
            string key = keyBinding.Trim();
            bool isRightSide = (key == "1" || key == "2");

            // Akumulasi sisa muatan tabung lambung
            if (isRightSide) rightTubesFiredCount = MathLib.Clamp(rightTubesFiredCount + 1, 0, 2);
            else leftTubesFiredCount = MathLib.Clamp(leftTubesFiredCount + 1, 0, 2);

            // PERBAIKAN: Jika pintu sisi ini SUDAH terbuka penuh dari tembakan sebelumnya, LANGSUNG LUNCURKAN!
            if ((isRightSide && isRightCoverOpenPermanent) || (!isRightSide && isLeftCoverOpenPermanent))
            {
                currentPhase = LaunchPhase.Launching;
                LaunchTorpedo();
                Log.Message($"[TORPEDO] Tube {keyBinding} Door already open. Launching instantly!\n");
            }
            else
            {
                // Jika belum terbuka, jalankan prosedur buka pintu normal seperti biasa
                currentPhase = LaunchPhase.OpeningCover;
                postCoverTimer = 0.0f;

                if (key == "3" || key == "4") coverTargetAngle = MathLib.Abs(coverTargetAngle);
                else coverTargetAngle = -MathLib.Abs(coverTargetAngle);

                if (torpedoCoverNode != null)
                {
                    vec3 currentEuler = MathLib.DecomposeRotationXYZ(torpedoCoverNode.GetWorldRotation().Mat3);
                    currentCoverAngle = currentEuler.y;
                }
                else currentCoverAngle = 0.0f;

                Log.Message($"[TORPEDO] Tube {keyBinding} Opening Door... Current Y Angle: {currentCoverAngle:F1}\n");
            }
        }

        if (currentPhase == LaunchPhase.OpeningCover)
        {
            UpdateCoverOpeningSequence(dt);
        }

        if (isLaunched && !isExploded && currentPhase == LaunchPhase.Launching)
        {
            flightTimer += dt;
            MoveTorpedo();
            CheckCollision();
        }
    }

    private void UpdateCoverOpeningSequence(float dt)
    {
        if (torpedoCoverNode == null) { LaunchTorpedo(); return; }

        if (MathLib.Abs(currentCoverAngle - coverTargetAngle) < 0.5f)
        {
            torpedoCoverNode.SetRotation(new quat(0, coverTargetAngle, 0));

            // PERBAIKAN: Kunci status pintu secara permanen di memori static global kapal
            string key = keyBinding.Trim();
            if (key == "1" || key == "2") isRightCoverOpenPermanent = true;
            if (key == "3" || key == "4") isLeftCoverOpenPermanent = true;

            postCoverTimer += dt;
            if (postCoverTimer >= postCoverOpenDelay) LaunchTorpedo();
        }
        else
        {
            currentCoverAngle = MathLib.MoveTowards(currentCoverAngle, coverTargetAngle, coverOpenSpeed * dt);
            torpedoCoverNode.SetRotation(new quat(0, currentCoverAngle, 0));
        }
    }

    private void LaunchTorpedo()
    {
        currentPhase = LaunchPhase.Launching;
        if (launchSmokeVFX != null)
        {
            launchSmokeVFX.Enabled = true;
            SetEmitterState(launchSmokeVFX, true);
        }

        dvec3 worldPos = node.WorldPosition;
        quat worldRot = node.GetWorldRotation();

        node.Parent = null;
        node.WorldPosition = worldPos;
        node.SetWorldRotation(worldRot);

        isLaunched = true;
        isInWater = false;
        flightTimer = 0.0f;
        currentSpeed = 5.0f;

        if (launchSound != null) { launchSound.Enabled = true; launchSound.Play(); }
        Log.Message($"[TORPEDO] Whitehead A244/S Ejected from Tube {keyBinding} successfully!\n");
    }

    private void MoveTorpedo()
    {
        float dt = Game.IFps;
        vec3 targetDir;

        if (!isInWater)
        {
            currentSpeed = MathLib.MoveTowards(currentSpeed, cruiseSpeed, accelerationRate * dt);

            targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
            if (flightTimer < straightAirDuration)
            {
                node.WorldTranslate(targetDir * currentSpeed * dt);
            }
            else
            {
                node.WorldTranslate((targetDir * currentSpeed * dt) + (vec3.DOWN * dropSpeed * dt));
            }

            if (node.WorldPosition.z <= 0.0f)
            {
                isInWater = true;

                if (launchSmokeVFX != null)
                {
                    dvec3 smokeWorldPos = launchSmokeVFX.WorldPosition;
                    quat smokeWorldRot = launchSmokeVFX.GetWorldRotation();
                    launchSmokeVFX.Parent = null;
                    launchSmokeVFX.WorldPosition = smokeWorldPos;
                    launchSmokeVFX.SetWorldRotation(smokeWorldRot);

                    ObjectParticles p = launchSmokeVFX as ObjectParticles ?? launchSmokeVFX.GetChild(0) as ObjectParticles;
                    if (p != null) p.EmitterEnabled = false;
                }

                if (entryWaterSound != null)
                {
                    entryWaterSound.Enabled = true;
                    entryWaterSound.Play();
                }
                if (explosionWaterPrefab != null)
                {
                    Node instance = explosionWaterPrefab.Clone();
                    instance.WorldPosition = node.WorldPosition;
                    instance.Enabled = true;
                }

                if (waterTrailVFX != null)
                {
                    waterTrailVFX.Enabled = true;
                    SetEmitterState(waterTrailVFX, true);
                }
            }
        }

        else
        {
            currentSpeed = MathLib.MoveTowards(currentSpeed, cruiseSpeed, accelerationRate * dt);

            if (targetNode != null)
            {
                vec3 currentPos = (vec3)node.WorldPosition;
                vec3 targetPos = (vec3)targetNode.WorldPosition;
                float targetZ = targetPos.z;

                float dist2D = (float)MathLib.Distance(new vec2(node.WorldPosition.x, node.WorldPosition.y), new vec2(targetNode.WorldPosition.x, targetNode.WorldPosition.y));
                if (targetZ >= -1.5f)
                {
                    targetZ = (dist2D > 50.0f) ? torpedoDepth : targetPos.z;
                }
                else
                {
                    targetZ = targetPos.z;
                }

                vec3 targetHomingPos = new vec3(targetPos.x, targetPos.y, targetZ);
                targetDir = MathLib.Normalize(targetHomingPos - currentPos);
            }
            else targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

            vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
            vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
            node.WorldTranslate(smoothDir * currentSpeed * dt);
            node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
        }
    }

    private void CheckCollision()
    {
        if (!isLaunched || isExploded || flightTimer < armingDelay || currentPhase != LaunchPhase.Launching) return;

        if (targetNode != null)
        {
            float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
            if (dist < 7.6f)
            {
                ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
                if (ship != null) ship.ActivateDamage();
                ExplodeTorpedo(node.WorldPosition, targetNode, true);
                return;
            }
        }

        vec3 p0 = (vec3)node.WorldPosition;
        vec3 p1 = p0 + (node.GetWorldDirection(MathLib.AXIS.Y) * (currentSpeed * Game.IFps + 3.0f));

        WorldIntersectionNormal hitInfo = new WorldIntersectionNormal();
        Node hitObj = World.GetIntersection(p0, p1, hitMask, hitInfo);
        if (hitObj != null)
        {
            if (hitObj == targetNode || hitObj.IsChild(targetNode) || hitObj.Name.Contains("ALUGORO") || hitObj.Name.Contains("BELATI") || hitObj.Name.Contains("GOLOK") || hitObj.Name.Contains("KRI"))
            {
                ShipDamageHandler ship = hitObj.GetComponent<ShipDamageHandler>() ?? hitObj.GetComponentInParent<ShipDamageHandler>();
                if (ship != null) ship.ActivateDamage();
                ExplodeTorpedo(hitInfo.Point, hitObj, true);
                return;
            }
        }

        if (flightTimer > 45.0f) ExplodeTorpedo(node.WorldPosition, null, false);
    }

    private void ExplodeTorpedo(dvec3 pos, Node hitParent, bool hitShip)
    {
        if (isExploded) return;
        isExploded = true;
        if (launchSound != null) launchSound.Stop();
        if (entryWaterSound != null) entryWaterSound.Stop();
        if (explosionSound != null)
        {
            SoundSource snd = explosionSound.Clone() as SoundSource;
            snd.Parent = hitParent;
            snd.WorldPosition = pos;
            snd.Enabled = true;
            snd.Play();
        }

        Node activeExplosionPrefab = hitShip ? explosionShipPrefab : explosionWaterPrefab;
        if (activeExplosionPrefab != null)
        {
            Node instance = activeExplosionPrefab.Clone();
            instance.Parent = hitParent;
            instance.WorldPosition = pos;
            instance.Enabled = true;
        }

        if (launchSmokeVFX != null) StopVFXSmoothly(launchSmokeVFX);
        if (waterTrailVFX != null) StopVFXSmoothly(waterTrailVFX);
        node.WorldPosition = new dvec3(0, 0, -10000);
    }

    public void ReloadTorpedo()
    {
        isLaunched = false;
        isExploded = false;
        isInWater = false;
        flightTimer = 0.0f;
        currentSpeed = 0.0f;
        currentPhase = LaunchPhase.Idle;
        node.Parent = initialParent;
        node.Position = (vec3)initialLocalPosition;
        node.SetRotation(initialLocalRotation);
        node.Enabled = false;
        node.Enabled = true;

        string key = keyBinding.Trim();
        if (key == "1" || key == "2") rightTubesFiredCount = MathLib.Clamp(rightTubesFiredCount - 1, 0, 2);
        if (key == "3" || key == "4") leftTubesFiredCount = MathLib.Clamp(leftTubesFiredCount - 1, 0, 2);

        // PERBAIKAN: Riset kunci pintu saat reload amunisi baru dimasukkan ke tabung
        if (key == "1" || key == "2") isRightCoverOpenPermanent = false;
        if (key == "3" || key == "4") isLeftCoverOpenPermanent = false;

        if (torpedoCoverNode != null)
        {
            torpedoCoverNode.SetRotation(new quat(0, 0, 0));
        }

        ResetAllSystems();
        Log.Message($"[TORPEDO] Tube {keyBinding} Armament Reloaded inside Launcher Rack.\n");
    }

    private void StopVFXSmoothly(Node vfxNode)
    {
        if (vfxNode == null) return; ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
        if (p != null)
        {
            p.EmitterEnabled = false;
            vfxNode.Parent = null;
        }

        else vfxNode.Enabled = false;
    }

    private void SetEmitterState(Node vfxNode, bool state)
    {
        if (vfxNode == null) return; ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
        if (p != null) p.EmitterEnabled = state;
    }

    private void ResetAllSystems()
    {
        currentCoverAngle = coverTargetAngle;
        if (launchSound != null) launchSound.Stop();
        if (entryWaterSound != null) entryWaterSound.Stop();

        if (launchSmokeVFX != null)
        {
            launchSmokeVFX.Enabled = false;
            launchSmokeVFX.Parent = node;
            launchSmokeVFX.Position = vec3.ZERO;
            SetEmitterState(launchSmokeVFX, true);
        }

        if (waterTrailVFX != null)
        {
            waterTrailVFX.Enabled = false;
            waterTrailVFX.Parent = node;
            waterTrailVFX.Position = vec3.ZERO;
            SetEmitterState(waterTrailVFX, true);
        }
    }
}