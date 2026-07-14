// ========================= Controller Beta 1 ====================

using System;
using System.Globalization;
using Unigine;

public class ShipRemoteControl : Component
{
    [Parameter(Title = "ID Kapal")]
    public string shipID = "KRI_RADJIMAN";
    public float maxMoveSpeed = 15.5f;
    public float accelerationFactor = 0.5f;
    public float maxTurnSpeed = 15.0f;
    public float turnAccelerationFactor = 1.5f;
    [Parameter(Title = "Apakah Kapal Selam?", Tooltip = "Centang ini KHUSUS untuk KRI Alugoro")]
    public bool isSubmarine = false;
    public float diveSpeed = 2.0f;
    public float maxDiveDepth = -15.0f;
    public float surfaceZPosition = 0.0f;
    public Node propellerLeft = null;
    public Node propellerRight = null;
    public float propRotationMultiplier = 600.0f;
    [Parameter(Title = "Sound Mesin (Snd_Engine)")]
    public SoundSource engineSound = null;
    public float idleVolume = 0.3f;
    public float maxGasVolume = 1.2f;
    public float idlePitch = 0.7f;
    public float maxGasPitch = 1.3f;
    [Parameter(Title = "Sound Klakson (Snd_horn)")]
    public SoundSource hornSound = null;
    [Parameter(Title = "Sound Sonar (Snd_Alugoro)")]
    public SoundSource submarineSonarSound = null;
    private float targetThrottle = 0.0f;
    private float actualThrottle = 0.0f;
    private float targetTurn = 0.0f;
    private float actualTurn = 0.0f;
    private bool isTurningLeft = false;
    private bool isTurningRight = false;
    private bool isCommandDive = false;
    private bool isCommandSurface = false;
    private float currentZDepth = 0.0f;
    private readonly object lockObject = new object();
    private bool isShipSelectedInDelphi = false;
    protected override void OnReady()
    {
        ShipTCPManager.RegisterShip(this);

        if (engineSound != null)
        {
            engineSound.Enabled = false;
            engineSound.Gain = 0.0f;
            engineSound.Loop = 1;
            engineSound.Stop();
        }
        if (submarineSonarSound != null)
        {
            submarineSonarSound.Enabled = false;
            submarineSonarSound.Loop = 1;
            submarineSonarSound.Stop();
        }
        if (hornSound != null)
        {
            hornSound.Enabled = false;
            hornSound.Loop = 0;
        }

        isShipSelectedInDelphi = false;
        currentZDepth = surfaceZPosition;
        dvec3 startPos = node.WorldPosition;
        startPos.z = currentZDepth;
        node.WorldPosition = startPos;
    }

    protected override void OnDisable()
    {
        targetThrottle = 0.0f; actualThrottle = 0.0f; targetTurn = 0.0f; actualTurn = 0.0f;
        isTurningLeft = false; isTurningRight = false; isCommandDive = false; isCommandSurface = false;

        if (engineSound != null) engineSound.Stop();
        if (submarineSonarSound != null) { submarineSonarSound.Stop(); submarineSonarSound.Loop = 0; }
        if (hornSound != null) hornSound.Stop();
    }

    // PARSING PERINTAH TCP JARINGAN KHUSUS UNTUK KENDALI LAMBUNG/KEMUDI KAPAL
    public void ProcessNetworkData(string rawData)
    {
        string[] parts = rawData.Split('|');
        if (parts.Length == 2 && parts[0].Trim().ToUpper() == shipID.ToUpper())
        {
            if (!isShipSelectedInDelphi)
            {
                isShipSelectedInDelphi = true;
                if (engineSound != null)
                {
                    engineSound.Enabled = true;
                    engineSound.Gain = idleVolume;
                    engineSound.Pitch = idlePitch;
                    engineSound.Play();
                }
                Log.Message($"[SYSTEM] {shipID} Connected from Delphi Combo Box.\n");
            }

            string command = parts[1].Trim().ToUpper();
            lock (lockObject)
            {
                if (command.Contains("THROTTLE:"))
                {
                    string[] valParts = command.Split(':');
                    if (valParts.Length > 1 && float.TryParse(valParts[1], out float val))
                        targetThrottle = val / 100.0f;
                }
                else if (command == "KIRI_START") isTurningLeft = true;
                else if (command == "KIRI_STOP") isTurningLeft = false;
                else if (command == "KANAN_START") isTurningRight = true;
                else if (command == "KANAN_STOP") isTurningRight = false;
                else if (command == "MENYELAM_START") { isCommandDive = true; isCommandSurface = false; }
                else if (command == "MENYELAM_STOP") { isCommandDive = false; }
                else if (command == "TERAPUNG_START") { isCommandSurface = true; isCommandDive = false; }
                else if (command == "TERAPUNG_STOP") { isCommandSurface = false; }
            }
        }
    }

