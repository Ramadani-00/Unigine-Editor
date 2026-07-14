// using System;
// using Unigine;

// [Component(PropertyGuid = "4a9edeeb46defb5206e94e7c245e2c1dd5aa9ee4")]
// public class SubmarineSonar : Component
// {
//     [Parameter(Title = "Sound Sonar (Snd_Alugoro)")]
//     public SoundSource sonarSound = null; // Tarik Snd_Alugoro ke sini
//     [Parameter(Title = "Jeda Ping Sonar (Detik)", Tooltip = "Berapa detik sekali suara sonar akan berbunyi Ping")]
//     public float pingInterval = 4.0f; // Default berbunyi setiap 3 detik sekali

//     private float sonarTimer = 0.0f;

//     protected override void OnReady()
//     {
//         // Persiapan awal audio saat simulasi dimulai
//         if (sonarSound != null)
//         {
//             sonarSound.Enabled = true;
//             sonarSound.Loop = 0; // KUNCI: Matikan loop hardware, kita atur jedanya manual lewat kodingan
//             sonarSound.Stop();

//             // Bunyikan "Ping" pertama kali saat simulasi dimulai
//             sonarSound.Play();
//             sonarTimer = 0.0f;
//         }
//     }

//     void Update()
//     {
//         if (sonarSound == null) return;

//         // Akumulasi timer setiap frame berjalan
//         sonarTimer += Game.IFps;

//         // TEPAT SAAT TIMER MENCAPAI JEDA INTERVAL
//         if (sonarTimer >= pingInterval)
//         {
//             // Paksa suara memutar ulang dari detik 0 ke depan
//             sonarSound.Stop();
//             sonarSound.Time = 0.0f;
//             sonarSound.Play();

//             // Reset timer internal kembali ke 0 untuk menghitung jeda Ping berikutnya
//             sonarTimer = 0.0f;

//             Log.Message("[SONAR] KRI Alugoro emitted a Sonar Ping.\n");
//         }
//     }

//     protected override void OnDisable()
//     {
//         // Matikan suara jika komponen atau kapal selam di-nonaktifkan
//         if (sonarSound != null) sonarSound.Stop();
//     }
// }

//==== Coba Baru ====

using System;
using Unigine;

[Component(PropertyGuid = "4a9edeeb46defb5206e94e7c245e2c1dd5aa9ee4")]
public class SubmarineSonar : Component
{
    [Parameter(Title = "Sound Sonar (Snd_Alugoro)")]
    public SoundSource sonarSound = null; // Tarik Snd_Alugoro ke sini

    [Parameter(Title = "Jeda Ping Sonar (Detik)", Tooltip = "Berapa detik sekali suara sonar akan berbunyi Ping")]
    public float pingInterval = 4.0f;

    private float sonarTimer = 0.0f;

    // --- KUNCI PERBAIKAN 1: TAMBAHKAN VARIABEL STATUS SELECTION ---
    private bool isSubmarineSelected = false;

    protected override void OnReady()
    {
        // --- KUNCI PERBAIKAN 2: AWAL START HARUS SENYAP ---
        // Sonar dilarang berbunyi saat Unigine baru di-load, tunggu sinyal Delphi masuk
        if (sonarSound != null)
        {
            sonarSound.Enabled = false;
            sonarSound.Loop = 0;
            sonarSound.Stop();
        }

        sonarTimer = 0.0f;
        isSubmarineSelected = false;
    }

    // --- KUNCI PERBAIKAN 3: FUNGSI AKTIVASI WAJIB PUBLIC AGAR REMOTE CONTROL BISA MEMANGGIL ---
    public void ActivateSonar()
    {
        if (!isSubmarineSelected)
        {
            isSubmarineSelected = true;
            if (sonarSound != null)
            {
                sonarSound.Enabled = true;
                sonarSound.Stop();
                sonarSound.Time = 0.0f;
                sonarSound.Play(); // Bunyikan "Ping" sambutan pertama kalinya
                sonarTimer = 0.0f;
                Log.Message("[SONAR] KRI Alugoro Sonar Activated via Remote Switch!\n");
            }
        }
    }

    void Update()
    {
        if (sonarSound == null) return;

        // KUNCI PERBAIKAN 4: JIKA BELUM DIPILIH DI DELPHI, JANGAN JALANKAN PING SONAR
        if (!isSubmarineSelected) return;

        // Akumulasi timer setiap frame berjalan
        sonarTimer += Game.IFps;

        // TEPAT SAAT TIMER MENCAPAI JEDA INTERVAL (Berulang setiap 4 detik)
        if (sonarTimer >= pingInterval)
        {
            if (sonarSound.IsPlaying) sonarSound.Stop();

            sonarSound.Time = 0.0f;
            sonarSound.Play();

            // Reset timer internal kembali ke 0 untuk menghitung jeda Ping berikutnya
            sonarTimer = 0.0f;

            Log.Message("[SONAR] Periodic Sonar Ping emitted.\n");
        }
    }

    protected override void OnDisable()
    {
        isSubmarineSelected = false;
        if (sonarSound != null) sonarSound.Stop();
    }
}
