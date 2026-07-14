using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "057b4e56598fc953d4cf380fef7c9ed57d78fd10")]
public class ShipDamageHandler : Component
{
    [Parameter(Title = "Daftar Efek Asap")]
    public List<Node> smokeVFXList = new List<Node>();

    [Parameter(Title = "Jumlah Hit untuk Berasap & Tenggelam")]
    public int hitsToActivate = 1;

    [Parameter(Title = "Jarak Tenggelam (Meter)")]
    public float sinkDepth = 6.0f;

    [Parameter(Title = "Kecepatan Tenggelam")]
    public float sinkSpeed = 0.5f;

    // === TAMBAHAN PARAMETER BARU UNTUK DELAY ===
    [Parameter(Title = "Jeda Waktu Tenggelam (Detik)", Tooltip = "Berapa detik kapal menunggu setelah meledak sebelum mulai amblas tenggelam")]
    public float sinkDelayDuration = 3.0f;

    private int currentHitCount = 0;
    private bool isBurning = false;

    // Variabel internal untuk mengatur waktu jeda
    private bool isWaitingToSink = false;
    private float delayTimer = 0.0f;

    private BuoyancyKapal buoyancyScript = null;
    private float currentOffset = 0.0f;

    protected override void OnReady()
    {
        buoyancyScript = node.GetComponent<BuoyancyKapal>();
        if (buoyancyScript == null && node.Parent != null)
        {
            Node rootNode = node;
            while (rootNode.Parent != null) rootNode = rootNode.Parent;
            buoyancyScript = rootNode.GetComponent<BuoyancyKapal>();
        }

        foreach (Node smoke in smokeVFXList)
        {
            if (smoke != null) smoke.Enabled = false;
        }
    }

    void Update()
    {
        float dt = Game.IFps;

        // Hitung mundur waktu delay sebelum kapal mulai amblas
        if (isWaitingToSink)
        {
            delayTimer += dt;
            if (delayTimer >= sinkDelayDuration)
            {
                isWaitingToSink = false;
                isBurning = true; // Mulai jalankan proses amblas air
                Log.Message($"[SHIP] Waktu jeda selesai! Kapal mulai tenggelam sekarang.\n");
            }
        }

        // Hanya berjalan setelah status isBurning aktif
        if (isBurning && buoyancyScript != null)
        {
            currentOffset = MathLib.Lerp(currentOffset, sinkDepth, sinkSpeed * dt);
            buoyancyScript.waterHeightOffset = currentOffset;
        }
    }

    public void ActivateDamage()
    {
        currentHitCount++;
        Log.Message($"[SHIP] {node.Name} terkena Hit ke-{currentHitCount}\n");

        if (currentHitCount >= hitsToActivate)
        {
            StartBurning();
        }
    }

    public void StartBurning()
    {
        // Pastikan tidak mengeksekusi ulang jika sudah dalam fase menunggu atau terbakar
        if (isWaitingToSink || isBurning) return;

        if (buoyancyScript == null) OnReady();

        // LANGSUNG MATIKAN REMOTE CONTROL DAN NYALAKAN ASAP SAAT MELEDAK
        Component remoteControl = node.GetComponent<ShipRemoteControl>();
        if (remoteControl == null && node.Parent != null)
        {
            Node rootNode = node;
            while (rootNode.Parent != null) rootNode = rootNode.Parent;
            remoteControl = rootNode.GetComponent<ShipRemoteControl>();
        }

        if (remoteControl != null)
        {
            remoteControl.Enabled = false;
            Log.Message($"[SHIP] Komponen 'ShipRemoteControl' dinonaktifkan! Kapal langsung lumpuh.\n");
        }

        foreach (Node smoke in smokeVFXList)
        {
            if (smoke != null)
            {
                smoke.Enabled = true;
                SetEmitterState(smoke, true);
            }
        }

        isWaitingToSink = true;
        delayTimer = 0.0f;

        Log.Message($"[SHIP] Kapal meledak! Asap menyala, menunda proses tenggelam selama {sinkDelayDuration} detik...\n");
    }

    public void ResetDamage()
    {
        isBurning = false;
        isWaitingToSink = false;
        delayTimer = 0.0f;
        currentHitCount = 0;
        currentOffset = 0.0f;

        if (buoyancyScript != null)
        {
            buoyancyScript.waterHeightOffset = 0.0f;
        }

        Component remoteControl = node.GetComponent<ShipRemoteControl>();
        if (remoteControl == null && node.Parent != null)
        {
            Node rootNode = node;
            while (rootNode.Parent != null) rootNode = rootNode.Parent;
            remoteControl = rootNode.GetComponent<ShipRemoteControl>();
        }

        if (remoteControl != null)
        {
            remoteControl.Enabled = true;
        }

        foreach (Node smoke in smokeVFXList)
        {
            if (smoke != null) StopVFXSmoothly(smoke);
        }
        Log.Message($"[SHIP] Kapal diperbaiki, posisi air dan remote control kembali normal.\n");
    }