    void Update()
    {
        float dt = Game.IFps;
        if (!isShipSelectedInDelphi) return;

        // 1. HITUNG INERSIA THROTTLE KAPAL UTAMA
        if (isShipSelectedInDelphi)
        {
            actualThrottle = MathLib.MoveTowards(actualThrottle, targetThrottle, accelerationFactor * dt);
        }
        else
        {
            actualThrottle = MathLib.MoveTowards(actualThrottle, 0.0f, accelerationFactor * dt);
        }

        // 2. HITUNG INERSIA BELOK KEMUDI
        lock (lockObject)
        {
            if (isTurningLeft && isShipSelectedInDelphi) targetTurn = 1.0f;
            else if (isTurningRight && isShipSelectedInDelphi) targetTurn = -1.0f;
            else targetTurn = 0.0f;
        }
        actualTurn = MathLib.MoveTowards(actualTurn, targetTurn, turnAccelerationFactor * dt);

        // TRANSLASI POSISI DAN ROTASI BALING-BALING KAPAL
        if (Math.Abs(actualThrottle) > 0.001f)
        {
            node.WorldTranslate(node.GetWorldDirection(MathLib.AXIS.Y) * (actualThrottle * maxMoveSpeed) * dt);
            if (propellerLeft != null) propellerLeft.Rotate(0, (actualThrottle * propRotationMultiplier) * dt, 0);
            if (propellerRight != null) propellerRight.Rotate(0, (actualThrottle * propRotationMultiplier) * dt, 0);
        }

        if (Math.Abs(actualTurn) > 0.001f) node.Rotate(0, 0, (actualTurn * maxTurnSpeed) * dt);

        // 3. LOGIKA KEDALAMAN KHUSUS KAPAL SELAM
        if (isSubmarine)
        {
            lock (lockObject)
            {
                if (isCommandDive) currentZDepth = MathLib.MoveTowards(currentZDepth, maxDiveDepth, diveSpeed * dt);
                else if (isCommandSurface) currentZDepth = MathLib.MoveTowards(currentZDepth, surfaceZPosition, diveSpeed * dt);
            }

            dvec3 worldPos = node.WorldPosition;
            worldPos.z = currentZDepth;
            node.WorldPosition = worldPos;
        }

        // 4. KONTROL AUDIO MESIN DINAMIS (LERP VOLUME & PITCH)
        if (engineSound != null)
        {
            if (!engineSound.IsPlaying)
            {
                engineSound.Enabled = true;
                engineSound.Play();
            }
            float throttleRatio = MathLib.Clamp(Math.Abs(actualThrottle), 0.0f, 1.0f);
            engineSound.Gain = MathLib.Lerp(idleVolume, maxGasVolume, throttleRatio);
            engineSound.Pitch = MathLib.Lerp(idlePitch, maxGasPitch, throttleRatio);
        }

        // 5. AUDIO SONAR JIKA KAPAL SELAM AMBLES DI BAWAH AIR
        if (isSubmarine && submarineSonarSound != null)
        {
            if (currentZDepth < surfaceZPosition - 15.0f)
            {
                if (!submarineSonarSound.IsPlaying)
                {
                    submarineSonarSound.Enabled = true;
                    submarineSonarSound.Stop();
                    submarineSonarSound.Play();
                }
            }
            else
            {
                if (submarineSonarSound.IsPlaying) submarineSonarSound.Stop();
                submarineSonarSound.Enabled = false;
            }
        }

        // 6. KLAKSON KAPAL (SPACE BAR KEYBOARD SIMULATOR)
        if (Input.IsKeyDown(Input.KEY.SPACE) && isShipSelectedInDelphi && hornSound != null)
        {
            hornSound.Enabled = true;
            hornSound.Stop();
            hornSound.Play();
        }
    }
}


// // ====================== Controller Beta ======================

// using System;
// using System.Globalization;
// using Unigine;

