using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "c1ff9081af909b4d49e9e250bbd093340fde981f")]
public class helicoptercontroller : Component
{
    [Parameter(Title = "Nama Identitas Heli (AS365N3 / NB412S)", Tooltip = "Wajib sama dengan teks pilihan di cbPilihHeli Delphi agar tidak salah sasaran")]
    public string keyBinding = "AS365N3";
    private Node rotorMain;
    private Node rotorTail;
    private Node ship;
    private Node helipad;
    private bool onDeck = true;
    private bool landing = false;
    private bool rotor_active = false;
    [Parameter(Title = "Kecepatan Auto Landing")]
    public float landing_speed = 3.0f;
    private float max_move_speed = 30.0f;
    private float max_lift_speed = 8.0f;
    private float max_turn_speed = 60.0f;
    private float current_move_speed = 0.0f;
    private float current_lift_speed = 0.0f;
    private float current_turn_speed = 0.0f;
    private float acceleration = 15.0f;
    private float deceleration = 10.0f;
    private float rotor_speed = 5000.0f;
    [Parameter(Title = "Sound Heli (Snd_heli)")]
    public SoundSource helicopterSound = null;
    [Parameter(Title = "Durasi File Audio (Detik)")]
    public float audioDuration = 81.0f;
    [Parameter(Title = "Waktu Pemotongan Akhir")]
    public float cutOffOffset = 0.5f;
    public float idleVolume = 0.2f;
    public float flyVolume = 1.2f;
    public float idlePitch = 0.6f;
    public float flyPitch = 1.3f;
    public float audioResponseSpeed = 2.0f;
    private float targetVolume = 0.0f;
    private float targetPitch = 1.0f;
    private float audioTimer = 0.0f;
    public bool isControlled = true;
    private bool isHit = false;
    private float crashSpinSpeed = 360.0f;
    private float crashSinkSpeed = 22.8f;
    [Parameter(Title = "VFX Asap Kebakaran Heli", Tooltip = "Masukkan node asap smoke_test ke sini")]
    public Node crashSmokeVFX = null;
    private bool netToggleMaju = false;
    private bool netToggleMundur = false;
    private bool netToggleKiri = false;
    private bool netToggleKanan = false;
    private bool netToggleNaik = false;
    private bool netToggleTurun = false;
    private bool netTriggerLanding = false;
    private readonly object lockObject = new object();

    public void Init()
    {
        ship = null;
        helipad = null;

        rotorMain = node.FindNode("rotorMain");
        rotorTail = node.FindNode("rotorTail");

        if (helicopterSound != null)
        {
            helicopterSound.Enabled = false;
            helicopterSound.Loop = 0;
            helicopterSound.Gain = 0.0f;
            helicopterSound.Stop();
        }

        targetVolume = 0.0f;
        targetPitch = idlePitch;
        audioTimer = 0.0f;

        isHit = false;

        lock (lockObject)
        {
            netToggleMaju = false; netToggleMundur = false;
            netToggleKiri = false; netToggleKanan = false;
            netToggleNaik = false; netToggleTurun = false;
            netTriggerLanding = false;
        }
    }

    public void ProcessNetworkData(string rawData)
    {
        string[] parts = rawData.Split('|');
        if (parts.Length == 2 && parts[0].Trim().ToUpper() == "KRI_RE_MARTADINATA")
        {
            string commandPayload = parts[1].Trim().ToUpper();

            if (commandPayload.Contains(":"))
            {
                string[] cmdValue = commandPayload.Split(':');
                if (cmdValue.Length == 2)
                {
                    string cmdName = cmdValue[0].Trim().ToUpper();
                    string targetHeliName = cmdValue[1].Trim().ToUpper();

                    // Validasi: Hanya jalankan perintah jika heli cocok dengan keyBinding di Editor
                    if (targetHeliName == keyBinding.Trim().ToUpper())
                    {
                        lock (lockObject)
                        {
                            if (cmdName == "HELI_TAKEOFF")
                            {
                                if (onDeck) PemicuTakeOffJaringan();
                            }
                            else if (cmdName == "HELI_TOGGLE_MAJU")
                            {
                                netToggleMaju = !netToggleMaju;
                                if (netToggleMaju) netToggleMundur = false; // Interlock keamanan gerakan
                            }
                            else if (cmdName == "HELI_TOGGLE_MUNDUR")
                            {
                                netToggleMundur = !netToggleMundur;
                                if (netToggleMundur) netToggleMaju = false;
                            }
                            else if (cmdName == "HELI_TOGGLE_KIRI")
                            {
                                netToggleKiri = !netToggleKiri;
                                if (netToggleKiri) netToggleKanan = false;
                            }
                            else if (cmdName == "HELI_TOGGLE_KANAN")
                            {
                                netToggleKanan = !netToggleKanan;
                                if (netToggleKanan) netToggleKiri = false;
                            }
                            else if (cmdName == "HELI_TOGGLE_NAIK")
                            {
                                netToggleNaik = !netToggleNaik;
                                if (netToggleNaik) netToggleTurun = false;
                            }
                            else if (cmdName == "HELI_TOGGLE_TURUN")
                            {
                                netToggleTurun = !netToggleTurun;
                                if (netToggleTurun) netToggleNaik = false;
                            }
                            else if (cmdName == "HELI_LANDING")
                            {
                                netTriggerLanding = true;
                            }
                        }
                    }
                }
            }
        }
    }