    private void StopVFXSmoothly(Node vfxNode)
    {
        if (vfxNode == null) return;
        ObjectParticles particles = vfxNode as ObjectParticles;
        if (particles == null && vfxNode.NumChildren > 0)
            particles = vfxNode.GetChild(0) as ObjectParticles;

        if (particles != null) particles.EmitterEnabled = false;
        else vfxNode.Enabled = false;
    }

    private void SetEmitterState(Node vfxNode, bool state)
    {
        if (vfxNode == null) return;
        ObjectParticles particles = vfxNode as ObjectParticles;
        if (particles == null && vfxNode.NumChildren > 0)
            particles = vfxNode.GetChild(0) as ObjectParticles;

        if (particles != null) particles.EmitterEnabled = state;
    }
}



// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "057b4e56598fc953d4cf380fef7c9ed57d78fd10")]
// public class ShipDamageHandler : Component
// {
//     [Parameter(Title = "Daftar Efek Asap")]
//     public List<Node> smokeVFXList = new List<Node>();

//     [Parameter(Title = "Jumlah Hit untuk Berasap & Tenggelam")]
//     public int hitsToActivate = 1; // Di-set 1 agar langsung bereaksi saat tertembak

//     [Parameter(Title = "Jarak Tenggelam (Meter)", Tooltip = "Seberapa dalam kapal amblas masuk ke dalam air")]
//     public float sinkDepth = 6.0f;

//     [Parameter(Title = "Kecepatan Tenggelam")]
//     public float sinkSpeed = 0.5f;

//     private int currentHitCount = 0;
//     private bool isBurning = false;

//     private BuoyancyKapal buoyancyScript = null;
//     private float currentOffset = 0.0f;

//     protected override void OnReady()
//     {
//         buoyancyScript = node.GetComponent<BuoyancyKapal>();

//         if (buoyancyScript == null && node.Parent != null)
//         {
//             Node rootNode = node;
//             while (rootNode.Parent != null)
//             {
//                 rootNode = rootNode.Parent;
//             }

//             buoyancyScript = rootNode.GetComponent<BuoyancyKapal>();
//         }

//         foreach (Node smoke in smokeVFXList)
//         {
//             if (smoke != null) smoke.Enabled = false;
//         }
//     }

//     void Update()
//     {
//         // Jika kapal terbakar, tambah offset tenggelam secara halus menggunakan Lerp
//         if (isBurning && buoyancyScript != null)
//         {
//             float dt = Game.IFps;

//             // Geser nilai offset secara perlahan menuju nilai sinkDepth target
//             currentOffset = MathLib.Lerp(currentOffset, sinkDepth, sinkSpeed * dt);

//             // Set hasilnya langsung ke script buoyancy kapal
//             buoyancyScript.waterHeightOffset = currentOffset;
//         }
//     }

//     public void ActivateDamage()
//     {
//         currentHitCount++;
//         Log.Message($"[SHIP] {node.Name} terkena Hit ke-{currentHitCount}\n");

//         if (currentHitCount >= hitsToActivate)
//         {
//             StartBurning();
//         }
//     }

//     public void StartBurning()
//     {
//         if (isBurning) return;
//         isBurning = true;

//         if (buoyancyScript == null) OnReady();

//         Component remoteControl = node.GetComponent<ShipRemoteControl>();

//         if (remoteControl == null && node.Parent != null)
//         {
//             Node rootNode = node;
//             while (rootNode.Parent != null) rootNode = rootNode.Parent;
//             remoteControl = rootNode.GetComponent<ShipRemoteControl>();
//         }

//         if (remoteControl != null)
//         {
//             remoteControl.Enabled = false;
//             Log.Message($"[SHIP] Komponen 'ShipRemoteControl' dinonaktifkan! Kapal lumpuh.\n");
//         }
//         else
//         {
//             Log.Warning($"[SHIP] Gagal menemukan komponen 'ShipRemoteControl' pada kapal target!\n");
//         }