// public class ShipRemoteControl : Component
// {
//     [Parameter(Title = "ID Kapal")]
//     public string shipID = "KRI_RADJIMAN";
//     public float maxMoveSpeed = 15.5f;
//     public float accelerationFactor = 0.5f;
//     public float maxTurnSpeed = 15.0f;
//     public float turnAccelerationFactor = 1.5f;
//     [Parameter(Title = "Apakah Kapal Selam?", Tooltip = "Centang ini KHUSUS untuk KRI Alugoro")]
//     public bool isSubmarine = false;
//     public float diveSpeed = 2.0f;
//     public float maxDiveDepth = -15.0f;
//     public float surfaceZPosition = 0.0f;
//     public Node propellerLeft = null;
//     public Node propellerRight = null;
//     public float propRotationMultiplier = 600.0f;
//     [Parameter(Title = "Sound Mesin (Snd_fatahillah)")]
//     public SoundSource engineSound = null;
//     public float idleVolume = 0.3f;
//     public float maxGasVolume = 1.2f;
//     public float idlePitch = 0.7f;
//     public float maxGasPitch = 1.3f;
//     [Parameter(Title = "Sound Klakson (Snd_horn)")]
//     public SoundSource hornSound = null;
//     [Parameter(Title = "Sound Sonar (Snd_Alugoro)")]
//     public SoundSource submarineSonarSound = null;
//     [Parameter(Title = "35mm Base Node (Yaw)")]
//     public Node clws35BaseNode = null;
//     [Parameter(Title = "35mm Launcher Node (Pitch)")]
//     public Node clws35LauncherNode = null;
//     [Parameter(Title = "35mm Shot Effect")]
//     public Node clws35ShotEffect = null;
//     [Parameter(Title = "35mm Sound Effect")]
//     public SoundSource clws35Sound = null;
//     [Parameter(Title = "35mm Kecepatan Tracking")]
//     public float clws35TrackingSpeed = 60.0f;
//     [Parameter(Title = "35mm Jarak Recoil")]
//     public float clws35RecoilDistance = 0.15f;
//     [Parameter(Title = "Target Helicopter Node")]
//     public Node targetHelicopter = null;
//     private float targetThrottle = 0.0f;
//     private float actualThrottle = 0.0f;
//     private float targetTurn = 0.0f;
//     private float actualTurn = 0.0f;
//     private dvec3 initial35LauncherPos = dvec3.ZERO;
//     private bool hasRecorded35Pos = false;
//     private float current35Recoil = 0.0f;
//     private float target35Recoil = 0.0f;
//     private bool isTurningLeft = false;
//     private bool isTurningRight = false;
//     private bool isCommandDive = false;
//     private bool isCommandSurface = false;
//     private float currentZDepth = 0.0f;
//     private float radar35TargetYaw = 0.0f;
//     private float current35Yaw = 0.0f;
//     private float radar35TargetPitch = 0.0f;
//     private float current35Pitch = 0.0f;
//     private bool trigger35Fire = false;
//     private bool isAutoTrackingMode = false;
//     private readonly object lockObject = new object();
//     private bool isShipSelectedInDelphi = false;
//     protected override void OnReady()
//     {
//         ShipTCPManager.RegisterShip(this);

//         if (engineSound != null) { engineSound.Enabled = false; engineSound.Gain = 0.0f; engineSound.Loop = 1; engineSound.Stop(); }
//         if (submarineSonarSound != null) { submarineSonarSound.Enabled = false; submarineSonarSound.Loop = 1; submarineSonarSound.Stop(); }
//         if (hornSound != null) { hornSound.Enabled = false; hornSound.Loop = 0; }

//         if (clws35ShotEffect != null) clws35ShotEffect.Enabled = false;
//         if (clws35Sound != null) { clws35Sound.Enabled = false; clws35Sound.Loop = 0; clws35Sound.Stop(); }

//         isShipSelectedInDelphi = false;
//         currentZDepth = surfaceZPosition;
//         dvec3 startPos = node.WorldPosition;
//         startPos.z = currentZDepth;
//         node.WorldPosition = startPos;
//     }

//     protected override void OnDisable()
//     {
//         targetThrottle = 0.0f; actualThrottle = 0.0f; targetTurn = 0.0f; actualTurn = 0.0f;
//         isTurningLeft = false; isTurningRight = false; isCommandDive = false; isCommandSurface = false;
//         radar35TargetYaw = 0.0f; current35Yaw = 0.0f; radar35TargetPitch = 0.0f; current35Pitch = 0.0f;
//         trigger35Fire = false; isAutoTrackingMode = false;
//         current35Recoil = 0.0f; target35Recoil = 0.0f;