    private void PemicuTakeOffJaringan()
    {
        dvec3 world_pos = node.WorldPosition;
        quat world_rot = node.GetWorldRotation();
        node.Parent = null;
        node.WorldPosition = world_pos;
        node.SetWorldRotation(world_rot);
        onDeck = false;
        rotor_active = true;

        if (helicopterSound != null)
        {
            helicopterSound.Enabled = true;
            helicopterSound.Gain = idleVolume;
            helicopterSound.Pitch = idlePitch;
            helicopterSound.Stop();
            helicopterSound.Play();
            audioTimer = 0.0f;
        }
        Log.Message($"[NETWORK SYSTEM] Helicopter {keyBinding} Take-Off completed successfully.\n");
    }

    public void Update()
    {
        float ifps = Game.IFps;
        dvec3 pos = node.WorldPosition;

        vec3 forward = (vec3)node.WorldTransform.GetColumn3(1);
        vec3 right = (vec3)node.WorldTransform.GetColumn3(0);

        if (rotorMain != null && (rotor_active || landing || !onDeck))
        {
            quat rot = new quat(0, 0, 1, rotor_speed * ifps);
            rotorMain.SetWorldRotation(rotorMain.GetWorldRotation() * rot);
        }

        if (rotorTail != null && (rotor_active || landing || !onDeck))
        {
            quat rot = new quat(1, 0, 0, rotor_speed * ifps);
            rotorTail.SetWorldRotation(rotorTail.GetWorldRotation() * rot);
        }

        if (isHit)
        {
            quat spinRot = new quat(0, 0, 1, crashSpinSpeed * ifps);
            node.SetWorldRotation(node.GetWorldRotation() * spinRot);
            pos.z -= crashSinkSpeed * ifps;
            node.WorldPosition = pos;

            if (rotorMain != null)
            {
                quat rot = new quat(0, 0, 1, rotor_speed * ifps);
                rotorMain.SetWorldRotation(rotorMain.GetWorldRotation() * rot);
            }

            if (pos.z < -0.5f)
            {
                node.Enabled = false;
                if (helicopterSound != null) helicopterSound.Stop();
                Log.Message($"[HELICOPTER] Helikopter hancur total dan tenggelam ke dasar laut.\n");
            }
            return;
        }

        if (rotor_active)
        {
            if (isControlled)
            {
                targetVolume = onDeck ? idleVolume : flyVolume;
                targetPitch = onDeck ? idlePitch : flyPitch;
            }
            else
            {
                targetVolume = idleVolume;
                targetPitch = idlePitch;
            }
        }
        else
        {
            if (onDeck)
            {
                targetVolume = 0.0f;
                targetPitch = idlePitch;
            }
            else if (landing)
            {
                targetVolume = flyVolume;
                targetPitch = flyPitch;
            }
        }

        if (helicopterSound != null && helicopterSound.IsPlaying)
        {
            audioTimer += ifps;
            float safeSwitchTime = audioDuration - cutOffOffset;

            if (audioTimer >= safeSwitchTime)
            {
                helicopterSound.Time = 0.0f;
                audioTimer = 0.0f;
            }

            helicopterSound.Gain = MathLib.Lerp(helicopterSound.Gain, targetVolume, audioResponseSpeed * ifps);
            helicopterSound.Pitch = MathLib.Lerp(helicopterSound.Pitch, targetPitch, audioResponseSpeed * ifps);

            if (targetVolume == 0.0f && helicopterSound.Gain < 0.01f)
            {
                helicopterSound.Stop();
                helicopterSound.Enabled = false;
            }
        }

        if (!isControlled)
        {
            current_move_speed = MathLib.MoveTowards(current_move_speed, 0.0f, deceleration * ifps);
            current_lift_speed = MathLib.MoveTowards(current_lift_speed, 0.0f, deceleration * ifps);
            current_turn_speed = MathLib.MoveTowards(current_turn_speed, 0.0f, deceleration * 2.0f * ifps);

            if (MathF.Abs(current_move_speed) > 0.001f) pos += forward * current_move_speed * ifps;
            if (MathF.Abs(current_lift_speed) > 0.001f) pos.z += current_lift_speed * ifps;
            if (MathF.Abs(current_turn_speed) > 0.001f)
            {
                quat inertiaRot = new quat(0, 0, 1, current_turn_speed * ifps);
                node.SetWorldRotation(node.GetWorldRotation() * inertiaRot);
            }

            if (!onDeck && !landing) node.WorldPosition = pos;
            goto SkipControls;
        }

        if (!onDeck && !landing)
        {
            bool localNetMaju, localNetMundur, localNetKiri, localNetKanan, localNetNaik, localNetTurun, localNetLanding;
            lock (lockObject)
            {
                localNetMaju = netToggleMaju;
                localNetMundur = netToggleMundur;
                localNetKiri = netToggleKiri;
                localNetKanan = netToggleKanan;
                localNetNaik = netToggleNaik;
                localNetTurun = netToggleTurun;
                localNetLanding = netTriggerLanding;
                netTriggerLanding = false;
            }

            if (localNetLanding)
            {
                Node closestHelipad = FindClosestHelipad();
                if (closestHelipad != null)
                {
                    helipad = closestHelipad;
                    ship = closestHelipad.Parent;
                    landing = true;
                    rotor_active = false;
                    Log.Message($"[RADAR HELI] Mengunci target pendaratan di: {ship.Name} -> {helipad.Name}\n");
                    goto SkipControls;
                }
            }

            if (localNetMaju)
            {
                current_move_speed = MathLib.Clamp(current_move_speed + acceleration * ifps, -max_move_speed, max_move_speed);
            }

            else if (localNetMundur)
            {
                current_move_speed = MathLib.Clamp(current_move_speed - acceleration * ifps, -max_move_speed, max_move_speed);
            }

            else
            {
                if (current_move_speed > 0) current_move_speed = MathLib.Max(0.0f, current_move_speed - deceleration * ifps);
                if (current_move_speed < 0) current_move_speed = MathLib.Min(0.0f, current_move_speed + deceleration * ifps);
            }

            pos += forward * current_move_speed * ifps;
            if (localNetKiri)
            {
                current_turn_speed = MathLib.Clamp(current_turn_speed + acceleration * 2.0f * ifps, -max_turn_speed, max_turn_speed);
            }
            else if (localNetKanan)
            {
                current_turn_speed = MathLib.Clamp(current_turn_speed - acceleration * 2.0f * ifps, -max_turn_speed, max_turn_speed);
            }
            else
            {
                if (current_turn_speed > 0) current_turn_speed = MathLib.Max(0.0f, current_turn_speed - deceleration * 2.0f * ifps);
                if (current_turn_speed < 0) current_turn_speed = MathLib.Min(0.0f, current_turn_speed + deceleration * 2.0f * ifps);
            }

            quat rot = new quat(0, 0, 1, current_turn_speed * ifps);
            node.SetWorldRotation(node.GetWorldRotation() * rot);

            if (localNetNaik)
            {
                current_lift_speed = MathLib.Clamp(current_lift_speed + acceleration * ifps, -max_lift_speed, max_lift_speed);
            }
            else if (localNetTurun)
            {
                current_lift_speed = MathLib.Clamp(current_lift_speed - acceleration * ifps, -max_lift_speed, max_lift_speed);
            }
            else
            {
                if (current_lift_speed > 0) current_lift_speed = MathLib.Max(0.0f, current_lift_speed - deceleration * ifps);
                if (current_lift_speed < 0) current_lift_speed = MathLib.Min(0.0f, current_lift_speed + deceleration * ifps);
            }
            pos.z += current_lift_speed * ifps;
            node.WorldPosition = pos;
        }

    SkipControls:
        if (landing && helipad != null)
        {
            dvec3 heli_pos = node.WorldPosition;
            dvec3 pad_pos = helipad.WorldPosition;

            heli_pos.x = MathLib.MoveTowards(heli_pos.x, pad_pos.x, landing_speed * ifps);
            heli_pos.y = MathLib.MoveTowards(heli_pos.y, pad_pos.y, landing_speed * ifps);
            heli_pos.z = MathLib.MoveTowards(heli_pos.z, pad_pos.z, landing_speed * ifps);

            node.WorldPosition = heli_pos;
            float distanceToPad = (float)MathLib.Distance(node.WorldPosition, helipad.WorldPosition);
            if (distanceToPad < 0.1f)
            {
                node.Parent = helipad;
                node.Position = new vec3(0, 0, 0);
                node.SetRotation(new quat(0, 0, 0));
                onDeck = true;
                landing = false;
                current_move_speed = 0.0f;
                current_lift_speed = 0.0f;
                current_turn_speed = 0.0f;

                lock (lockObject)
                {
                    netToggleMaju = false;
                    netToggleMundur = false;
                    netToggleKiri = false;
                    netToggleKanan = false;
                    netToggleNaik = false;
                    netToggleTurun = false;
                    Log.Message($"[SYSTEM] Helicopter {keyBinding} landed smoothly on deck: {ship.Name}\n");
                }
            }
        }
    }
    public void Shutdown()
    {

    }

