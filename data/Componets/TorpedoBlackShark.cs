using System;
using Unigine;

[Component(PropertyGuid = "713d3b469e2c433ff293ab79dd65a6b5eba8a2f1")]
public class TorpedoBlackShark : Component
{
    public float launchPushSpeed = 50.0f;  // Dorongan awal keluar dari tabung kapal selam
    public float cruiseSpeed = 35.0f;      // Kecepatan jelajah penuh di dalam air (~50+ knot)
    public float accelerationRate = 5.0f; // Akselerasi mesin elektrik Black Shark
    public float turnSpeed = 2.5f;         // Cekatan dalam berbelok melacak target
    public float torpedoDepth = -10.0f;    // Kedalaman jelajah awal di bawah air (Z)
    [Parameter(Title = "Target Node")] public Node targetNode = null;
    [Parameter(Title = "Tombol Tembak")] public string keyBinding = "0"; // Default tombol 0
    [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
    [Parameter(Title = "Ledakan Target (explosion1)")] public Node explosionShipPrefab = null;
    [Parameter(Title = "Efek Buih Bawah Air (Trail)")] public Node waterTrailVFX = null;
    [Parameter(Title = "Sound Meluncur (Snd_meluncur)")] public SoundSource launchSound = null;
    [Parameter(Title = "Sound Ledakan (Snd_ledakan)")] public SoundSource explosionSound = null;

    private float currentSpeed = 0.0f;
    private bool isLaunched = false;
    private bool isExploded = false;
    private float flightTimer = 0.0f;

    private Node initialParent;
    private dvec3 initialLocalPosition;
    private quat initialLocalRotation;

    protected override void OnReady()
    {
        // Simpan referensi posisi di dalam tabung haluan KRI Alugoro
        initialParent = node.Parent;
        initialLocalPosition = node.Position;
        initialLocalRotation = node.GetRotation();

        ResetAllSystems();
    }

    void Update()
    {
        // Reset sistem menggunakan tombol R
        if (Input.IsKeyDown(Input.KEY.R))
        {
            ReloadTorpedo();
            return;
        }

        // Baca input tembak jika torpedo masih standby di tabung
        if (!isLaunched && !isExploded)
        {
            if (CheckInput()) LaunchTorpedo();
        }

        // Jalankan simulasi navigasi bawah air setelah meluncur
        if (isLaunched && !isExploded)
        {
            flightTimer += Game.IFps;
            MoveTorpedoSubmerged();
            CheckCollisionRaycast();
        }
    }

    private void LaunchTorpedo()
    {
        dvec3 worldPos = node.WorldPosition;
        quat worldRot = node.GetWorldRotation();

        node.Parent = null; // Lepas dari struktur KRI Alugoro
        node.WorldPosition = worldPos;
        node.SetWorldRotation(worldRot);

        isLaunched = true;
        flightTimer = 0.0f;
        currentSpeed = launchPushSpeed;

        // Aktifkan jejak buih air instan saat keluar tabung
        if (waterTrailVFX != null)
        {
            waterTrailVFX.Enabled = true;
            SetEmitterState(waterTrailVFX, true);
        }

        if (launchSound != null) { launchSound.Enabled = true; launchSound.Play(); }
        Log.Message($"[BLACK SHARK] Torpedo Heavyweight Launched from KRI Alugoro via Key {keyBinding}!\n");
    }

    private void MoveTorpedoSubmerged()
    {
        float dt = Game.IFps;
        vec3 targetDir;

        // Akselerasi motor elektrik bawah air menuju kecepatan penuh
        if (currentSpeed < cruiseSpeed) currentSpeed += accelerationRate * dt;

        if (targetNode != null)
        {
            vec3 currentPos = (vec3)node.WorldPosition;
            vec3 targetPos = (vec3)targetNode.WorldPosition;

            // Logika Homing: Jika target berada di permukaan, torpedo akan merayap naik secara gradual 
            // mendekati lambung bawah target saat jaraknya sudah dekat.
            float distToTarget = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
            float finalTargetDepth = targetPos.z;

            // Jika masih jauh, kunci kedalaman jelajah taktis (torpedoDepth)
            if (distToTarget > 150.0f)
            {
                finalTargetDepth = torpedoDepth;
            }

            vec3 targetHomingPos = new vec3(targetPos.x, targetPos.y, finalTargetDepth);
            targetDir = MathLib.Normalize(targetHomingPos - currentPos);
        }
        else
        {
            targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
        }

        // Navigasi dan translasi koordinat 3D
        vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
        vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
        node.WorldTranslate(smoothDir * currentSpeed * dt);
        node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
    }

    private void CheckCollisionRaycast()
    {
        // Jarak aman 1.5 detik agar tidak menabrak lambung haluan KRI Alugoro sendiri saat keluar tabung
        if (!isLaunched || isExploded || flightTimer < 1.5f) return;

        // Deteksi hantaman menggunakan Laser Intersection presisi menempel lambung
        vec3 p0 = (vec3)node.WorldPosition;
        vec3 p1 = p0 + (node.GetWorldDirection(MathLib.AXIS.Y) * (currentSpeed * Game.IFps + 3.0f));

        WorldIntersectionNormal hitInfo = new WorldIntersectionNormal();
        Node hitObj = World.GetIntersection(p0, p1, hitMask, hitInfo);

        if (hitObj != null)
        {
            // Deteksi jika mengenai bodi kapal target
            if (hitObj == targetNode || hitObj.IsChild(targetNode) || hitObj.Name.Contains("FATAHILLAH") || hitObj.Name.Contains("BELATI"))
            {
                node.WorldPosition = hitInfo.Point;

                ShipDamageHandler ship = hitObj.GetComponent<ShipDamageHandler>() ?? hitObj.GetComponentInParent<ShipDamageHandler>();
                if (ship != null) ship.ActivateDamage();

                ExplodeTorpedo(hitInfo.Point, hitObj);
                return;
            }
        }

        // Self-destruction jika torpedo melesat terlalu jauh tanpa mengenai sasaran (60 detik)
        if (flightTimer > 60.0f) ExplodeTorpedo(node.WorldPosition, null);
    }

    private void ExplodeTorpedo(dvec3 pos, Node hitParent)
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

        if (explosionShipPrefab != null)
        {
            Node instance = explosionShipPrefab.Clone();
            instance.Parent = hitParent;
            instance.WorldPosition = pos;
            instance.Enabled = true;
        }

        if (waterTrailVFX != null) StopVFXSmoothly(waterTrailVFX);
        node.WorldPosition = new dvec3(0, 0, -10000);
    }

