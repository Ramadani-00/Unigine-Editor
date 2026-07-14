using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "e1f955eb5e4f78a288c37082601c289d00095a23")]
public class pagarkanan : Component
{
    [ShowInEditor] public Node gateNode;
    [ShowInEditor] public float openAngle = 90.0f;
    [ShowInEditor] public float closeAngle = 0.0f;
    [ShowInEditor] public float speed = 30.0f;

    private float currentAngle = 0.0f;
    private bool shouldOpen = false; // Status apakah pagar harus terbuka atau tertutup

    void Update()
    {
        // 1. Tombol "O" untuk Toggle (Buka/Tutup)
        if (Input.IsKeyDown(Input.KEY.O))
        {
            shouldOpen = !shouldOpen; // Balikkan status (jika true jadi false, dst)
            Log.Message(shouldOpen ? "Membuka...\n" : "Menutup...\n");
        }

        // 2. Logika Pergerakan Pagar
        if (shouldOpen && currentAngle < openAngle)
        {
            // Proses Membuka (Tambah Sudut)
            currentAngle += speed * Game.IFps;
            if (currentAngle > openAngle) currentAngle = openAngle;
        }
        else if (!shouldOpen && currentAngle > closeAngle)
        {
            // Proses Menutup (Kurangi Sudut)
            currentAngle -= speed * Game.IFps;
            if (currentAngle < closeAngle) currentAngle = closeAngle;
        }

        // 3. Terapkan Rotasi ke Node
        gateNode.SetRotation(new quat(0.0f, currentAngle, 0.0f));
    }
}