    private Node FindClosestHelipad()
    {
        List<Node> allNodes = new List<Node>();
        World.GetNodes(allNodes);
        Node bestTarget = null;
        double closestDistance = double.MaxValue;
        if (allNodes != null && allNodes.Count > 0)
        {
            foreach (Node n in allNodes)
            {
                if (n != null && n.Enabled && n.Name.Contains("Helipad"))
                {
                    double dist = MathLib.Distance(node.WorldPosition, n.WorldPosition);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestTarget = n;
                    }
                }
            }
        }

        return bestTarget;
    }

    public void TriggerCrash()
    {
        if (isHit) return;
        isHit = true;
        isControlled = false;
        landing = false;
        if (crashSmokeVFX != null)
        {
            crashSmokeVFX.Enabled = true;
            ObjectParticles particles = crashSmokeVFX as ObjectParticles;
            if (particles == null && crashSmokeVFX.NumChildren > 0) particles = crashSmokeVFX.GetChild(0) as ObjectParticles;
            if (particles != null) particles.EmitterEnabled = true;
        }
        Log.Message($"[MAYDAY] Helicopter was hit by VL MICA! Going down spinning!\n");
    }
}



// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "c1ff9081af909b4d49e9e250bbd093340fde981f")]
// public class helicoptercontroller : Component
// {
//     private Node rotorMain;
//     private Node rotorTail;
//     private Node ship;
//     private Node helipad;
//     private bool onDeck = true;
//     private bool landing = false;
//     private bool rotor_active = false;

