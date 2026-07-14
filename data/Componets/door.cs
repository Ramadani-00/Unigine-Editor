using Unigine;
using System;
using System.Collections;
using System.Collections.Generic;

[Component(PropertyGuid = "3090db8d15bc49f49fc6d86e38673f5d7dd2c6de")]
public class ShipDoorController : Component
{
    [ShowInEditor]
    public Node doorNode;

    [Parameter]
    public float openSpeed = 45.0f; // Kecepatan rotasi (derajat/detik)

    [Parameter]
    public float openAngle = 90f; // Sudut rebah (90 derajat = rata)

    public int doorType = 0; // 0 untuk pintu engsel atas, 1 untuk pintu engsel bawah

    private float currentAngle;

    public float startAngle; // Sudut awal pintu tertutup
    private bool isOpening = false;
    private bool isOpen = false;

    private void Init()
    {
        // console.WriteLine("");
        currentAngle = startAngle;
        // SetRampRotation(currentAngle);
        Unigine.Console.Run("background_update 0");

    }

    private void Update()
    {
        // Tekan O untuk buka/tutup
        if (Input.IsKeyDown(Input.KEY.O))
        {
            isOpening = !isOpen;
            isOpen = !isOpen;
        }

        if (doorNode == null) return;

        float delta = Game.IFps;

        if (isOpening)
        {
            if (currentAngle < openAngle)
            {
                currentAngle += openSpeed * delta;
                if (currentAngle > openAngle) currentAngle = openAngle;

                SetRampRotation(currentAngle);
            }
        }
        else
        {
            if (doorType == 0 && currentAngle > 0.0f)
            {
                currentAngle -= openSpeed * delta;
                if (currentAngle < 0.0f) currentAngle = 0.0f;

                SetRampRotation(currentAngle);
            }

            if (doorType == 1 && currentAngle > startAngle)
            {
                currentAngle -= openSpeed * delta;
                if (currentAngle < startAngle) currentAngle = startAngle;

                SetRampRotation(currentAngle);
            }
        }
    }

    void SetRampRotation(float angle)
    {
        // Menggunakan sumbu X untuk rotasi merebah ke bawah
        // Jika arah terbalik (malah ke atas), ganti 'angle' menjadi '-angle'
        if (doorType == 0)
        {
            quat rotation = new quat(angle, 0, 0);
            if (doorNode != null)
                doorNode.SetRotation(rotation);
        }

        if (doorType == 1)
        {
            quat rotation1 = new quat(angle, 0, 180);
            if (doorNode != null)
                doorNode.SetRotation(rotation1);
        }

    }
}
