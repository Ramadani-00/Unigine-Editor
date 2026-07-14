// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "ad5ca4d8c8e3d59cae4d3b0fac7f75ed7582378e")]
// public class GunLauncher : Component
// {
//     private Node launcher;
//     private Node gunFlash;

//     public float pitchSpeed = 30.0f;

//     // Batas Rotasi (Derajat)
//     public float maxUp = 45.0f;
//     public float maxDown = -10.0f;
//     private float currentPitch = 0.0f;

//     // Recoil
//     private vec3 startPos;
//     public float recoilDistance = 0.15f;
//     public float recoilSpeed = 5.0f;
//     private bool recoilActive = false;

//     private void Init()
//     {
//         launcher = node;
//         gunFlash = launcher.FindNode("artillery_shot");
//         startPos = (vec3)launcher.Position;

//         // Ambil sudut awal saat ini
//         currentPitch = launcher.GetRotation().GetAngle(new vec3(1, 0, 0));
//     }

//     private void Update()
//     {
//         float ifps = Game.IFps;

//         // --- Kontrol Pitch (Naik/Turun) ---
//         // Meriam Naik (Tombol R) - Hanya jalan jika belum lewat batas maxUp
//         if (Input.IsKeyPressed(Input.KEY.R) && currentPitch < maxUp)
//         {
//             float angle = pitchSpeed * ifps;
//             currentPitch += angle; // Catat perubahan sudut

//             quat rot = new quat(1, 0, 0, angle);
//             launcher.SetRotation(launcher.GetRotation() * rot);
//         }

//         // Meriam Turun (Tombol F) - Hanya jalan jika belum lewat batas maxDown
//         if (Input.IsKeyPressed(Input.KEY.F) && currentPitch > maxDown)
//         {
//             float angle = -pitchSpeed * ifps;
//             currentPitch += angle; // Catat perubahan sudut

//             quat rot = new quat(1, 0, 0, angle);
//             launcher.SetRotation(launcher.GetRotation() * rot);
//         }

//         // --- Logika Menembak ---
//         if (Input.IsKeyDown(Input.KEY.SPACE))
//         {
//             if (gunFlash != null)
//             {
//                 gunFlash.Enabled = false;
//                 gunFlash.Enabled = true;
//             }
//             recoilActive = true;
//         }

//         // --- Logika Recoil (Mundur-Maju) ---
//         dvec3 currentPos = launcher.Position;

//         if (recoilActive)
//         {
//             currentPos.y -= recoilDistance * ifps * recoilSpeed;
//             launcher.Position = currentPos;

//             if (currentPos.y <= startPos.y - recoilDistance)
//                 recoilActive = false;
//         }
//         else
//         {
//             launcher.Position = MathLib.Lerp(currentPos, startPos, ifps * recoilSpeed);
//         }
//     }
// }

using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "ad5ca4d8c8e3d59cae4d3b0fac7f75ed7582378e")]
public class GunLauncher : Component
{
    private Node launcher;
    private Node gunFlash;

    public float pitchSpeed = 30.0f;

    public float maxUp = 45.0f;
    public float maxDown = -10.0f;

    private float currentPitch = 0.0f;
    private float targetPitch = 0.0f;

    private vec3 startPos;
    public float recoilDistance = 0.15f;
    public float recoilSpeed = 5.0f;
    private bool recoilActive = false;

    private void Init()
    {
        launcher = node;
        gunFlash = launcher.FindNode("artillery_shot");
        startPos = (vec3)launcher.Position;
    }

    private void Update()
    {
        float ifps = Game.IFps;

        // ===== SMOOTH PITCH =====
        targetPitch = MathLib.Clamp(targetPitch, maxDown, maxUp);
        currentPitch = MathLib.Lerp(currentPitch, targetPitch, 5.0f * ifps);

        // FIX ROTASI
        launcher.SetRotation(new quat(new vec3(1, 0, 0), currentPitch));

        // ===== RECOIL =====
        dvec3 currentPos = launcher.Position;

        if (recoilActive)
        {
            currentPos.y -= recoilDistance * ifps * recoilSpeed;
            launcher.Position = currentPos;

            if (currentPos.y <= startPos.y - recoilDistance)
                recoilActive = false;
        }
        else
        {
            launcher.Position = MathLib.Lerp(currentPos, startPos, ifps * recoilSpeed);
        }
    }

    // ===== DIPANGGIL DARI UI =====
    public void AddPitch(float value)
    {
        targetPitch += value;
    }

    public void Fire()
    {
        if (gunFlash != null)
        {
            gunFlash.Enabled = false;
            gunFlash.Enabled = true;
        }
        recoilActive = true;
    }
}