//     [Parameter(Title = "Kecepatan Auto Landing")]
//     public float landing_speed = 3.0f;
//     private float max_move_speed = 30.0f;
//     private float max_lift_speed = 8.0f;
//     private float max_turn_speed = 60.0f;
//     private float current_move_speed = 0.0f;
//     private float current_lift_speed = 0.0f;
//     private float current_turn_speed = 0.0f;
//     private float acceleration = 15.0f;
//     private float deceleration = 10.0f;
//     private float rotor_speed = 5000.0f;

//     [Parameter(Title = "Sound Heli (Snd_heli)")]
//     public SoundSource helicopterSound = null;
//     [Parameter(Title = "Durasi File Audio (Detik)")]
//     public float audioDuration = 81.0f;
//     [Parameter(Title = "Waktu Pemotongan Akhir")]
//     public float cutOffOffset = 0.5f;
//     public float idleVolume = 0.2f;
//     public float flyVolume = 1.2f;
//     public float idlePitch = 0.6f;
//     public float flyPitch = 1.3f;
//     public float audioResponseSpeed = 2.0f;
//     private float targetVolume = 0.0f;
//     private float targetPitch = 1.0f;
//     private float audioTimer = 0.0f;
//     public bool isControlled = true;

//     private bool isHit = false;
//     private float crashSpinSpeed = 360.0f;
//     private float crashSinkSpeed = 22.8f;

//     [Parameter(Title = "VFX Asap Kebakaran Heli", Tooltip = "Masukkan node asap smoke_test ke sini")]
//     public Node crashSmokeVFX = null;

//     public void Init()
//     {
//         ship = null;
//         helipad = null;

//         rotorMain = node.FindNode("rotorMain");
//         rotorTail = node.FindNode("rotorTail");

//         if (helicopterSound != null)
//         {
//             helicopterSound.Enabled = false;
//             helicopterSound.Loop = 0;
//             helicopterSound.Gain = 0.0f;
//             helicopterSound.Stop();
//         }

//         targetVolume = 0.0f;
//         targetPitch = idlePitch;
//         audioTimer = 0.0f;

//         isHit = false;
//     }

//     public void Update()
//     {
//         float ifps = Game.IFps;
//         dvec3 pos = node.WorldPosition;

//         vec3 forward = (vec3)node.WorldTransform.GetColumn3(1);
//         vec3 right = (vec3)node.WorldTransform.GetColumn3(0);

//         // --- LOGIKA JATUH BERPUTAR JIKA TERKENA MISIL VL MICA ---
//         if (isHit)
//         {
//             // 1. Putar helikopter di sumbu Z (Yaw) secara terus-menerus
//             quat spinRot = new quat(0, 0, 1, crashSpinSpeed * ifps);
//             node.SetWorldRotation(node.GetWorldRotation() * spinRot);

//             // 2. Tarik helikopter meluncur jatuh ke bawah laut
//             pos.z -= crashSinkSpeed * ifps;
//             node.WorldPosition = pos;

//             // 3. Pastikan baling-baling utama tetap berputar visual agar realistis
//             if (rotorMain != null)
//             {
//                 quat rot = new quat(0, 0, 1, rotor_speed * ifps);
//                 rotorMain.SetWorldRotation(rotorMain.GetWorldRotation() * rot);
//             }

//             // 4. Jika helikopter masuk ke dalam air (koordinat z di bawah -0.5f), matikan objek
//             if (pos.z < -0.5f)
//             {
//                 node.Enabled = false;
//                 if (helicopterSound != null) helicopterSound.Stop();
//                 Log.Message($"[HELICOPTER] Helikopter hancur total dan tenggelam ke dasar laut.\n");
//             }

//             return; // Potong jalur eksekusi di sini agar input keyboard tidak bisa dipakai lagi
//         }

//         if (rotor_active)
//         {
//             if (isControlled)
//             {
//                 if (onDeck)
//                 {
//                     targetVolume = idleVolume;
//                     targetPitch = idlePitch;
//                 }
//                 else
//                 {
//                     targetVolume = flyVolume;
//                     targetPitch = flyPitch;
//                 }
//             }
//             else
//             {
//                 targetVolume = idleVolume;
//                 targetPitch = idlePitch;
//             }
//         }
//         else
//         {
//             if (onDeck)
//             {
//                 targetVolume = 0.0f;
//                 targetPitch = idlePitch;
//             }
//             else if (landing)
//             {
//                 targetVolume = flyVolume;
//                 targetPitch = flyPitch;
//             }
//         }

