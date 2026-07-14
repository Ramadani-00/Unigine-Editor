using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "2a3d376d452c371271500f81d2d852d6e4d3d581")]
public class Foam : Component
{
    [ShowInEditor]
    private Node TempatFoam = null, foamm;

    [ShowInEditor]
    private FieldHeight bowWakeField_1 = null;

    [ShowInEditor]
    private FieldHeight bowWakeField_2 = null;

    private dvec3 lastPosition;
    private float currentSpeed = 0.0f;

    // Variabel internal untuk menampung komponen partikel busa
    private ObjectParticles foamParticles = null;
    private float maxEmissionRate = 0.0f;
    private float currentEmission = 0.0f;

    private void Init()
    {
        foamm.Enabled = true;
        foamm.GetChild(0).Enabled = true;
        foamm.WorldRotate(node.GetRotation());

        // Pastikan skala node tetap normal (1, 1, 1) agar tidak merusak sistem partikel
        foamm.WorldScale = vec3.ONE;

        lastPosition = node.WorldPosition;

        if (bowWakeField_1 != null) bowWakeField_1.Power = 0.0f;
        if (bowWakeField_2 != null) bowWakeField_2.Power = 0.0f;

        // Mencari komponen ObjectParticles di dalam node foamm secara otomatis
        foamParticles = foamm as ObjectParticles;
        if (foamParticles == null && foamm.GetChild(0) != null)
        {
            // Jika tidak ketemu di induknya, cari di anak pertamanya
            foamParticles = foamm.GetChild(0) as ObjectParticles;
        }

        if (foamParticles != null)
        {
            // Catat berapa jumlah semprotan partikel maksimal yang Anda atur di editor awalnya
            maxEmissionRate = foamParticles.SpawnRate;
            // Mulai dari angka 0 saat kapal diam di awal game
            foamParticles.SpawnRate = 0.0f;
        }
    }

    private void Update()
    {
        // 1. Logika pengaturan posisi busa bawaan Anda
        dvec3 checkpos = new dvec3(node.WorldPosition);
        checkpos.z += 10.419;
        checkpos.y += 200.0;

        foamm.WorldPosition = checkpos;
        foamm.GetChild(0).WorldPosition = TempatFoam.WorldPosition;
        foamm.GetChild(0).SetWorldRotation(TempatFoam.GetWorldRotation());

        // 2. HITUNG KECEPATAN DAN ARAH MAJU/MUNDUR
        dvec3 currentPosition = node.WorldPosition;
        double distanceMoved = MathLib.Distance(currentPosition, lastPosition);
        currentSpeed = (float)(distanceMoved / Game.IFps);

        vec3 moveDirection = vec3.ZERO;
        if (distanceMoved > 0.001)
        {
            moveDirection = (vec3)(currentPosition - lastPosition);
            moveDirection.Normalize();
        }

        vec3 shipForward = node.GetWorldDirection(MathLib.AXIS.Y);
        float directionCheck = MathLib.Dot(moveDirection, shipForward);

        lastPosition = currentPosition;

        // 3. HITUNG RASIO KECEPATAN UNTUK SKALA DINAMIS
        float maxSpeedLimit = 15.0f;
        float speedRatio = MathLib.Clamp(currentSpeed / maxSpeedLimit, 0.0f, 1.0f);

        // --- KONTROL BUSA BELAKANG (BISA MAJU / MUNDUR SECARA HALUS) ---
        if (foamParticles != null)
        {
            if (currentSpeed > 0.1f)
            {
                // Naikkan jumlah semprotan busa secara halus mengikuti kecepatan kapal
                float targetEmission = speedRatio * maxEmissionRate;
                currentEmission = MathLib.Lerp(currentEmission, targetEmission, Game.IFps * 1.5f);
            }
            else
            {
                // Turunkan jumlah semprotan busa ke angka 0 secara perlahan saat berhenti
                currentEmission = MathLib.Lerp(currentEmission, 0.0f, Game.IFps * 1.0f);
            }

            // Terapkan nilai emisi baru ke sistem partikel Unigine
            foamParticles.SpawnRate = currentEmission;
        }


        // --- KONTROL OMBAK DEPAN (HANYA AKTIF SAAT MAJU) ---
        if (currentSpeed > 0.1f && directionCheck > 0.1f)
        {
            float maxWavePower = 4.0f;
            float dynamicPower = speedRatio * maxWavePower;

            if (bowWakeField_1 != null)
                bowWakeField_1.Power = MathLib.Lerp(bowWakeField_1.Power, dynamicPower, Game.IFps * 2.0f);

            if (bowWakeField_2 != null)
                bowWakeField_2.Power = MathLib.Lerp(bowWakeField_2.Power, dynamicPower, Game.IFps * 2.0f);
        }
        else
        {
            if (bowWakeField_1 != null)
                bowWakeField_1.Power = MathLib.Lerp(bowWakeField_1.Power, 0.0f, Game.IFps * 2.0f);

            if (bowWakeField_2 != null)
                bowWakeField_2.Power = MathLib.Lerp(bowWakeField_2.Power, 0.0f, Game.IFps * 2.0f);
        }
    }
}


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "2a3d376d452c371271500f81d2d852d6e4d3d581")]
// public class Foam : Component
// {

//     // [ParameterFile(Filter = ".node")]
//     // public string foam;



//     [ShowInEditor]
//     private Node TempatFoam = null, foamm;

//     // private Node foamm;

//     private int Object_id;

//     private void Init()
//     {
//         // write here code to be called on component initialization
//         // foamm = World.LoadNode(foam);
//         // foamm.SetRotation(node.GetRotation());
//         foamm.Enabled = true;
//         foamm.GetChild(0).Enabled = true;
//         foamm.WorldRotate(node.GetRotation());
//         // foamm.Enabled = false;
//     }

//     private void Update()
//     {
//         // write here code to be called before updating each render frame

//         dvec3 checkpos = new dvec3(node.WorldPosition);
//         checkpos.z += 10.419;
//         checkpos.y += 200.0;
//         // checkpos.y += 50;

//         foamm.WorldPosition = checkpos;

//         foamm.GetChild(0).WorldPosition = TempatFoam.WorldPosition;
//         foamm.GetChild(0).SetWorldRotation(TempatFoam.GetWorldRotation());

//         // Object_id=LogicKapalKapal.getObjectID(node.ID);

//         // 	if(Object_id>=1)
//         // 	{
//         // 		ShipInfo k = LogicKapalKapal.getShipInfo(Object_id);			

//         // 		if(k != null){
//         // 			if(k.order_id == 0){

//         // 				// foamm.GetChild(0).Enabled = true;
//         // 			}
//         // 			// foamm.Enabled = true;
//         // 		}
//         // 	}


//         // Object_id=LogicKapalKapal.getObjectID(node.ID);

//         // 	if(Object_id>=1)
//         // 	{
//         // 		ShipInfo k = LogicKapalKapal.getShipInfo(Object_id);			

//         // 		if(k != null){

//         // 			if(k.order_id == 3){
//         // 				foamm.Enabled = false;
//         // 			}
//         // 			else{
//         // 			}
//         // 		}
//         // 	}

//         // FoamBesar.WorldPosition = checkpos;
//         // FoamBelakang.WorldPosition = TempatFoam.WorldPosition;
//         // FoamBelakang.SetWorldRotation(TempatFoam.GetWorldRotation());

//     }
// }