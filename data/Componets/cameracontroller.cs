using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "37d88c1f6442a9bc360f57b74c108bab55b71d51")]
public class cameracontroller : Component
{
    private Node kameragolok;
    private Node BelatiCamera;
    private Node KameraRadjiman;
    private Node KameraBrawijaya;
    private Node kamerafatahillah;
    private Node KameraSAM;
    private Node KameraAlugoro;
    private Node Kameraheli;
    private Node KameraMartadinata;
    private Node as365n3;
    private bool isKeyLocked = false;
    public void Init()
    {
        kameragolok = World.GetNodeByName("kameragolok");
        BelatiCamera = World.GetNodeByName("BelatiCamera");
        KameraRadjiman = World.GetNodeByName("KameraRadjiman");
        KameraBrawijaya = World.GetNodeByName("KameraBrawijaya");
        KameraSAM = World.GetNodeByName("KameraSAM");
        kamerafatahillah = World.GetNodeByName("kamerafatahillah");
        KameraAlugoro = World.GetNodeByName("KameraAlugoro");
        Kameraheli = World.GetNodeByName("Kameraheli");
        KameraMartadinata = World.GetNodeByName("KameraMartadinata");
        as365n3 = World.GetNodeByName("as365n3");

        isKeyLocked = false;
    }

    public void Update()
    {

        bool anyKeyPressed = Input.IsKeyPressed(Input.KEY.DIGIT_1) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_2) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_3) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_4) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_5) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_6) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_7) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_8) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_9) ||
                             Input.IsKeyPressed(Input.KEY.DIGIT_0);

        if (!anyKeyPressed)
        {
            isKeyLocked = false;
        }

        if (isKeyLocked)
        {
            return;
        }

        if (Input.IsKeyDown(Input.KEY.DIGIT_1))
        {
            isKeyLocked = true;
            SwitchToCamera(kameragolok);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_2))
        {
            isKeyLocked = true;
            SwitchToCamera(KameraRadjiman);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_3))
        {
            isKeyLocked = true;
            SwitchToCamera(BelatiCamera);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_4))
        {
            isKeyLocked = true;
            SwitchToCamera(kamerafatahillah);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_5))
        {
            isKeyLocked = true;
            SwitchToCamera(KameraAlugoro);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_6))
        {
            isKeyLocked = true;
            SwitchToCamera(KameraMartadinata);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_7))
        {
            isKeyLocked = true;
            SwitchToCamera(Kameraheli);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_8))
        {
            isKeyLocked = true;
            SwitchToCamera(KameraBrawijaya);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_9))
        {
            isKeyLocked = true;
            SwitchToCamera(as365n3);
        }
        else if (Input.IsKeyDown(Input.KEY.DIGIT_0))
        {
            isKeyLocked = true;
            SwitchToCamera(KameraSAM);
        }
    }

    private void SwitchToCamera(Node cameraNode)
    {
        if (cameraNode != null && cameraNode is Player player)
        {
            // PROTEKSI TAMBAHAN: Jika kamera yang aktif saat ini sudah sama dengan target, langsung batalkan cetak log
            if (Game.Player == player)
            {
                return;
            }

            Game.Player = player;
            Log.Message($"[CAMERA] Berhasil pindah fokus ke kamera: {cameraNode.Name}\n");
        }
        else if (cameraNode == null)
        {
            Log.Warning("[CAMERA] Gagal berpindah! Node kamera target berstatus kosong (null).\n");
        }
    }
}


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "37d88c1f6442a9bc360f57b74c108bab55b71d51")]
// public class cameracontroller : Component
// {
//     private Node kameragolok;
//     private Node BelatiCamera;
//     private Node KameraRadjiman;
//     private Node KameraBrawijaya;
//     private Node kamerafatahillah;
//     private Node KameraSAM;
//     private Node KameraAlugoro;
//     private Node Kameraheli;
//     private Node KameraMartadinata;
//     private Node as365n3;
//     public void Init()
//     {
//         kameragolok = World.GetNodeByName("kameragolok");
//         BelatiCamera = World.GetNodeByName("BelatiCamera");
//         KameraRadjiman = World.GetNodeByName("KameraRadjiman");
//         KameraBrawijaya = World.GetNodeByName("KameraBrawijaya");
//         KameraSAM = World.GetNodeByName("KameraSAM");
//         kamerafatahillah = World.GetNodeByName("kamerafatahillah");
//         KameraAlugoro = World.GetNodeByName("KameraAlugoro");
//         Kameraheli = World.GetNodeByName("Kameraheli");
//         KameraMartadinata = World.GetNodeByName("KameraMartadinata");
//         as365n3 = World.GetNodeByName("as365n3");
//     }

//     public void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.DIGIT_1))
//         {
//             SwitchToCamera(kameragolok);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_2))
//         {
//             SwitchToCamera(KameraRadjiman);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_3))
//         {
//             SwitchToCamera(BelatiCamera);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_4))
//         {
//             SwitchToCamera(kamerafatahillah);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_5))
//         {
//             SwitchToCamera(KameraAlugoro);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_6))
//         {
//             SwitchToCamera(KameraMartadinata);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_7))
//         {
//             SwitchToCamera(Kameraheli);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_8))
//         {
//             SwitchToCamera(KameraBrawijaya);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_9))
//         {
//             SwitchToCamera(as365n3);
//         }

//         if (Input.IsKeyDown(Input.KEY.DIGIT_0))
//         {
//             SwitchToCamera(KameraSAM);
//         }

//     }

//     private void SwitchToCamera(Node cameraNode)
//     {
//         if (cameraNode != null && cameraNode is Player player)
//         {
//             Game.Player = player;
//             Log.Message($"[CAMERA] Berhasil pindah fokus ke kamera: {cameraNode.Name}\n");
//         }
//         else if (cameraNode == null)
//         {
//             Log.Warning("[CAMERA] Gagal berpindah! Node kamera target berstatus kosong (null).\n");
//         }
//     }
// }