//         if (helicopterSound != null && helicopterSound.IsPlaying)
//         {
//             audioTimer += ifps;
//             float safeSwitchTime = audioDuration - cutOffOffset;

//             if (audioTimer >= safeSwitchTime)
//             {
//                 helicopterSound.Time = 0.0f;
//                 audioTimer = 0.0f;
//             }

//             helicopterSound.Gain = MathLib.Lerp(helicopterSound.Gain, targetVolume, audioResponseSpeed * ifps);
//             helicopterSound.Pitch = MathLib.Lerp(helicopterSound.Pitch, targetPitch, audioResponseSpeed * ifps);

//             if (targetVolume == 0.0f && helicopterSound.Gain < 0.01f)
//             {
//                 helicopterSound.Stop();
//                 helicopterSound.Enabled = false;
//             }
//         }

//         if (!isControlled)
//         {
//             current_move_speed = MathLib.MoveTowards(current_move_speed, 0.0f, deceleration * ifps);
//             current_lift_speed = MathLib.MoveTowards(current_lift_speed, 0.0f, deceleration * ifps);
//             current_turn_speed = MathLib.MoveTowards(current_turn_speed, 0.0f, deceleration * 2.0f * ifps);

//             if (MathF.Abs(current_move_speed) > 0.001f) pos += forward * current_move_speed * ifps;
//             if (MathF.Abs(current_lift_speed) > 0.001f) pos.z += current_lift_speed * ifps;
//             if (MathF.Abs(current_turn_speed) > 0.001f)
//             {
//                 quat inertiaRot = new quat(0, 0, 1, current_turn_speed * ifps);
//                 node.SetWorldRotation(node.GetWorldRotation() * inertiaRot);
//             }
//             if (!onDeck && !landing) node.WorldPosition = pos;

//             goto SkipKeyboardControls;
//         }

//         if (!onDeck && !landing)
//         {
//             if (Input.IsKeyPressed(Input.KEY.I))
//             {
//                 current_move_speed = MathLib.Clamp(current_move_speed + acceleration * ifps, -max_move_speed, max_move_speed);
//             }
//             else if (Input.IsKeyPressed(Input.KEY.K))
//             {
//                 current_move_speed = MathLib.Clamp(current_move_speed - acceleration * ifps, -max_move_speed, max_move_speed);
//             }
//             else
//             {
//                 if (current_move_speed > 0) current_move_speed = MathLib.Max(0.0f, current_move_speed - deceleration * ifps);
//                 if (current_move_speed < 0) current_move_speed = MathLib.Min(0.0f, current_move_speed + deceleration * ifps);
//             }
//             pos += forward * current_move_speed * ifps;

//             if (Input.IsKeyPressed(Input.KEY.J))
//             {
//                 current_turn_speed = MathLib.Clamp(current_turn_speed + acceleration * 2.0f * ifps, -max_turn_speed, max_turn_speed);
//             }
//             else if (Input.IsKeyPressed(Input.KEY.L))
//             {
//                 current_turn_speed = MathLib.Clamp(current_turn_speed - acceleration * 2.0f * ifps, -max_turn_speed, max_turn_speed);
//             }
//             else
//             {
//                 if (current_turn_speed > 0) current_turn_speed = MathLib.Max(0.0f, current_turn_speed - deceleration * 2.0f * ifps);
//                 if (current_turn_speed < 0) current_turn_speed = MathLib.Min(0.0f, current_turn_speed + deceleration * 2.0f * ifps);
//             }
//             quat rot = new quat(0, 0, 1, current_turn_speed * ifps);
//             node.SetWorldRotation(node.GetWorldRotation() * rot);

//             if (Input.IsKeyPressed(Input.KEY.F))
//             {
//                 current_lift_speed = MathLib.Clamp(current_lift_speed + acceleration * ifps, -max_lift_speed, max_lift_speed);
//             }
//             else if (Input.IsKeyPressed(Input.KEY.G))
//             {
//                 current_lift_speed = MathLib.Clamp(current_lift_speed - acceleration * ifps, -max_lift_speed, max_lift_speed);
//             }
//             else
//             {
//                 if (current_lift_speed > 0) current_lift_speed = MathLib.Max(0.0f, current_lift_speed - deceleration * ifps);
//                 if (current_lift_speed < 0) current_lift_speed = MathLib.Min(0.0f, current_lift_speed + deceleration * ifps);
//             }
//             pos.z += current_lift_speed * ifps;

//             node.WorldPosition = pos;
//         }

//         if (Input.IsKeyPressed(Input.KEY.V) && onDeck)
//         {
//             dvec3 world_pos = node.WorldPosition;
//             quat world_rot = node.GetWorldRotation();
//             node.Parent = null;
//             node.WorldPosition = world_pos;
//             node.SetWorldRotation(world_rot);
//             onDeck = false;
//             rotor_active = true;