    public void ReloadTorpedo()
    {
        isLaunched = false;
        isExploded = false;
        flightTimer = 0.0f;
        currentSpeed = 0.0f;

        node.Parent = initialParent;
        node.Position = (vec3)initialLocalPosition;
        node.SetRotation(initialLocalRotation);

        node.Enabled = false;
        node.Enabled = true;

        ResetAllSystems();
        Log.Message("[BLACK SHARK] Torpedo Reloaded inside Submarine Tube.\n");
    }

    private void StopVFXSmoothly(Node vfxNode)
    {
        if (vfxNode == null) return;
        ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
        if (p != null) { p.EmitterEnabled = false; vfxNode.Parent = null; }
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
        if (launchSound != null) launchSound.Stop();
        if (waterTrailVFX != null) { waterTrailVFX.Enabled = false; waterTrailVFX.Parent = node; waterTrailVFX.Position = vec3.ZERO; SetEmitterState(waterTrailVFX, true); }
    }

    private bool CheckInput()
    {
        string key = keyBinding.ToUpper().Trim();
        if (key == "F1") return Input.IsKeyDown(Input.KEY.F1);
        if (key == "F2") return Input.IsKeyDown(Input.KEY.F2);
        if (key == "F3") return Input.IsKeyDown(Input.KEY.F3);
        if (key == "F4") return Input.IsKeyDown(Input.KEY.F4);
        if (key == "F5") return Input.IsKeyDown(Input.KEY.F5);
        if (key == "F6") return Input.IsKeyDown(Input.KEY.F6);
        if (key == "F7") return Input.IsKeyDown(Input.KEY.F7);
        if (key == "F8") return Input.IsKeyDown(Input.KEY.F8);
        return false;
    }
}
