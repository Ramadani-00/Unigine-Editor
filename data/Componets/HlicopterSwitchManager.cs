// using System.Collections;
// using System.Collections.Generic;
// using Unigine;

// [Component(PropertyGuid = "6dd818e554fc77ddf651871c449511e33432ccae")]
// public class HlicopterSwitchManager : Component
// {
//     [Parameter(Title = "Helikopter 1")]
//     public Node helicopter1 = null;

//     [Parameter(Title = "Helikopter 2")]
//     public Node helicopter2 = null;

//     private helicoptercontroller controller1 = null;
//     private helicoptercontroller controller2 = null;
//     private int activeIndex = 1; // 1 = Heli 1 Aktif, 2 = Heli 2 Aktif

//     protected override void OnReady()
//     {
//         if (helicopter1 != null) controller1 = helicopter1.GetComponent<helicoptercontroller>();
//         if (helicopter2 != null) controller2 = helicopter2.GetComponent<helicoptercontroller>();

//         ApplySwitch();
//         Log.Message("[HELICOPTER MANAGER] Siap. Tekan F10 untuk berganti helikopter.\n");
//     }

//     public void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.F10))
//         {
//             if (activeIndex == 1) activeIndex = 2;
//             else activeIndex = 1;

//             ApplySwitch();
//         }
//     }

//     private void ApplySwitch()
//     {
//         // KEDUA KOMPONEN TETAP AKTIF (Enabled = true), hanya variabel isControlled yang diubah!
//         if (activeIndex == 1)
//         {
//             if (controller1 != null) { controller1.Enabled = true; controller1.isControlled = true; }
//             if (controller2 != null) { controller2.Enabled = true; controller2.isControlled = false; }
//             Log.Message("[HELICOPTER MANAGER] Sekarang mengendalikan: HELIKOPTER 1\n");
//         }
//         else
//         {
//             if (controller1 != null) { controller1.Enabled = true; controller1.isControlled = false; }
//             if (controller2 != null) { controller2.Enabled = true; controller2.isControlled = true; }
//             Log.Message("[HELICOPTER MANAGER] Sekarang mengendalikan: HELIKOPTER 2\n");
//         }
//     }
// }