//             if (helicopterSound != null)
//             {
//                 helicopterSound.Enabled = true;
//                 helicopterSound.Gain = idleVolume;
//                 helicopterSound.Pitch = idlePitch;
//                 helicopterSound.Stop();
//                 helicopterSound.Play();
//                 audioTimer = 0.0f;
//             }
//         }

//         if (Input.IsKeyPressed(Input.KEY.T) && !onDeck && !landing)
//         {
//             Node closestHelipad = FindClosestHelipad();

//             if (closestHelipad != null)
//             {
//                 helipad = closestHelipad;
//                 ship = closestHelipad.Parent;
//                 landing = true;
//                 rotor_active = false;
//                 Log.Message($"[RADAR HELI] Mengunci target pendaratan di: {ship.Name} -> {helipad.Name}\n");
//             }
//         }

//     SkipKeyboardControls:
//         if (landing && helipad != null)
//         {
//             dvec3 heli_pos = node.WorldPosition;
//             dvec3 pad_pos = helipad.WorldPosition;

//             heli_pos.x = MathLib.MoveTowards(heli_pos.x, pad_pos.x, landing_speed * ifps);
//             heli_pos.y = MathLib.MoveTowards(heli_pos.y, pad_pos.y, landing_speed * ifps);
//             heli_pos.z = MathLib.MoveTowards(heli_pos.z, pad_pos.z, landing_speed * ifps);

//             node.WorldPosition = heli_pos;
//             float distanceToPad = (float)MathLib.Distance(node.WorldPosition, helipad.WorldPosition);
//             if (distanceToPad < 0.1f)
//             {
//                 node.Parent = helipad;
//                 node.Position = new vec3(0, 0, 0);
//                 node.SetRotation(new quat(0, 0, 0));
//                 onDeck = true;
//                 landing = false;
//                 current_move_speed = 0.0f;
//                 current_lift_speed = 0.0f;
//                 current_turn_speed = 0.0f;
//                 Log.Message($"[SYSTEM] Helicopter landed smoothly on deck: {ship.Name}\n");
//             }
//         }

//         if (rotorMain != null && (rotor_active || landing))
//         {
//             quat rot = new quat(0, 0, 1, rotor_speed * ifps);
//             rotorMain.SetWorldRotation(rotorMain.GetWorldRotation() * rot);
//         }

//         if (rotorTail != null && (rotor_active || landing))
//         {
//             quat rot = new quat(1, 0, 0, rotor_speed * ifps);
//             rotorTail.SetWorldRotation(rotorTail.GetWorldRotation() * rot);
//         }
//     }

//     public void Shutdown()
//     {

//     }

//     private Node FindClosestHelipad()
//     {
//         List<Node> allNodes = new List<Node>();
//         World.GetNodes(allNodes);
//         Node bestTarget = null;
//         double closestDistance = double.MaxValue;
//         if (allNodes != null && allNodes.Count > 0)
//         {
//             foreach (Node n in allNodes)
//             {
//                 if (n != null && n.Enabled && n.Name.Contains("Helipad"))
//                 {
//                     double dist = MathLib.Distance(node.WorldPosition, n.WorldPosition);
//                     if (dist < closestDistance)
//                     {
//                         closestDistance = dist;
//                         bestTarget = n;
//                     }
//                 }
//             }
//         }

//         return bestTarget;
//     }

//     public void TriggerCrash()
//     {
//         if (isHit) return;
//         isHit = true;
//         isControlled = false;
//         landing = false;

//         if (crashSmokeVFX != null)
//         {
//             crashSmokeVFX.Enabled = true;
//             ObjectParticles particles = crashSmokeVFX as ObjectParticles;
//             if (particles == null && crashSmokeVFX.NumChildren > 0) particles = crashSmokeVFX.GetChild(0) as ObjectParticles;
//             if (particles != null) particles.EmitterEnabled = true;
//         }

//         Log.Message($"[MAYDAY] Helicopter was hit by VL MICA! Going down spinning!\n");
//     }
// }


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "c1ff9081af909b4d49e9e250bbd093340fde981f")]
// public class helicoptercontroller : Component
// {
//     private Node rotorMain;
//     private Node rotorTail;
//     private Node ship;
//     private Node helipad;
//     private bool onDeck = true;
//     private bool landing = false;
//     private bool rotor_active = false;
//     [Parameter(Title = "Kecepatan Auto Landing")]
//     public float landing_speed = 3.0f;
//     private float max_move_speed = 30.0f;
//     private float max_lift_speed = 8.0f;
//     private float max_turn_speed = 60.0f;
//     private float current_move_speed = 0.0f;
//     private float current_lift_speed = 0.0f;
//     private float current_turn_speed = 0.0f;
//     private float acceleration = 15.0f;
//     private float deceleration = 10.0f;
//     private float rotor_speed = 5000.0f;
//     [Parameter(Title = "Sound Heli (Snd_heli)")]
//     public SoundSource helicopterSound = null;
//     [Parameter(Title = "Durasi File Audio (Detik)")]
//     public float audioDuration = 81.0f;
//     [Parameter(Title = "Waktu Pemotongan Akhir")]
//     public float cutOffOffset = 0.5f;
//     public float idleVolume = 0.2f;
//     public float flyVolume = 1.2f;
//     public float idlePitch = 0.6f;
//     public float flyPitch = 1.3f;
//     public float audioResponseSpeed = 2.0f;
//     private float targetVolume = 0.0f;
//     private float targetPitch = 1.0f;
//     private float audioTimer = 0.0f;
//     public bool isControlled = true;