//         if (engineSound != null) engineSound.Stop();
//         if (submarineSonarSound != null) { submarineSonarSound.Stop(); submarineSonarSound.Loop = 0; }
//         if (hornSound != null) hornSound.Stop();
//         if (clws35Sound != null) clws35Sound.Stop();
//     }

//     public void ProcessNetworkData(string rawData)
//     {
//         string[] parts = rawData.Split('|');
//         if (parts.Length == 2 && parts[0].Trim().ToUpper() == shipID.ToUpper())
//         {
//             if (!isShipSelectedInDelphi)
//             {
//                 isShipSelectedInDelphi = true;
//                 if (engineSound != null) { engineSound.Enabled = true; engineSound.Gain = idleVolume; engineSound.Pitch = idlePitch; engineSound.Play(); }
//                 Log.Message($"[SYSTEM] {shipID} Connected from Delphi Combo Box.\n");
//             }

//             string command = parts[1].Trim().ToUpper();
//             lock (lockObject)
//             {
//                 if (command.Contains("THROTTLE:"))
//                 {
//                     string[] valParts = command.Split(':');
//                     if (valParts.Length > 1 && float.TryParse(valParts[1], out float val))
//                         targetThrottle = val / 100.0f;
//                 }
//                 else if (command == "KIRI_START") isTurningLeft = true;
//                 else if (command == "KIRI_STOP") isTurningLeft = false;
//                 else if (command == "KANAN_START") isTurningRight = true;
//                 else if (command == "KANAN_STOP") isTurningRight = false;
//                 else if (command == "MENYELAM_START") { isCommandDive = true; isCommandSurface = false; }
//                 else if (command == "MENYELAM_STOP") { isCommandDive = false; }
//                 else if (command == "TERAPUNG_START") { isCommandSurface = true; isCommandDive = false; }
//                 else if (command == "TERAPUNG_STOP") { isCommandSurface = false; }

//                 // KONTROL RADAR LOCK UNTUK KANON 35mm
//                 else if (command == "AUTO_TRACK_ON") isAutoTrackingMode = true;
//                 else if (command == "AUTO_TRACK_OFF") isAutoTrackingMode = false;

//                 else if (command == "ARTILERI_TEMBAK")
//                 {
//                     trigger35Fire = true; // Hanya memicu kanon 35mm CIWS di skrip lambung ini
//                 }
//             }
//         }
//     }

//     void Update()
//     {
//         float dt = Game.IFps;
//         if (!isShipSelectedInDelphi) return;

//         bool local35Fire;

//         lock (lockObject)
//         {
//             local35Fire = trigger35Fire;
//             trigger35Fire = false;
//         }

//         // PERGERAKAN KAPAL UTAMA
//         actualThrottle = MathLib.MoveTowards(actualThrottle, isShipSelectedInDelphi ? targetThrottle : 0.0f, accelerationFactor * dt);
//         lock (lockObject)
//         {
//             if (isTurningLeft && isShipSelectedInDelphi) targetTurn = 1.0f;
//             else if (isTurningRight && isShipSelectedInDelphi) targetTurn = -1.0f;
//             else targetTurn = 0.0f;
//         }

//         actualTurn = MathLib.MoveTowards(actualTurn, targetTurn, turnAccelerationFactor * dt);

//         if (Math.Abs(actualThrottle) > 0.001f)
//         {
//             node.WorldTranslate(node.GetWorldDirection(MathLib.AXIS.Y) * (actualThrottle * maxMoveSpeed) * dt);
//             if (propellerLeft != null) propellerLeft.Rotate(0, (actualThrottle * propRotationMultiplier) * dt, 0);
//             if (propellerRight != null) propellerRight.Rotate(0, (actualThrottle * propRotationMultiplier) * dt, 0);
//         }

//         if (Math.Abs(actualTurn) > 0.001f) node.Rotate(0, 0, (actualTurn * maxTurnSpeed) * dt);

//         if (isSubmarine)
//         {
//             lock (lockObject)
//             {
//                 if (isCommandDive) currentZDepth = MathLib.MoveTowards(currentZDepth, maxDiveDepth, diveSpeed * dt);
//                 else if (isCommandSurface) currentZDepth = MathLib.MoveTowards(currentZDepth, surfaceZPosition, diveSpeed * dt);
//             }

