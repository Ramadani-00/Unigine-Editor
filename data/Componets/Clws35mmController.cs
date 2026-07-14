using System;
using System.Globalization;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "17f28ff7980ce8192733f191ea643e099a7c4916")]
public class Clws35mmController : Component
{
    [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama persis dengan nama ID Kapal di dropdown Delphi, misal: KRI_RE_MARTADINATA")]
    public string ownerShipID = "KRI_RE_MARTADINATA";
    [Parameter(Title = "35mm Base Node (Yaw)")]
    public Node clws35BaseNode = null;
    [Parameter(Title = "35mm Launcher Node (Pitch)")]
    public Node clws35LauncherNode = null;
    [Parameter(Title = "35mm Shot Effect (Muzzle Flash)")]
    public Node clws35ShotEffect = null;
    [Parameter(Title = "35mm Sound Effect (Audio Single Source)")]
    public SoundSource clws35Sound = null;
    [Parameter(Title = "35mm Kecepatan Tracking")]
    public float clws35TrackingSpeed = 60.0f;
    [Parameter(Title = "35mm Jarak Recoil (Meter)")]
    public float clws35RecoilDistance = 0.15f;
    [Parameter(Title = "Target Helicopter Node")]
    public Node targetHelicopter = null;
    private float radar35TargetYaw = 0.0f;
    private float current35Yaw = 0.0f;
    private float radar35TargetPitch = 0.0f;
    private float current35Pitch = 0.0f;
    private dvec3 initial35LauncherPos = dvec3.ZERO;
    private bool hasRecorded35Pos = false;
    private float current35Recoil = 0.0f;
    private float target35Recoil = 0.0f;
    private bool isAutoTrackingMode = false;
    private string fireMode = "SINGLE";
    private bool isHoldingTrigger = false;
    private float fireRateTimer = 0.0f;
    private float fireRateJeda = 0.07f;
    private List<SoundSource> activeClonedSounds = new List<SoundSource>();
    private readonly object lockObject = new object();

    protected override void OnReady()
    {
        if (clws35ShotEffect != null) clws35ShotEffect.Enabled = false;
        if (clws35Sound != null)
        {
            clws35Sound.Enabled = false;
            clws35Sound.Loop = 0;
            clws35Sound.Stop();
        }
    }

    protected override void OnDisable()
    {
        radar35TargetYaw = 0.0f; current35Yaw = 0.0f;
        radar35TargetPitch = 0.0f; current35Pitch = 0.0f;
        isAutoTrackingMode = false; isHoldingTrigger = false;
        current35Recoil = 0.0f; target35Recoil = 0.0f;

        if (clws35Sound != null) clws35Sound.Stop();

        // Matikan semua kloningan suara yang tersisa jika skrip dimatikan
        foreach (var snd in activeClonedSounds)
        {
            if (snd != null) { snd.Stop(); snd.DeleteLater(); }
        }
        activeClonedSounds.Clear();
    }

    public void ProcessNetworkData(string rawData)
    {
        string[] parts = rawData.Split('|');
        if (parts.Length == 2 && parts[0].Trim().ToUpper() == ownerShipID.ToUpper())
        {
            string command = parts[1].Trim().ToUpper();
            lock (lockObject)
            {
                if (command == "AUTO_TRACK_ON") isAutoTrackingMode = true;
                else if (command == "AUTO_TRACK_OFF") isAutoTrackingMode = false;
                else if (command == "MODE_35MM_SINGLE") fireMode = "SINGLE";
                else if (command == "MODE_35MM_BURST") fireMode = "BURST";
                else if (command == "CIWS_TEMBAK_START")
                {
                    isHoldingTrigger = true;
                    if (fireMode == "SINGLE") fireRateTimer = 0.0f;
                }
                else if (command == "CIWS_TEMBAK_STOP")
                {
                    isHoldingTrigger = false;
                }
            }
        }
    }

    void Update()
    {
        float dt = Game.IFps;

        bool localAutoTrack;
        string localFireMode;
        bool localHoldingTrigger;

        lock (lockObject)
        {
            localAutoTrack = isAutoTrackingMode;
            localFireMode = fireMode;
            localHoldingTrigger = isHoldingTrigger;
        }

        // REAL-TIME RADAR AUTO TRACKING ANTI-JITTER
        if (localAutoTrack && targetHelicopter != null && clws35BaseNode != null)
        {
            double distance = MathLib.Distance(clws35BaseNode.WorldPosition, targetHelicopter.WorldPosition);

            if (distance > 4500.0f) // Pembatasan jarak operasional radar meriam taktis
            {
                radar35TargetYaw = 0.0f;
                radar35TargetPitch = 0.0f;
            }
            else
            {

                dmat4 parentWorldMat = clws35BaseNode.Parent != null ? clws35BaseNode.Parent.WorldTransform : dmat4.IDENTITY;
                dmat4 worldToLocalMat = MathLib.Inverse(parentWorldMat);

                // Konversi koordinat helikopter dan pangkal meriam ke ruang tenang lokal kapal
                dvec3 localTargetPos = worldToLocalMat * targetHelicopter.WorldPosition;
                dvec3 localBasePos = worldToLocalMat * clws35BaseNode.WorldPosition;
                dvec3 finalLocalDir = localTargetPos - localBasePos;

                // Hitung sudut rotasi horizontal (Yaw) dan vertikal (Pitch) murni bebas interupsi perputaran laras
                double angleYaw = Math.Atan2(-finalLocalDir.x, finalLocalDir.y) * MathLib.RAD2DEG;
                double distance2D = Math.Sqrt(finalLocalDir.x * finalLocalDir.x + finalLocalDir.y * finalLocalDir.y);
                double anglePitch = Math.Atan2(finalLocalDir.z, distance2D) * MathLib.RAD2DEG;

                // Kunci batas aman agar laras dilarang berputar menusuk dek interior lambung sendiri
                radar35TargetYaw = MathLib.Clamp((float)angleYaw, -135.0f, 135.0f);
                radar35TargetPitch = MathLib.Clamp((float)anglePitch, -5.0f, 85.0f);
            }
        }
        else if (!localAutoTrack)
        {
            radar35TargetYaw = 0.0f;
            radar35TargetPitch = 0.0f;
        }

        // ROTASI BASE (YAW)
        if (clws35BaseNode != null)
        {
            current35Yaw = MathLib.MoveTowards(current35Yaw, radar35TargetYaw, clws35TrackingSpeed * dt);
            clws35BaseNode.SetRotation(new quat(0, 0, current35Yaw));
        }

        // ELEVASI LAUNCHER (PITCH) & RECOIL
        if (clws35LauncherNode != null)
        {
            if (!hasRecorded35Pos) { initial35LauncherPos = clws35LauncherNode.Position; hasRecorded35Pos = true; }
            current35Pitch = MathLib.MoveTowards(current35Pitch, radar35TargetPitch, clws35TrackingSpeed * dt);
            clws35LauncherNode.SetRotation(new quat(current35Pitch, 0, 0));

            if (current35Recoil != target35Recoil)
            {
                current35Recoil = MathLib.MoveTowards(current35Recoil, target35Recoil, (target35Recoil > 0.001f ? 25.0f : 3.0f) * dt);
                if (MathF.Abs(current35Recoil - target35Recoil) < 0.001f) target35Recoil = 0.0f;
                clws35LauncherNode.Position = initial35LauncherPos;
                clws35LauncherNode.Translate(0.0f, -current35Recoil, 0.0f);
            }
        }

        // SISTEM MANAJEMEN TIMING FIRE RATE JEDA PELURU
        if (fireRateTimer > 0.0f)
        {
            fireRateTimer -= dt;
        }

        if (localHoldingTrigger && localAutoTrack)
        {
            if (localFireMode == "SINGLE")
            {
                lock (lockObject) { isHoldingTrigger = false; }
                ExecuteWeaponFire();
            }
            else if (localFireMode == "BURST" && fireRateTimer <= 0.0f)
            {
                fireRateTimer = fireRateJeda;
                ExecuteWeaponFire();
            }
        }

        // MEMBERSIHKAN KLONINGAN AUDIO YANG SUDAH SELESAI BERBUNYI
        for (int i = activeClonedSounds.Count - 1; i >= 0; i--)
        {
            SoundSource snd = activeClonedSounds[i];
            if (snd != null && !snd.IsPlaying)
            {
                snd.Stop();
                snd.DeleteLater();
                activeClonedSounds.RemoveAt(i);
            }
        }
    }

    // EKSEKUSI KLONING AUDIO TAKTIS (POLYPHONIC MULTI-INSTANCING UNTUK BURST FIRE)
    private void ExecuteWeaponFire()
    {
        // Memicu efek api moncong meriam
        if (clws35ShotEffect != null)
        {
            clws35ShotEffect.Enabled = false;
            clws35ShotEffect.Enabled = true;
        }

        // SISTEM SOLUSI UTAMA: Gandakan komponen audio secara dinamis untuk tiap butir peluru
        if (clws35Sound != null)
        {
            // Gandakan komponen audio aslinya
            SoundSource clonedSound = clws35Sound.Clone() as SoundSource;
            if (clonedSound != null)
            {
                clonedSound.Parent = clws35LauncherNode;
                clonedSound.Position = vec3.ZERO;
                clonedSound.Enabled = true;
                clonedSound.Play();

                // Daftarkan ke list pembersih memori otomatis
                activeClonedSounds.Add(clonedSound);
            }
        }

        target35Recoil = clws35RecoilDistance;
        Log.Message($"[CIWS MILITARY] 35mm Fired. Mode: {fireMode}. Audio Stack Count: {activeClonedSounds.Count}\n");
    }
}