//     public void Init()
//     {
//         ship = null;
//         helipad = null;

//         rotorMain = node.FindNode("rotorMain");
//         rotorTail = node.FindNode("rotorTail");

//         if (helicopterSound != null)
//         {
//             helicopterSound.Enabled = false;
//             helicopterSound.Loop = 0;
//             helicopterSound.Gain = 0.0f;
//             helicopterSound.Stop();
//         }

//         targetVolume = 0.0f;
//         targetPitch = idlePitch;
//         audioTimer = 0.0f;
//     }

//     public void Update()
//     {
//         float ifps = Game.IFps;
//         dvec3 pos = node.WorldPosition;

//         vec3 forward = (vec3)node.WorldTransform.GetColumn3(1);
//         vec3 right = (vec3)node.WorldTransform.GetColumn3(0);

//         if (rotor_active)
//         {
//             if (isControlled)
//             {
//                 if (onDeck)
//                 {
//                     targetVolume = idleVolume;
//                     targetPitch = idlePitch;
//                 }
//                 else
//                 {
//                     targetVolume = flyVolume;
//                     targetPitch = flyPitch;
//                 }
//             }
//             else
//             {
//                 targetVolume = idleVolume;
//                 targetPitch = idlePitch;
//             }
//         }
//         else
//         {
//             if (onDeck)
//             {
//                 targetVolume = 0.0f;
//                 targetPitch = idlePitch;
//             }
//             else if (landing)
//             {
//                 targetVolume = flyVolume;
//                 targetPitch = flyPitch;
//             }
//         }

//         if (helicopterSound != null && helicopterSound.IsPlaying)
//         {
//             audioTimer += ifps;
//             float safeSwitchTime = audioDuration - cutOffOffset;

//             if (audioTimer >= safeSwitchTime)
//             {
//                 helicopterSound.Time = 0.0f;
//                 audioTimer = 0.0f;
//             }

//             helicopterSound.Gain = MathLib.Lerp(helicopterSound.Gain, targetVolume, audioResponseSpeed * ifps);
//             helicopterSound.Pitch = MathLib.Lerp(helicopterSound.Pitch, targetPitch, audioResponseSpeed * ifps);

//             if (targetVolume == 0.0f && helicopterSound.Gain < 0.01f)
//             {
//                 helicopterSound.Stop();
//                 helicopterSound.Enabled = false;
//             }
//         }

//         if (!isControlled)
//         {
//             current_move_speed = MathLib.MoveTowards(current_move_speed, 0.0f, deceleration * ifps);
//             current_lift_speed = MathLib.MoveTowards(current_lift_speed, 0.0f, deceleration * ifps);
//             current_turn_speed = MathLib.MoveTowards(current_turn_speed, 0.0f, deceleration * 2.0f * ifps);

//             if (MathF.Abs(current_move_speed) > 0.001f) pos += forward * current_move_speed * ifps;
//             if (MathF.Abs(current_lift_speed) > 0.001f) pos.z += current_lift_speed * ifps;
//             if (MathF.Abs(current_turn_speed) > 0.001f)
//             {
//                 quat inertiaRot = new quat(0, 0, 1, current_turn_speed * ifps);
//                 node.SetWorldRotation(node.GetWorldRotation() * inertiaRot);
//             }
//             if (!onDeck && !landing) node.WorldPosition = pos;

//             goto SkipKeyboardControls;
//         }

//         if (!onDeck && !landing)
//         {
//             if (Input.IsKeyPressed(Input.KEY.I))
//             {
//                 current_move_speed = MathLib.Clamp(current_move_speed + acceleration * ifps, -max_move_speed, max_move_speed);
//             }
//             else if (Input.IsKeyPressed(Input.KEY.K))
//             {
//                 current_move_speed = MathLib.Clamp(current_move_speed - acceleration * ifps, -max_move_speed, max_move_speed);
//             }
//             else
//             {
//                 if (current_move_speed > 0) current_move_speed = MathLib.Max(0.0f, current_move_speed - deceleration * ifps);
//                 if (current_move_speed < 0) current_move_speed = MathLib.Min(0.0f, current_move_speed + deceleration * ifps);
//             }
//             pos += forward * current_move_speed * ifps;