//             dvec3 worldPos = node.WorldPosition; worldPos.z = currentZDepth; node.WorldPosition = worldPos;
//         }

//         // Auto Tracking 35mm CIWS
//         if (isAutoTrackingMode && targetHelicopter != null && clws35BaseNode != null)
//         {
//             dvec3 directionToTarget = targetHelicopter.WorldPosition - clws35BaseNode.WorldPosition;
//             double angleYaw = Math.Atan2(-directionToTarget.x, directionToTarget.y) * MathLib.RAD2DEG;
//             double distance2D = Math.Sqrt(directionToTarget.x * directionToTarget.x + directionToTarget.y * directionToTarget.y);
//             double anglePitch = Math.Atan2(directionToTarget.z, distance2D) * MathLib.RAD2DEG;

//             radar35TargetYaw = (float)angleYaw;
//             radar35TargetPitch = (float)anglePitch;
//         }

//         if (clws35BaseNode != null)
//         {
//             current35Yaw = MathLib.MoveTowards(current35Yaw, isAutoTrackingMode ? radar35TargetYaw : 0.0f, clws35TrackingSpeed * dt);
//             clws35BaseNode.SetRotation(new quat(0, 0, current35Yaw));
//         }

//         if (clws35LauncherNode != null)
//         {
//             if (!hasRecorded35Pos)
//             {
//                 initial35LauncherPos = clws35LauncherNode.Position;
//                 hasRecorded35Pos = true;
//             }

//             current35Pitch = MathLib.MoveTowards(current35Pitch, isAutoTrackingMode ? radar35TargetPitch : 0.0f, clws35TrackingSpeed * dt);
//             clws35LauncherNode.SetRotation(new quat(current35Pitch, 0, 0));

//             if (current35Recoil != target35Recoil)
//             {
//                 current35Recoil = MathLib.MoveTowards(current35Recoil, target35Recoil, (target35Recoil > 0.001f ? 25.0f : 3.0f) * dt);
//                 if (MathF.Abs(current35Recoil - target35Recoil) < 0.001f) target35Recoil = 0.0f;
//                 clws35LauncherNode.Position = initial35LauncherPos;
//                 clws35LauncherNode.Translate(0.0f, -current35Recoil, 0.0f);
//             }

//         }

//         if (local35Fire && isAutoTrackingMode)
//         {
//             if (clws35ShotEffect != null)
//             {
//                 clws35ShotEffect.Enabled = false;
//                 clws35ShotEffect.Enabled = true;
//             }

//             if (clws35Sound != null)
//             {
//                 clws35Sound.Enabled = true;
//                 clws35Sound.Stop();
//                 clws35Sound.Play();
//             }

//             target35Recoil = clws35RecoilDistance;
//             Log.Message($"[ARTILLERY] 35mm Pew! {shipID} Menembak pada Yaw: {current35Yaw:F1}, Pitch: {current35Pitch:F1}\n");
//         }

//         // Audio Mesin Kapal
//         if (engineSound != null)
//         {
//             if (!engineSound.IsPlaying)
//             {
//                 engineSound.Enabled = true;
//                 engineSound.Play();
//             }

//             float throttleRatio = MathLib.Clamp(Math.Abs(actualThrottle), 0.0f, 1.0f);
//             engineSound.Gain = MathLib.Lerp(idleVolume, maxGasVolume, throttleRatio);
//             engineSound.Pitch = MathLib.Lerp(idlePitch, maxGasPitch, throttleRatio);
//         }

//         // Sonar Kapal Selam
//         if (isSubmarine && submarineSonarSound != null)
//         {
//             if (currentZDepth < surfaceZPosition - 15.0)
//             {
//                 if (!submarineSonarSound.IsPlaying)
//                 {
//                     submarineSonarSound.Enabled = true;
//                     submarineSonarSound.Stop();
//                     submarineSonarSound.Play();
//                 }
//             }

//             else
//             {
//                 if (submarineSonarSound.IsPlaying) submarineSonarSound.Stop();
//                 submarineSonarSound.Enabled = false;
//             }

//         }

//         // Klakson Kapal
//         if (Input.IsKeyDown(Input.KEY.SPACE) && isShipSelectedInDelphi && hornSound != null)
//         {
//             hornSound.Enabled = true;
//             hornSound.Stop();
//             hornSound.Play();
//         }

//     }

// }