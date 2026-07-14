// ====================== Controller Gun76mm ==========================

using System;
using System.Globalization;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "cb17e0a5c87283287c23596ff7fb2bd1d535d0cc")]
public class Gun76mmController : Component
{
    [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama persis dengan nama ID Kapal di dropdown Delphi, misal: KRI_RE_MARTADINATA")]
    public string ownerShipID = "KRI_RE_MARTADINATA";
    [Parameter(Title = "76mm Base Node (Yaw)", Tooltip = "Node untuk rotasi horizontal")]
    public Node gun76BaseNode = null;
    [Parameter(Title = "76mm Launcher Node (Pitch)", Tooltip = "Node untuk elevasi vertikal laras")]
    public Node gun76LauncherNode = null;
    [Parameter(Title = "76mm Shot Effect (Muzzle Flash)")]
    public Node gun76ShotEffect = null;
    [Parameter(Title = "76mm Sound Effect (Audio Source Asli)")]
    public SoundSource gun76Sound = null;
    [Parameter(Title = "76mm Kecepatan Putar (Base)")]
    public float gun76RotationSpeed = 40.0f;
    [Parameter(Title = "76mm Kecepatan Elevasi (Laras)")]
    public float gun76ElevationSpeed = 32.0f;
    [Parameter(Title = "76mm Jarak Recoil (Meter)")]
    public float gun76RecoilDistance = 0.3f;
    private float manual76TargetYaw = 0.0f;
    private float current76Yaw = 0.0f;
    private float manual76TargetPitch = 0.0f;
    private float current76Pitch = 0.0f;
    private dvec3 initial76LauncherPos = dvec3.ZERO;
    private bool hasRecorded76Pos = false;
    private float current76Recoil = 0.0f;
    private float target76Recoil = 0.0f;
    private bool trigger76Fire = false;
    private readonly object lockObject = new object();
    private List<SoundSource> activeClonedSounds = new List<SoundSource>();

    protected override void OnReady()
    {
        if (gun76ShotEffect != null) gun76ShotEffect.Enabled = false;

        if (gun76Sound != null)
        {
            gun76Sound.Enabled = false;
            gun76Sound.Loop = 0;
            gun76Sound.Stop();
        }
    }

    protected override void OnDisable()
    {
        manual76TargetYaw = 0.0f; current76Yaw = 0.0f;
        manual76TargetPitch = 0.0f; current76Pitch = 0.0f;
        trigger76Fire = false; current76Recoil = 0.0f; target76Recoil = 0.0f;

        if (gun76Sound != null) gun76Sound.Stop();

        // Bersihkan sisa kloningan suara yang masih aktif saat skrip dimatikan
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
                if (command == "ARTILERI_TEMBAK")
                {
                    trigger76Fire = true;
                }
                else if (command.Contains(":"))
                {
                    string[] cmdValue = command.Split(':');
                    if (cmdValue.Length == 2)
                    {
                        string cmdName = cmdValue[0].Trim().ToUpper();
                        string cmdStringVal = cmdValue[1].Trim();

                        if (float.TryParse(cmdStringVal, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedValue))
                        {
                            if (cmdName == "SET_ARTILERI_YAW")
                            {
                                manual76TargetYaw = parsedValue;
                            }
                            else if (cmdName == "SET_ARTILERI_PITCH")
                            {
                                manual76TargetPitch = parsedValue;
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

        float local76TargetYaw, local76TargetPitch;
        bool local76Fire;

        lock (lockObject)
        {
            local76TargetYaw = manual76TargetYaw;
            local76TargetPitch = manual76TargetPitch;
            local76Fire = trigger76Fire;
            trigger76Fire = false;
        }

        if (gun76BaseNode != null)
        {
            current76Yaw = MathLib.MoveTowards(current76Yaw, local76TargetYaw, gun76RotationSpeed * dt);
            gun76BaseNode.SetRotation(new quat(0, 0, current76Yaw));
        }

        if (gun76LauncherNode != null)
        {
            if (!hasRecorded76Pos)
            {
                initial76LauncherPos = gun76LauncherNode.Position;
                hasRecorded76Pos = true;
            }

            current76Pitch = MathLib.MoveTowards(current76Pitch, local76TargetPitch, gun76ElevationSpeed * dt);
            gun76LauncherNode.SetRotation(new quat(current76Pitch, 0, 0));

            if (current76Recoil != target76Recoil)
            {
                current76Recoil = MathLib.MoveTowards(current76Recoil, target76Recoil, (target76Recoil > 0.001f ? 15.0f : 2.0f) * dt);
                if (MathF.Abs(current76Recoil - target76Recoil) < 0.001f) target76Recoil = 0.0f;

                gun76LauncherNode.Position = initial76LauncherPos;
                gun76LauncherNode.Translate(0.0f, -current76Recoil, 0.0f);
            }
        }

        if (local76Fire)
        {
            if (gun76ShotEffect != null) { gun76ShotEffect.Enabled = false; gun76ShotEffect.Enabled = true; }
            if (gun76Sound != null)
            {
                // Gandakan komponen audio bawaan secara instan di koordinat laras meriam
                SoundSource clonedSound = gun76Sound.Clone() as SoundSource;
                if (clonedSound != null)
                {
                    clonedSound.Parent = gun76LauncherNode; // Ikat ke moncong laras agar suara ikut bergerak dinamis
                    clonedSound.Position = vec3.ZERO;
                    clonedSound.Enabled = true;
                    clonedSound.Play(); // Mainkan secara mandiri tanpa memutus gelombang suara sebelumnya

                    activeClonedSounds.Add(clonedSound); // Masukkan ke daftar pantau pembersih
                }
            }

            target76Recoil = gun76RecoilDistance;
            Log.Message($"[ARTILLERY 76mm] Boom! {ownerShipID} fired shell manual. Yaw: {current76Yaw:F1}, Pitch: {current76Pitch:F1}. Audio instances: {activeClonedSounds.Count}\n");

            // EKSEKUSI PENEMBAKAN GARIS FISIK (RAYCAST)
            if (gun76LauncherNode != null)
            {
                dvec3 p0 = gun76LauncherNode.WorldPosition;
                vec3 direction = gun76LauncherNode.GetWorldDirection(MathLib.AXIS.Y);
                dvec3 p1 = p0 + (dvec3)(direction * 5000.0f);

                WorldIntersection intersection = new WorldIntersection();
                Node hitNode = World.GetIntersection(p0, p1, 1, intersection);

                if (hitNode != null)
                {
                    dvec3 hitPoint = intersection.Point;
                    Log.Message($"[COMBAT SYSTEM] Peluru 76mm menabrak objek: {hitNode.Name}\n");

                    if (hitNode.Name.Contains("Golok") || hitNode.RootNode.Name.Contains("Golok") || hitNode.Name.Contains("Ship") || hitNode.Name.Contains("KRI"))
                    {
                        Log.Message($"[COMBAT SYSTEM] ALERT: TARGET HIT! Sukses meledakkan lambung target {hitNode.Name}.\n");

                        if (gun76ShotEffect != null)
                        {
                            Node instanceExplosion = gun76ShotEffect.Clone();
                            instanceExplosion.Parent = null;
                            instanceExplosion.WorldPosition = hitPoint;
                            instanceExplosion.Enabled = true;
                        }
                    }
                }
            }
        }

        // PEMBERSIH AUDIO OTOMATIS: MENGHAPUS OBJEK KLON SUARA JIKA SELESAI BERBUNYI
        for (int i = activeClonedSounds.Count - 1; i >= 0; i--)
        {
            SoundSource snd = activeClonedSounds[i];
            if (snd != null && !snd.IsPlaying)
            {
                snd.Stop();
                snd.DeleteLater(); // Hapus dari memori game loop Unigine
                activeClonedSounds.RemoveAt(i);
            }
        }
    }
}