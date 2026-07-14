using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unigine;

[Component(PropertyGuid = "7c7aab7f39e8ce2941a70fdff7870aea2389239a")]
public class Gun127mmController : Component
{
    [Parameter(Title = "ID Kapal Pemilik", Tooltip = "Harus sama persis dengan nama ID Kapal di dropdown Delphi, misal: KRI_RE_MARTADINATA")]
    public string ownerShipID = "KRI_RE_MARTADINATA";

    [Parameter(Title = "127mm Base Node (Yaw)", Tooltip = "Node untuk rotasi horizontal")]
    public Node gun127BaseNode = null;

    [Parameter(Title = "127mm Launcher Node (Pitch)", Tooltip = "Node untuk elevasi vertikal laras")]
    public Node gun127LauncherNode = null;

    [Parameter(Title = "127mm Shot Effect (Muzzle Flash)")]
    public Node gun127ShotEffect = null;

    [Parameter(Title = "127mm Sound Effect (Audio Source Asli)")]
    public SoundSource gun127Sound = null;

    [Parameter(Title = "127mm Kecepatan Putar (Base)")]
    public float gun127RotationSpeed = 40.0f;

    [Parameter(Title = "127mm Kecepatan Elevasi (Laras)")]
    public float gun127ElevationSpeed = 32.0f;

    [Parameter(Title = "127mm Jarak Recoil (Meter)")]
    public float gun127RecoilDistance = 0.3f;

    private float manual127TargetYaw = 0.0f;
    private float current127Yaw = 0.0f;
    private float manual127TargetPitch = 0.0f;
    private float current127Pitch = 0.0f;
    private dvec3 initial127LauncherPos = dvec3.ZERO;
    private bool hasRecorded127Pos = false;
    private float current127Recoil = 0.0f;
    private float target127Recoil = 0.0f;
    private bool trigger127Fire = false;
    private readonly object lockObject = new object();
    private List<SoundSource> activeClonedSounds = new List<SoundSource>();

    protected override void OnReady()
    {
        if (gun127ShotEffect != null) gun127ShotEffect.Enabled = false;

        if (gun127Sound != null)
        {
            gun127Sound.Enabled = false;
            gun127Sound.Loop = 0;
            gun127Sound.Stop();
        }
    }

    protected override void OnDisable()
    {
        manual127TargetYaw = 0.0f; current127Yaw = 0.0f;
        manual127TargetPitch = 0.0f; current127Pitch = 0.0f;
        trigger127Fire = false; current127Recoil = 0.0f; target127Recoil = 0.0f;

        if (gun127Sound != null) gun127Sound.Stop();

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
                if (command == "MERIAM127_TEMBAK")
                {
                    trigger127Fire = true;
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
                            if (cmdName == "SET_MERIAM127_YAW")
                            {
                                manual127TargetYaw = parsedValue;
                            }
                            else if (cmdName == "SET_MERIAM127_PITCH")
                            {
                                manual127TargetPitch = parsedValue;
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

        float local127TargetYaw, local127TargetPitch;
        bool local127Fire;

        lock (lockObject)
        {
            local127TargetYaw = manual127TargetYaw;
            local127TargetPitch = manual127TargetPitch;
            local127Fire = trigger127Fire;
            trigger127Fire = false;
        }

        if (gun127BaseNode != null)
        {
            current127Yaw = MathLib.MoveTowards(current127Yaw, local127TargetYaw, gun127RotationSpeed * dt);
            gun127BaseNode.SetRotation(new quat(0, 0, current127Yaw));
        }

        if (gun127LauncherNode != null)
        {
            if (!hasRecorded127Pos)
            {
                initial127LauncherPos = gun127LauncherNode.Position;
                hasRecorded127Pos = true;
            }

            current127Pitch = MathLib.MoveTowards(current127Pitch, local127TargetPitch, gun127ElevationSpeed * dt);
            gun127LauncherNode.SetRotation(new quat(current127Pitch, 0, 0));

            if (current127Recoil != target127Recoil)
            {
                current127Recoil = MathLib.MoveTowards(current127Recoil, target127Recoil, (target127Recoil > 0.001f ? 15.0f : 2.0f) * dt);
                if (MathLib.Abs(current127Recoil - target127Recoil) < 0.001f) target127Recoil = 0.0f;

                gun127LauncherNode.Position = initial127LauncherPos;
                gun127LauncherNode.Translate(0.0f, -current127Recoil, 0.0f);
            }
        }

        if (local127Fire)
        {
            if (gun127ShotEffect != null) { gun127ShotEffect.Enabled = false; gun127ShotEffect.Enabled = true; }
            if (gun127Sound != null)
            {
                SoundSource clonedSound = gun127Sound.Clone() as SoundSource;
                if (clonedSound != null)
                {
                    clonedSound.Parent = gun127LauncherNode;
                    clonedSound.Position = vec3.ZERO;
                    clonedSound.Enabled = true;
                    clonedSound.Play();
                    activeClonedSounds.Add(clonedSound);
                }
            }

            target127Recoil = gun127RecoilDistance;

            if (gun127LauncherNode != null)
            {
                dvec3 p0 = gun127LauncherNode.WorldPosition;
                vec3 direction = gun127LauncherNode.GetWorldDirection(MathLib.AXIS.Y);
                dvec3 p1 = p0 + (dvec3)(direction * 5000.0f);

                WorldIntersection intersection = new WorldIntersection();
                Node hitNode = World.GetIntersection(p0, p1, 1, intersection);

                if (hitNode != null)
                {
                    dvec3 hitPoint = intersection.Point;
                    if (hitNode.Name.Contains("Golok") || hitNode.RootNode.Name.Contains("Golok") || hitNode.Name.Contains("Ship") || hitNode.Name.Contains("KRI"))
                    {
                        if (gun127ShotEffect != null)
                        {
                            Node instanceExplosion = gun127ShotEffect.Clone();
                            instanceExplosion.Parent = null;
                            instanceExplosion.WorldPosition = hitPoint;
                            instanceExplosion.Enabled = true;
                        }
                    }
                }
            }
        }

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
}