using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "b3141bd76e1c924b64b83dbe2d9b51b46e7d168f")]
public class pagarkiri : Component
{
    [ShowInEditor] public Node gateNode;
    [ShowInEditor] public float openAngle = -90.0f;
    [ShowInEditor] public float closeAngle = 0.0f;
    [ShowInEditor] public float speed = 30.0f;

    private float currentAngle = 0.0f;
    private bool shouldOpen = false; // Status apakah pagar harus terbuka atau tertutup

    void Update()
    {
        if (Input.IsKeyDown(Input.KEY.O))
        {
            shouldOpen = !shouldOpen;
        }

        if (shouldOpen && currentAngle > openAngle) // Ganti < menjadi > karena targetnya negatif
        {
            // Karena bergerak ke arah negatif, kita gunakan pengurangan (-)
            currentAngle -= speed * Game.IFps;

            if (currentAngle < openAngle)
                currentAngle = openAngle;
        }
        else if (!shouldOpen && currentAngle < closeAngle) // Ganti > menjadi <
        {
            // Kembali ke 0 berarti angka harus ditambah (+)
            currentAngle += speed * Game.IFps;

            if (currentAngle > closeAngle)
                currentAngle = closeAngle;
        }

        gateNode.SetRotation(new quat(0.0f, currentAngle, 0.0f));
    }
}