//         foreach (Node smoke in smokeVFXList)
//         {
//             if (smoke != null)
//             {
//                 smoke.Enabled = true;
//                 SetEmitterState(smoke, true);
//             }
//         }
//         Log.Message($"[SHIP] Kerusakan berat aktif! Mengurangi daya apung secara bertahap.\n");
//     }

//     // public void StartBurning()
//     // {
//     //     if (isBurning) return;
//     //     isBurning = true;

//     //     if (buoyancyScript == null) OnReady();

//     //     foreach (Node smoke in smokeVFXList)
//     //     {
//     //         if (smoke != null)
//     //         {
//     //             smoke.Enabled = true;
//     //             SetEmitterState(smoke, true);
//     //         }
//     //     }
//     //     Log.Message($"[SHIP] Kerusakan berat aktif! Mengurangi daya apung secara bertahap.\n");
//     // }

//     public void ResetDamage()
//     {
//         isBurning = false;
//         currentHitCount = 0;
//         currentOffset = 0.0f;

//         if (buoyancyScript != null)
//         {
//             buoyancyScript.waterHeightOffset = 0.0f;
//         }

//         Component remoteControl = node.GetComponent<ShipRemoteControl>();
//         if (remoteControl == null && node.Parent != null)
//         {
//             Node rootNode = node;
//             while (rootNode.Parent != null) rootNode = rootNode.Parent;
//             remoteControl = rootNode.GetComponent<ShipRemoteControl>();
//         }

//         if (remoteControl != null)
//         {
//             remoteControl.Enabled = true;
//             Log.Message($"[SHIP] Komponen 'ShipRemoteControl' diaktifkan kembali.\n");
//         }

//         foreach (Node smoke in smokeVFXList)
//         {
//             if (smoke != null) StopVFXSmoothly(smoke);
//         }
//         Log.Message($"[SHIP] Kapal diperbaiki, posisi air kembali normal.\n");
//     }

//     // public void ResetDamage()
//     // {
//     //     isBurning = false;
//     //     currentHitCount = 0;
//     //     currentOffset = 0.0f;

//     //     if (buoyancyScript != null)
//     //     {
//     //         buoyancyScript.waterHeightOffset = 0.0f;
//     //     }

//     //     foreach (Node smoke in smokeVFXList)
//     //     {
//     //         if (smoke != null) StopVFXSmoothly(smoke);
//     //     }
//     //     Log.Message($"[SHIP] Kapal diperbaiki, posisi air kembali normal.\n");
//     // }

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

// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "057b4e56598fc953d4cf380fef7c9ed57d78fd10")]
// public class ShipDamageHandler : Component
// {
//     [Parameter(Title = "Daftar Efek Asap")]
//     public List<Node> smokeVFXList = new List<Node>();

//     [Parameter(Title = "Jumlah Hit untuk Berasap")]
//     public int hitsToActivate = 3;

//     private int currentHitCount = 0;
//     private bool isBurning = false;

//     protected override void OnReady()
//     {
//         // Matikan semua asap di awal simulasi
//         foreach (Node smoke in smokeVFXList)
//         {
//             if (smoke != null) smoke.Enabled = false;
//         }
//     }

//     // --- BAGIAN UPDATE DIHAPUS LOGIKA TIMER-NYA ---
//     void Update()
//     {
//         // Tidak perlu logika timer jika ingin menyala terus
//     }

//     public void ActivateDamage()
//     {
//         currentHitCount++;
//         Log.Message($"[SHIP] {node.Name} terkena Hit ke-{currentHitCount}\n");

//         // Jika hit sudah mencapai batas (misal 3 kali), nyalakan asap selamanya
//         if (currentHitCount >= hitsToActivate)
//         {
//             StartBurning();
//         }
//     }

//     public void StartBurning()
//     {
//         isBurning = true;
//         foreach (Node smoke in smokeVFXList)
//         {
//             if (smoke != null)
//             {
//                 smoke.Enabled = true;
//                 SetEmitterState(smoke, true);
//             }
//         }
//         Log.Message($"[SHIP] {node.Name}: Kerusakan berat! Asap aktif permanen.\n");
//     }

//     // Fungsi ini sekarang hanya dipanggil secara manual jika ingin memperbaiki kapal
//     public void ResetDamage()
//     {
//         isBurning = false;
//         currentHitCount = 0;
//         foreach (Node smoke in smokeVFXList)
//         {
//             if (smoke != null) StopVFXSmoothly(smoke);
//         }
//         Log.Message($"[SHIP] {node.Name}: Kapal diperbaiki, asap dimatikan.\n");
//     }

//     // --- FUNGSI HELPER ---
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