//             if (Input.IsKeyPressed(Input.KEY.J))
//             {
//                 current_turn_speed = MathLib.Clamp(current_turn_speed + acceleration * 2.0f * ifps, -max_turn_speed, max_turn_speed);
//             }
//             else if (Input.IsKeyPressed(Input.KEY.L))
//             {
//                 current_turn_speed = MathLib.Clamp(current_turn_speed - acceleration * 2.0f * ifps, -max_turn_speed, max_turn_speed);
//             }
//             else
//             {
//                 if (current_turn_speed > 0) current_turn_speed = MathLib.Max(0.0f, current_turn_speed - deceleration * 2.0f * ifps);
//                 if (current_turn_speed < 0) current_turn_speed = MathLib.Min(0.0f, current_turn_speed + deceleration * 2.0f * ifps);
//             }
//             quat rot = new quat(0, 0, 1, current_turn_speed * ifps);
//             node.SetWorldRotation(node.GetWorldRotation() * rot);

//             if (Input.IsKeyPressed(Input.KEY.F))
//             {
//                 current_lift_speed = MathLib.Clamp(current_lift_speed + acceleration * ifps, -max_lift_speed, max_lift_speed);
//             }
//             else if (Input.IsKeyPressed(Input.KEY.G))
//             {
//                 current_lift_speed = MathLib.Clamp(current_lift_speed - acceleration * ifps, -max_lift_speed, max_lift_speed);
//             }
//             else
//             {
//                 if (current_lift_speed > 0) current_lift_speed = MathLib.Max(0.0f, current_lift_speed - deceleration * ifps);
//                 if (current_lift_speed < 0) current_lift_speed = MathLib.Min(0.0f, current_lift_speed + deceleration * ifps);
//             }
//             pos.z += current_lift_speed * ifps;

//             node.WorldPosition = pos;
//         }

//         if (Input.IsKeyPressed(Input.KEY.V) && onDeck)
//         {
//             dvec3 world_pos = node.WorldPosition;
//             quat world_rot = node.GetWorldRotation();
//             node.Parent = null;
//             node.WorldPosition = world_pos;
//             node.SetWorldRotation(world_rot);
//             onDeck = false;
//             rotor_active = true;

//             if (helicopterSound != null)
//             {
//                 helicopterSound.Enabled = true;
//                 helicopterSound.Gain = idleVolume;
//                 helicopterSound.Pitch = idlePitch;
//                 helicopterSound.Stop();
//                 helicopterSound.Play();
//                 audioTimer = 0.0f;
//             }
//         }

//         if (Input.IsKeyPressed(Input.KEY.T) && !onDeck && !landing)
//         {
//             Node closestHelipad = FindClosestHelipad();

//             if (closestHelipad != null)
//             {
//                 helipad = closestHelipad;
//                 ship = closestHelipad.Parent;
//                 landing = true;
//                 rotor_active = false;
//                 Log.Message($"[RADAR HELI] Mengunci target pendaratan di: {ship.Name} -> {helipad.Name}\n");
//             }
//         }

//     SkipKeyboardControls:

//         if (landing && helipad != null)
//         {
//             dvec3 heli_pos = node.WorldPosition;
//             dvec3 pad_pos = helipad.WorldPosition;

//             heli_pos.x = MathLib.MoveTowards(heli_pos.x, pad_pos.x, landing_speed * ifps);
//             heli_pos.y = MathLib.MoveTowards(heli_pos.y, pad_pos.y, landing_speed * ifps);
//             heli_pos.z = MathLib.MoveTowards(heli_pos.z, pad_pos.z, landing_speed * ifps);

//             node.WorldPosition = heli_pos;
//             float distanceToPad = (float)MathLib.Distance(node.WorldPosition, helipad.WorldPosition);
//             if (distanceToPad < 0.1f)
//             {
//                 node.Parent = helipad;
//                 node.Position = new vec3(0, 0, 0);
//                 node.SetRotation(new quat(0, 0, 0));

//                 onDeck = true;
//                 landing = false;
//                 current_move_speed = 0.0f;
//                 current_lift_speed = 0.0f;
//                 current_turn_speed = 0.0f;

//                 Log.Message($"[SYSTEM] Helicopter landed smoothly on deck: {ship.Name}\n");
//             }

//         }

//         if (rotorMain != null && (rotor_active || landing))
//         {
//             quat rot = new quat(0, 0, 1, rotor_speed * ifps);
//             rotorMain.SetWorldRotation(rotorMain.GetWorldRotation() * rot);
//         }

//         if (rotorTail != null && (rotor_active || landing))
//         {
//             quat rot = new quat(1, 0, 0, rotor_speed * ifps);
//             rotorTail.SetWorldRotation(rotorTail.GetWorldRotation() * rot);
//         }

//     }

//     public void Shutdown()
//     {

//     }

//     private Node FindClosestHelipad()
//     {
//         List<Node> allNodes = new List<Node>();
//         World.GetNodes(allNodes);

//         Node bestTarget = null;
//         double closestDistance = double.MaxValue;

//         if (allNodes != null && allNodes.Count > 0)
//         {
//             foreach (Node n in allNodes)
//             {
//                 if (n != null && n.Enabled && n.Name.Contains("Helipad"))
//                 {
//                     double dist = MathLib.Distance(node.WorldPosition, n.WorldPosition);

//                     if (dist < closestDistance)
//                     {
//                         closestDistance = dist;
//                         bestTarget = n;
//                     }
//                 }
//             }
//         }

//         return bestTarget;
//     }

// }