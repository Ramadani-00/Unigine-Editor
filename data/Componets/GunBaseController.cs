// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "c0b9c83046465fe34285d8872b436157bdc47326")]
// public class GunBaseController : Component
// {

//     // [Parameter(Group = "Input Settings")]
//     // public float angleN = 45.0f;

//     // [Parameter(Group = "Input Settings")]
//     // public float angleM = 300.0f;

//     [Parameter] public float nilaiInputM = 45.0f;
//     [Parameter] public float nilaiInputN = 300.0f;

//     private float targetAngle2D = 0.0f;

//     private void Update()
//     {

//         if (Input.IsKeyPressed(Input.KEY.M))
//         {
//             targetAngle2D = nilaiInputM;   // Mengambil langsung nilai apa pun yang diketik dieditor
//         }
//         else if (Input.IsKeyPressed(Input.KEY.N))
//         {
//             targetAngle2D = nilaiInputN;  // Mengambil langsung nilai apa pun yang diketik dieditor
//         }

//         // // Simulasi input tim 2D dengan keyboard
//         // if (Input.IsKeyPressed(Input.KEY.N))
//         // {
//         //     targetAngle2D = 45.0f;   // seumpama tim 2D kirim 45
//         // }
//         // else if (Input.IsKeyPressed(Input.KEY.M))
//         // {
//         //     targetAngle2D = 300.0f;  // seumpama tim 2D kirim 300
//         // }

//         // Normalisasi ke range -180..180
//         float normalizedAngle = MathLib.DeltaAngle(0.0f, targetAngle2D);
//         //Log.MessageLine(normalizedAngle);


//         // Balik tanda supaya positif = kanan
//         float finalAngle = -normalizedAngle;

//         // Terapkan rotasi pada sumbu Z
//         node.SetRotation(new quat(0, 0, finalAngle));
//     }


//     //     public float turnSpeed = 40.0f;

//     //     // Tentukan batas rotasi dalam derajat (misal: -45 sampai 45)
//     //     public float minAngle = -45.0f;
//     //     public float maxAngle = 45.0f;

//     //     // Variabel untuk melacak sudut rotasi saat ini
//     //     private float currentAngle = 0.0f;

//     //     private void Update()
//     //     {
//     //         // 45, 300
//     //         float ifps = Game.IFps;
//     //         float deltaRotation = 0;

//     //         // Cek input
//     //         if (Input.IsKeyPressed(Input.KEY.N))
//     //         {
//     //             deltaRotation = turnSpeed * ifps;
//     //         }
//     //         else if (Input.IsKeyPressed(Input.KEY.M))
//     //         {
//     //             deltaRotation = -turnSpeed * ifps;
//     //         }

//     //         if (deltaRotation != 0)
//     //         {
//     //             // Update nilai sudut dan batasi (Clamp) agar tetap di range min-max
//     //             currentAngle = MathLib.Clamp(currentAngle + deltaRotation, minAngle, maxAngle);

//     //             // Terapkan rotasi lokal pada sumbu Z berdasarkan currentAngle
//     //             // Menggunakan SetRotation (lokal) lebih aman untuk sistem batas daripada WorldRotation
//     //             node.SetRotation(new quat(0, 0, 1, currentAngle));
//     //         }
//     //     }
// }
using System;
using Unigine;

[Component(PropertyGuid = "c0b9c83046465fe34285d8872b436157bdc47326")]
public class GunBaseController : Component
{
    public float rotationSpeed = 60.0f;

    private float targetAngle = 0.0f;
    private float currentAngle = 0.0f;

    void Update()
    {
        float ifps = Game.IFps;

        // Smooth menuju target
        currentAngle = MathLib.Lerp(currentAngle, targetAngle, 5.0f * ifps);

        // ROTASI YANG BENAR (Z axis)
        node.SetRotation(new quat(new vec3(0, 0, 1), currentAngle));
    }

    // Dipanggil dari UI
    public void AddRotation(float value)
    {
        targetAngle += value;
    }

    public void SetRotationAngle(float value)
    {
        targetAngle = value;
    }
}