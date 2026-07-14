// // using System;
// // using Unigine;

// // [Component(PropertyGuid = "f441cb609a7fc07426a453069e84559475069369")]
// // public class TorpedoLauncher : Component
// // {
// //     public float launchPushSpeed = 30.0f;  // Kecepatan horizontal saat keluar tabung
// //     public float cruiseSpeed = 25.0f;      // Kecepatan jelajah di dalam air (~50 knot)
// //     public float accelerationRate = 10.0f; // Akselerasi di dalam air
// //     public float turnSpeed = 2.0f;         // Kecepatan belok melacak lambung kapal musuh
// //     public float torpedoDepth = -2.0f;     // Kedalaman torpedo di bawah air (Z)
// //     [Parameter(Title = "Durasi Maju Horizontal", Tooltip = "Berapa detik torpedo meluncur ke depan sebelum langsung turun mendatar")]
// //     public float straightAirDuration = 0.5f;
// //     [Parameter(Title = "Kecepatan Jatuh Kebawah", Tooltip = "Kecepatan vertikal torpedo saat turun mendatar ke air")]
// //     public float dropSpeed = 12.0f;
// //     [Parameter(Title = "Target Node")] public Node targetNode = null;
// //     [Parameter(Title = "Tombol Tembak")] public string keyBinding = "9";
// //     [Parameter(Title = "Ledakan Kapal (explosion1)")] public Node explosionShipPrefab = null;
// //     [Parameter(Title = "Ledakan Air (water_explosion)")] public Node explosionWaterPrefab = null;
// //     [Parameter(Title = "Efek Buih Air (Trail)")] public Node waterTrailVFX = null;
// //     [Parameter(Title = "Sound Meluncur (Snd_meluncur)")] public SoundSource launchSound = null;
// //     [Parameter(Title = "Sound Masuk Air (Snd_masukair)")] public SoundSource entryWaterSound = null;
// //     [Parameter(Title = "Sound Ledakan (Snd_ledakan)")] public SoundSource explosionSound = null;


// //     private float currentSpeed = 0.0f;
// //     private bool isLaunched = false;
// //     private bool isExploded = false;
// //     private bool isInWater = false;
// //     private float flightTimer = 0.0f;

// //     private Node initialParent;
// //     private dvec3 initialLocalPosition;
// //     private quat initialLocalRotation;

// //     protected override void OnReady()
// //     {
// //         initialParent = node.Parent;
// //         initialLocalPosition = node.Position;
// //         initialLocalRotation = node.GetRotation();

// //         ResetAllSystems();
// //     }

// //     void Update()
// //     {
// //         if (Input.IsKeyDown(Input.KEY.R))
// //         {
// //             ReloadTorpedo();
// //             return;
// //         }

// //         if (!isLaunched && !isExploded)
// //         {
// //             if (CheckInput()) LaunchTorpedo();
// //         }

// //         if (isLaunched && !isExploded)
// //         {
// //             flightTimer += Game.IFps;
// //             MoveTorpedo();
// //             CheckCollision();
// //         }
// //     }

// //     private void LaunchTorpedo()
// //     {
// //         dvec3 worldPos = node.WorldPosition;
// //         quat worldRot = node.GetWorldRotation();

// //         node.Parent = null;
// //         node.WorldPosition = worldPos;
// //         node.SetWorldRotation(worldRot);

// //         isLaunched = true;
// //         isInWater = false;
// //         flightTimer = 0.0f;
// //         currentSpeed = launchPushSpeed;

// //         if (launchSound != null) { launchSound.Enabled = true; launchSound.Play(); }
// //         Log.Message($"[TORPEDO] MK46 Ejected Horizontal! Key: {keyBinding}\n");
// //     }

// //     private void MoveTorpedo()
// //     {
// //         float dt = Game.IFps;
// //         vec3 targetDir;

// //         // FASE 1: Di Udara (Keluar Tabung)
// //         if (!isInWater)
// //         {
// //             // Arah maju horizontal tetap sejajar dengan orientasi tabung (Sumbu Y positif)
// //             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

// //             if (flightTimer < straightAirDuration)
// //             {
// //                 // Bagian A: Maju lurus horizontal ke depan
// //                 node.WorldTranslate(targetDir * currentSpeed * dt);
// //             }
// //             else
// //             {
// //                 // Bagian B: Tetap bergerak maju + ditarik ke bawah (Turun mendatar / Flat Drop)
// //                 // KUNCI: Tidak merubah rotasi, bodi torpedo tetap horizontal sejajar air
// //                 node.WorldTranslate((targetDir * currentSpeed * dt) + (vec3.DOWN * dropSpeed * dt));
// //             }

// //             // Cek jika koordinat Z torpedo sudah menyentuh permukaan air laut (Z <= 0)
// //             if (node.WorldPosition.z <= 0.0f)
// //             {
// //                 isInWater = true;
// //                 currentSpeed = cruiseSpeed * 0.7f;

// //                 if (entryWaterSound != null) { entryWaterSound.Enabled = true; entryWaterSound.Play(); }

// //                 if (explosionWaterPrefab != null)
// //                 {
// //                     Node instance = explosionWaterPrefab.Clone();
// //                     instance.WorldPosition = node.WorldPosition;
// //                     instance.Enabled = true;
// //                 }

// //                 if (waterTrailVFX != null) { waterTrailVFX.Enabled = true; SetEmitterState(waterTrailVFX, true); }
// //                 Log.Message("[TORPEDO] MK46 Entered Water Horizonally.\n");
// //             }
// //         }
// //         // FASE 2: Di Dalam Air (Melacak Target Lambung)
// //         else
// //         {
// //             if (currentSpeed < cruiseSpeed) currentSpeed += accelerationRate * dt;

// //             if (targetNode != null)
// //             {
// //                 vec3 currentPos = (vec3)node.WorldPosition;
// //                 vec3 targetPos = (vec3)targetNode.WorldPosition;

// //                 vec3 submergedTargetPos = new vec3(targetPos.x, targetPos.y, torpedoDepth);
// //                 targetDir = MathLib.Normalize(submergedTargetPos - currentPos);
// //             }
// //             else
// //             {
// //                 targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
// //             }

// //             // Mengunci kedalaman torpedo secara konsisten di dalam air
// //             dvec3 currentWorldPos = node.WorldPosition;
// //             currentWorldPos.z = torpedoDepth;
// //             node.WorldPosition = currentWorldPos;

// //             // Eksekusi pergerakan melacak di dalam air
// //             vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
// //             vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
// //             node.WorldTranslate(smoothDir * currentSpeed * dt);
// //             node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
// //         }
// //     }

// //     private void CheckCollision()
// //     {
// //         if (!isLaunched || isExploded || flightTimer < 1.0f) return;

// //         if (targetNode != null)
// //         {
// //             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
// //             if (dist < 7.5f)
// //             {
// //                 ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
// //                 if (ship != null) ship.ActivateDamage();

// //                 ExplodeTorpedo(node.WorldPosition, targetNode, true);
// //                 return;
// //             }
// //         }

// //         if (flightTimer > 35.0f) ExplodeTorpedo(node.WorldPosition, null, false);
// //     }

// //     private void ExplodeTorpedo(dvec3 pos, Node hitParent, bool hitShip)
// //     {
// //         if (isExploded) return;
// //         isExploded = true;

// //         if (launchSound != null) launchSound.Stop();
// //         if (entryWaterSound != null) entryWaterSound.Stop();

// //         if (explosionSound != null)
// //         {
// //             SoundSource snd = explosionSound.Clone() as SoundSource;
// //             snd.Parent = hitParent;
// //             snd.WorldPosition = pos;
// //             snd.Enabled = true;
// //             snd.Play();
// //         }

// //         Node activeExplosionPrefab = hitShip ? explosionShipPrefab : explosionWaterPrefab;
// //         if (activeExplosionPrefab != null)
// //         {
// //             Node instance = activeExplosionPrefab.Clone();
// //             instance.Parent = hitParent;
// //             instance.WorldPosition = pos;
// //             instance.Enabled = true;
// //         }

// //         if (waterTrailVFX != null) StopVFXSmoothly(waterTrailVFX);
// //         node.WorldPosition = new dvec3(0, 0, -10000);
// //     }

// //     public void ReloadTorpedo()
// //     {
// //         isLaunched = false;
// //         isExploded = false;
// //         isInWater = false;
// //         flightTimer = 0.0f;
// //         currentSpeed = 0.0f;

// //         node.Parent = initialParent;
// //         node.Position = (vec3)initialLocalPosition;
// //         node.SetRotation(initialLocalRotation);

// //         node.Enabled = false;
// //         node.Enabled = true;

// //         ResetAllSystems();
// //         Log.Message("[TORPEDO] MK46 Reloaded.\n");
// //     }

// //     private void StopVFXSmoothly(Node vfxNode)
// //     {
// //         if (vfxNode == null) return;
// //         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
// //         if (p != null) { p.EmitterEnabled = false; vfxNode.Parent = null; }
// //         else vfxNode.Enabled = false;
// //     }

// //     private void SetEmitterState(Node vfxNode, bool state)
// //     {
// //         if (vfxNode == null) return;
// //         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
// //         if (p != null) p.EmitterEnabled = state;
// //     }

// //     private void ResetAllSystems()
// //     {
// //         if (launchSound != null) launchSound.Stop();
// //         if (entryWaterSound != null) entryWaterSound.Stop();
// //         if (waterTrailVFX != null) { waterTrailVFX.Enabled = false; waterTrailVFX.Parent = node; waterTrailVFX.Position = vec3.ZERO; SetEmitterState(waterTrailVFX, true); }
// //     }

// //     private bool CheckInput()
// //     {
// //         string key = keyBinding.ToUpper().Trim();
// //         if (key == "1") return Input.IsKeyDown(Input.KEY.DIGIT_1);
// //         if (key == "2") return Input.IsKeyDown(Input.KEY.DIGIT_2);
// //         if (key == "3") return Input.IsKeyDown(Input.KEY.DIGIT_3);
// //         if (key == "4") return Input.IsKeyDown(Input.KEY.DIGIT_4);
// //         if (key == "5") return Input.IsKeyDown(Input.KEY.DIGIT_5);
// //         if (key == "6") return Input.IsKeyDown(Input.KEY.DIGIT_6);
// //         // if (key == "7") return Input.IsKeyDown(Input.KEY.DIGIT_7);
// //         // if (key == "8") return Input.IsKeyDown(Input.KEY.DIGIT_8);
// //         // if (key == "9") return Input.IsKeyDown(Input.KEY.DIGIT_9);
// //         // if (key == "0") return Input.IsKeyDown(Input.KEY.DIGIT_0);
// //         return false;
// //     }
// // }

// //============ Versi Stabil =========

// // using System;
// // using Unigine;

// // [Component(PropertyGuid = "f441cb609a7fc07426a453069e84559475069369")]
// // public class TorpedoLauncher : Component
// // {
// //     public float launchPushSpeed = 30.0f;  // Kecepatan horizontal saat keluar tabung
// //     public float cruiseSpeed = 25.0f;      // Kecepatan jelajah di dalam air (~50 knot)
// //     public float accelerationRate = 1.0f; // Akselerasi di dalam air
// //     public float turnSpeed = 2.0f;         // Kecepatan belok melacak lambung kapal musuh
// //     public float torpedoDepth = -2.0f;     // Kedalaman torpedo di bawah air (Z)
// //     [Parameter(Title = "Durasi Maju Horizontal")] public float straightAirDuration = 0.5f;
// //     [Parameter(Title = "Kecepatan Jatuh Kebawah")] public float dropSpeed = 12.0f;
// //     [Parameter(Title = "Target Node")] public Node targetNode = null;
// //     [Parameter(Title = "Tombol Tembak")] public string keyBinding = "9";
// //     [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
// //     [Parameter(Title = "Ledakan Kapal (explosion1)")] public Node explosionShipPrefab = null;
// //     [Parameter(Title = "Ledakan Air (water_explosion)")] public Node explosionWaterPrefab = null;
// //     [Parameter(Title = "Efek Buih Air (Trail)")] public Node waterTrailVFX = null;

// //     // --- PARAMETER ASAP PELUNCURAN ---
// //     [Parameter(Title = "Asap Peluncuran (smoke_wisp_2)")] public Node launchSmokeVFX = null;
// //     [Parameter(Title = "Sound Meluncur (Snd_meluncur)")] public SoundSource launchSound = null;
// //     [Parameter(Title = "Sound Masuk Air (Snd_masukair)")] public SoundSource entryWaterSound = null;
// //     [Parameter(Title = "Sound Ledakan (Snd_ledakan)")] public SoundSource explosionSound = null;

// //     private float currentSpeed = 0.0f;
// //     private bool isLaunched = false;
// //     private bool isExploded = false;
// //     private bool isInWater = false;
// //     private float flightTimer = 0.0f;

// //     private Node initialParent;
// //     private dvec3 initialLocalPosition;
// //     private quat initialLocalRotation;

// //     protected override void OnReady()
// //     {
// //         initialParent = node.Parent;
// //         initialLocalPosition = node.Position;
// //         initialLocalRotation = node.GetRotation();

// //         ResetAllSystems();
// //     }

// //     void Update()
// //     {
// //         if (Input.IsKeyDown(Input.KEY.R))
// //         {
// //             ReloadTorpedo();
// //             return;
// //         }

// //         if (!isLaunched && !isExploded)
// //         {
// //             if (CheckInput()) LaunchTorpedo();
// //         }

// //         if (isLaunched && !isExploded)
// //         {
// //             flightTimer += Game.IFps;
// //             MoveTorpedo();
// //             CheckCollision();
// //         }
// //     }

// //     private void LaunchTorpedo()
// //     {
// //         // KUNCI UTAMA INSTAN: Aktifkan asap DI FRAME YANG SAMA saat tombol ditekan,
// //         // sebelum torpedo melepas kaitan parent agar posisi partikel awal menempel pas di lubang tabung.
// //         if (launchSmokeVFX != null)
// //         {
// //             launchSmokeVFX.Enabled = true;
// //             SetEmitterState(launchSmokeVFX, true);
// //         }

// //         dvec3 worldPos = node.WorldPosition;
// //         quat worldRot = node.GetWorldRotation();

// //         node.Parent = null;
// //         node.WorldPosition = worldPos;
// //         node.SetWorldRotation(worldRot);

// //         isLaunched = true;
// //         isInWater = false;
// //         flightTimer = 0.0f;
// //         currentSpeed = launchPushSpeed;

// //         if (launchSound != null) { launchSound.Enabled = true; launchSound.Play(); }
// //         Log.Message($"[TORPEDO] MK46 Ejected Horizontal with Instant Launch Smoke! Key: {keyBinding}\n");
// //     }

// //     private void MoveTorpedo()
// //     {
// //         float dt = Game.IFps;
// //         vec3 targetDir;

// //         // FASE 1: Di Udara (Keluar Tabung)
// //         if (!isInWater)
// //         {
// //             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

// //             if (flightTimer < straightAirDuration)
// //             {
// //                 node.WorldTranslate(targetDir * currentSpeed * dt);
// //             }
// //             else
// //             {
// //                 node.WorldTranslate((targetDir * currentSpeed * dt) + (vec3.DOWN * dropSpeed * dt));
// //             }

// //             if (node.WorldPosition.z <= 0.0f)
// //             {
// //                 isInWater = true;
// //                 currentSpeed = cruiseSpeed * 0.7f;

// //                 // Matikan asap peluncuran (smoke_wisp_2) secara halus saat tercebur air
// //                 if (launchSmokeVFX != null) StopVFXSmoothly(launchSmokeVFX);

// //                 if (entryWaterSound != null) { entryWaterSound.Enabled = true; entryWaterSound.Play(); }

// //                 if (explosionWaterPrefab != null)
// //                 {
// //                     Node instance = explosionWaterPrefab.Clone();
// //                     instance.WorldPosition = node.WorldPosition;
// //                     instance.Enabled = true;
// //                 }

// //                 if (waterTrailVFX != null) { waterTrailVFX.Enabled = true; SetEmitterState(waterTrailVFX, true); }
// //                 Log.Message("[TORPEDO] MK46 Entered Water. Launch Smoke Disabled, Water Trail Active.\n");
// //             }
// //         }
// //         // FASE 2: Di Dalam Air (Melacak Lambung Target)
// //         else
// //         {
// //             if (currentSpeed < cruiseSpeed) currentSpeed += accelerationRate * dt;

// //             if (targetNode != null)
// //             {
// //                 vec3 currentPos = (vec3)node.WorldPosition;
// //                 vec3 targetPos = (vec3)targetNode.WorldPosition;

// //                 vec3 submergedTargetPos = new vec3(targetPos.x, targetPos.y, torpedoDepth);
// //                 targetDir = MathLib.Normalize(submergedTargetPos - currentPos);
// //             }
// //             else
// //             {
// //                 targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
// //             }

// //             dvec3 currentWorldPos = node.WorldPosition;
// //             currentWorldPos.z = torpedoDepth;
// //             node.WorldPosition = currentWorldPos;

// //             vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
// //             vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
// //             node.WorldTranslate(smoothDir * currentSpeed * dt);
// //             node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
// //         }
// //     }

// //     private void CheckCollision()
// //     {
// //         if (!isLaunched || isExploded || flightTimer < 1.0f) return;

// //         if (targetNode != null)
// //         {
// //             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
// //             if (dist < 8.5f)
// //             {
// //                 ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
// //                 if (ship != null) ship.ActivateDamage();

// //                 ExplodeTorpedo(node.WorldPosition, targetNode, true);
// //                 return;
// //             }
// //         }

// //         if (flightTimer > 35.0f) ExplodeTorpedo(node.WorldPosition, null, false);
// //     }

// //     private void ExplodeTorpedo(dvec3 pos, Node hitParent, bool hitShip)
// //     {
// //         if (isExploded) return;
// //         isExploded = true;

// //         if (launchSound != null) launchSound.Stop();
// //         if (entryWaterSound != null) entryWaterSound.Stop();

// //         if (explosionSound != null)
// //         {
// //             SoundSource snd = explosionSound.Clone() as SoundSource;
// //             snd.Parent = hitParent;
// //             snd.WorldPosition = pos;
// //             snd.Enabled = true;
// //             snd.Play();
// //         }

// //         Node activeExplosionPrefab = hitShip ? explosionShipPrefab : explosionWaterPrefab;
// //         if (activeExplosionPrefab != null)
// //         {
// //             Node instance = activeExplosionPrefab.Clone();
// //             instance.Parent = hitParent;
// //             instance.WorldPosition = pos;
// //             instance.Enabled = true;
// //         }

// //         if (launchSmokeVFX != null) StopVFXSmoothly(launchSmokeVFX);
// //         if (waterTrailVFX != null) StopVFXSmoothly(waterTrailVFX);
// //         node.WorldPosition = new dvec3(0, 0, -10000);
// //     }

// //     public void ReloadTorpedo()
// //     {
// //         isLaunched = false;
// //         isExploded = false;
// //         isInWater = false;
// //         flightTimer = 0.0f;
// //         currentSpeed = 0.0f;

// //         node.Parent = initialParent;
// //         node.Position = (vec3)initialLocalPosition;
// //         node.SetRotation(initialLocalRotation);

// //         node.Enabled = false;
// //         node.Enabled = true;

// //         ResetAllSystems();
// //         Log.Message("[TORPEDO] MK46 Reloaded.\n");
// //     }

// //     private void StopVFXSmoothly(Node vfxNode)
// //     {
// //         if (vfxNode == null) return;
// //         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
// //         if (p != null) { p.EmitterEnabled = false; vfxNode.Parent = null; }
// //         else vfxNode.Enabled = false;
// //     }

// //     private void SetEmitterState(Node vfxNode, bool state)
// //     {
// //         if (vfxNode == null) return;
// //         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
// //         if (p != null) p.EmitterEnabled = state;
// //     }

// //     private void ResetAllSystems()
// //     {
// //         if (launchSound != null) launchSound.Stop();
// //         if (entryWaterSound != null) entryWaterSound.Stop();

// //         if (launchSmokeVFX != null) { launchSmokeVFX.Enabled = false; launchSmokeVFX.Parent = node; launchSmokeVFX.Position = vec3.ZERO; SetEmitterState(launchSmokeVFX, true); }
// //         if (waterTrailVFX != null) { waterTrailVFX.Enabled = false; waterTrailVFX.Parent = node; waterTrailVFX.Position = vec3.ZERO; SetEmitterState(waterTrailVFX, true); }
// //     }

// //     private bool CheckInput()
// //     {
// //         string key = keyBinding.ToUpper().Trim();
// //         if (key == "1") return Input.IsKeyDown(Input.KEY.DIGIT_1);
// //         if (key == "2") return Input.IsKeyDown(Input.KEY.DIGIT_2);
// //         if (key == "3") return Input.IsKeyDown(Input.KEY.DIGIT_3);
// //         if (key == "4") return Input.IsKeyDown(Input.KEY.DIGIT_4);
// //         if (key == "5") return Input.IsKeyDown(Input.KEY.DIGIT_5);
// //         if (key == "6") return Input.IsKeyDown(Input.KEY.DIGIT_6);
// //         // if (key == "7") return Input.IsKeyDown(Input.KEY.DIGIT_7);
// //         // if (key == "8") return Input.IsKeyDown(Input.KEY.DIGIT_8);
// //         // if (key == "9") return Input.IsKeyDown(Input.KEY.DIGIT_9);
// //         // if (key == "0") return Input.IsKeyDown(Input.KEY.DIGIT_0);
// //         return false;
// //     }
// // }

// //===== Versi Beta =====

// // using System;
// // using Unigine;

// // [Component(PropertyGuid = "f441cb609a7fc07426a453069e84559475069369")]
// // public class TorpedoLauncher : Component
// // {
// //     public float launchPushSpeed = 15.0f;  // Kecepatan horizontal saat keluar tabung
// //     public float cruiseSpeed = 50.0f;      // Kecepatan jelajah di dalam air (~50 knot)
// //     public float accelerationRate = 10.0f; // Akselerasi di dalam air
// //     public float turnSpeed = 2.5f;         // Cekatan dalam berbelok (Dinaikkan sedikit agar tidak putar-putar)

// //     [Parameter(Title = "Kedalaman Standar Air", Tooltip = "Kedalaman jika mengejar kapal permukaan (Z = -2 meter)")]
// //     public float torpedoDepth = -2.0f;
// //     [Parameter(Title = "Durasi Maju Horizontal")] public float straightAirDuration = 0.5f;
// //     [Parameter(Title = "Kecepatan Jatuh Kebawah")] public float dropSpeed = 12.0f;
// //     [Parameter(Title = "Target Node")] public Node targetNode = null;
// //     [Parameter(Title = "Tombol Tembak")] public string keyBinding = "9";
// //     [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
// //     [Parameter(Title = "Ledakan Kapal (explosion1)")] public Node explosionShipPrefab = null;
// //     [Parameter(Title = "Ledakan Air (water_explosion)")] public Node explosionWaterPrefab = null;
// //     [Parameter(Title = "Efek Buih Air (Trail)")] public Node waterTrailVFX = null;
// //     [Parameter(Title = "Asap Peluncuran (smoke_wisp_2)")] public Node launchSmokeVFX = null;
// //     [Parameter(Title = "Sound Meluncur (Snd_meluncur)")] public SoundSource launchSound = null;
// //     [Parameter(Title = "Sound Masuk Air (Snd_masukair)")] public SoundSource entryWaterSound = null;
// //     [Parameter(Title = "Sound Ledakan (Snd_ledakan)")] public SoundSource explosionSound = null;

// //     private float currentSpeed = 0.0f;
// //     private bool isLaunched = false;
// //     private bool isExploded = false;
// //     private bool isInWater = false;
// //     private float flightTimer = 0.0f;

// //     private Node initialParent;
// //     private dvec3 initialLocalPosition;
// //     private quat initialLocalRotation;

// //     protected override void OnReady()
// //     {
// //         initialParent = node.Parent;
// //         initialLocalPosition = node.Position;
// //         initialLocalRotation = node.GetRotation();

// //         ResetAllSystems();
// //     }

// //     void Update()
// //     {
// //         if (Input.IsKeyDown(Input.KEY.R))
// //         {
// //             ReloadTorpedo();
// //             return;
// //         }

// //         if (!isLaunched && !isExploded)
// //         {
// //             if (CheckInput()) LaunchTorpedo();
// //         }

// //         if (isLaunched && !isExploded)
// //         {
// //             flightTimer += Game.IFps;
// //             MoveTorpedo();
// //             CheckCollision();
// //         }
// //     }

// //     private void LaunchTorpedo()
// //     {
// //         if (launchSmokeVFX != null)
// //         {
// //             launchSmokeVFX.Enabled = true;
// //             SetEmitterState(launchSmokeVFX, true);
// //         }

// //         dvec3 worldPos = node.WorldPosition;
// //         quat worldRot = node.GetWorldRotation();

// //         node.Parent = null;
// //         node.WorldPosition = worldPos;
// //         node.SetWorldRotation(worldRot);

// //         isLaunched = true;
// //         isInWater = false;
// //         flightTimer = 0.0f;
// //         currentSpeed = launchPushSpeed;

// //         if (launchSound != null) { launchSound.Enabled = true; launchSound.Play(); }
// //         Log.Message($"[TORPEDO] MK46 Deployed!\n");
// //     }

// //     private void MoveTorpedo()
// //     {
// //         float dt = Game.IFps;
// //         vec3 targetDir;

// //         // FASE 1: Di Udara (Keluar Tabung)
// //         if (!isInWater)
// //         {
// //             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

// //             if (flightTimer < straightAirDuration)
// //             {
// //                 node.WorldTranslate(targetDir * currentSpeed * dt);
// //             }
// //             else
// //             {
// //                 node.WorldTranslate((targetDir * currentSpeed * dt) + (vec3.DOWN * dropSpeed * dt));
// //             }

// //             if (node.WorldPosition.z <= 0.0f)
// //             {
// //                 isInWater = true;
// //                 currentSpeed = cruiseSpeed * 0.7f;

// //                 if (launchSmokeVFX != null) StopVFXSmoothly(launchSmokeVFX);
// //                 if (entryWaterSound != null) { entryWaterSound.Enabled = true; entryWaterSound.Play(); }

// //                 if (explosionWaterPrefab != null)
// //                 {
// //                     Node instance = explosionWaterPrefab.Clone();
// //                     instance.WorldPosition = node.WorldPosition;
// //                     instance.Enabled = true;
// //                 }

// //                 if (waterTrailVFX != null) { waterTrailVFX.Enabled = true; SetEmitterState(waterTrailVFX, true); }
// //             }
// //         }
// //         // FASE 2: Di Dalam Air (MELACAK TARGET 3D TRUE HOMING)
// //         else
// //         {
// //             if (currentSpeed < cruiseSpeed) currentSpeed += accelerationRate * dt;

// //             if (targetNode != null)
// //             {
// //                 vec3 currentPos = (vec3)node.WorldPosition;
// //                 vec3 targetPos = (vec3)targetNode.WorldPosition;

// //                 // --- PERBAIKAN UTAMA: CEK KETINGGIAN TARGET ---
// //                 float targetZ = targetPos.z;

// //                 // Jika target berada di permukaan (Z >= -1.5m), gunakan depth standar torpedo (-2m)
// //                 if (targetZ >= -1.5f)
// //                 {
// //                     targetZ = torpedoDepth;

// //                     // Kunci posisi Z torpedo agar mendatar indah di permukaan jika kejar kapal permukaan
// //                     dvec3 currentWorldPos = node.WorldPosition;
// //                     currentWorldPos.z = torpedoDepth;
// //                     node.WorldPosition = currentWorldPos;
// //                 }
// //                 else
// //                 {
// //                     // KUNCI SENJATA: Jika target adalah kapal selam yang MENYELAM (Z < -1.5m),
// //                     // LEPASKAN KUNCIAN Z, biarkan torpedo ikut berbelok menukik ke dalam air laut (3D Homing)
// //                     targetZ = targetPos.z;
// //                 }

// //                 vec3 targetHomingPos = new vec3(targetPos.x, targetPos.y, targetZ);
// //                 targetDir = MathLib.Normalize(targetHomingPos - currentPos);
// //             }
// //             else
// //             {
// //                 targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
// //             }

// //             // Eksekusi rotasi 3D memburu target bawah laut
// //             vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
// //             vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
// //             node.WorldTranslate(smoothDir * currentSpeed * dt);
// //             node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
// //         }
// //     }

// //     private void CheckCollision()
// //     {
// //         if (!isLaunched || isExploded || flightTimer < 1.0f) return;

// //         // --- SISTEM DETEKSI GEOMETRI GANDA (ANTI-LOOSING UNDERWATER) ---
// //         if (targetNode != null)
// //         {
// //             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);

// //             // Pengaman Utama: Jika jarak bodi torpedo ke pivot pusat kapal selam kurang dari 22 meter, 
// //             // langsung ledakkan (mencegah luput akibat tabrakan laser terhalang partikel air)
// //             if (dist < 22.0f)
// //             {
// //                 ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
// //                 if (ship != null) ship.ActivateDamage();

// //                 ExplodeTorpedo(node.WorldPosition, targetNode, true);
// //                 return;
// //             }
// //         }

// //         // Jalankan pelacakan laser cadangan di permukaan lambung samping
// //         vec3 p0 = (vec3)node.WorldPosition;
// //         vec3 p1 = p0 + (node.GetWorldDirection(MathLib.AXIS.Y) * (currentSpeed * Game.IFps + 3.0f));

// //         WorldIntersectionNormal hitInfo = new WorldIntersectionNormal();
// //         Node hitObj = World.GetIntersection(p0, p1, hitMask, hitInfo);

// //         if (hitObj != null)
// //         {
// //             if (hitObj == targetNode || hitObj.IsChild(targetNode) || hitObj.Name.Contains("ALUGORO") || hitObj.Name.Contains("BELATI"))
// //             {
// //                 node.WorldPosition = hitInfo.Point;

// //                 ShipDamageHandler ship = hitObj.GetComponent<ShipDamageHandler>() ?? hitObj.GetComponentInParent<ShipDamageHandler>();
// //                 if (ship != null) ship.ActivateDamage();

// //                 ExplodeTorpedo(hitInfo.Point, hitObj, true);
// //                 return;
// //             }
// //         }

// //         if (flightTimer > 35.0f) ExplodeTorpedo(node.WorldPosition, null, false);
// //     }

// //     private void ExplodeTorpedo(dvec3 pos, Node hitParent, bool hitShip)
// //     {
// //         if (isExploded) return;
// //         isExploded = true;

// //         if (launchSound != null) launchSound.Stop();
// //         if (entryWaterSound != null) entryWaterSound.Stop();

// //         if (explosionSound != null)
// //         {
// //             SoundSource snd = explosionSound.Clone() as SoundSource;
// //             snd.Parent = hitParent;
// //             snd.WorldPosition = pos;
// //             snd.Enabled = true;
// //             snd.Play();
// //         }

// //         Node activeExplosionPrefab = hitShip ? explosionShipPrefab : explosionWaterPrefab;
// //         if (activeExplosionPrefab != null)
// //         {
// //             Node instance = activeExplosionPrefab.Clone();
// //             instance.Parent = hitParent;
// //             instance.WorldPosition = pos;
// //             instance.Enabled = true;
// //         }

// //         if (launchSmokeVFX != null) StopVFXSmoothly(launchSmokeVFX);
// //         if (waterTrailVFX != null) StopVFXSmoothly(waterTrailVFX);
// //         node.WorldPosition = new dvec3(0, 0, -10000);
// //     }

// //     public void ReloadTorpedo()
// //     {
// //         isLaunched = false;
// //         isExploded = false;
// //         isInWater = false;
// //         flightTimer = 0.0f;
// //         currentSpeed = 0.0f;

// //         node.Parent = initialParent;
// //         node.Position = (vec3)initialLocalPosition;
// //         node.SetRotation(initialLocalRotation);

// //         node.Enabled = false;
// //         node.Enabled = true;

// //         ResetAllSystems();
// //     }

// //     private void StopVFXSmoothly(Node vfxNode)
// //     {
// //         if (vfxNode == null) return;
// //         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
// //         if (p != null) { p.EmitterEnabled = false; vfxNode.Parent = null; }
// //         else vfxNode.Enabled = false;
// //     }

// //     private void SetEmitterState(Node vfxNode, bool state)
// //     {
// //         if (vfxNode == null) return;
// //         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
// //         if (p != null) p.EmitterEnabled = state;
// //     }

// //     private void ResetAllSystems()
// //     {
// //         if (launchSound != null) launchSound.Stop();
// //         if (entryWaterSound != null) entryWaterSound.Stop();
// //         if (launchSmokeVFX != null) { launchSmokeVFX.Enabled = false; launchSmokeVFX.Parent = node; launchSmokeVFX.Position = vec3.ZERO; SetEmitterState(launchSmokeVFX, true); }
// //         if (waterTrailVFX != null) { waterTrailVFX.Enabled = false; waterTrailVFX.Parent = node; waterTrailVFX.Position = vec3.ZERO; SetEmitterState(waterTrailVFX, true); }
// //     }

// //     private bool CheckInput()
// //     {
// //         string key = keyBinding.ToUpper().Trim();
// //         if (key == "1") return Input.IsKeyDown(Input.KEY.DIGIT_1);
// //         if (key == "2") return Input.IsKeyDown(Input.KEY.DIGIT_2);
// //         if (key == "3") return Input.IsKeyDown(Input.KEY.DIGIT_3);
// //         if (key == "4") return Input.IsKeyDown(Input.KEY.DIGIT_4);
// //         if (key == "5") return Input.IsKeyDown(Input.KEY.DIGIT_5);
// //         if (key == "6") return Input.IsKeyDown(Input.KEY.DIGIT_6);
// //         return false;
// //     }
// // }

// //========== Update Baru ========

// using System;
// using Unigine;

// [Component(PropertyGuid = "f441cb609a7fc07426a453069e84559475069369")]
// public class TorpedoLauncher : Component
// {
//     public float launchPushSpeed = 15.0f;
//     public float cruiseSpeed = 20.0f;
//     public float accelerationRate = 10.0f;
//     public float turnSpeed = 2.5f;

//     [Parameter(Title = "Kedalaman Standar Air")]
//     public float torpedoDepth = -2.0f;
//     [Parameter(Title = "Durasi Maju Horizontal")] public float straightAirDuration = 0.5f;
//     [Parameter(Title = "Kecepatan Jatuh Kebawah")] public float dropSpeed = 12.0f;
//     [Parameter(Title = "Target Node")] public Node targetNode = null;
//     [Parameter(Title = "Tombol Tembak")] public string keyBinding = "9";
//     [Parameter(Title = "Mask Tabrakan")] public int hitMask = 1;
//     [Parameter(Title = "Ledakan Kapal (explosion1)")] public Node explosionShipPrefab = null;
//     [Parameter(Title = "Ledakan Air (water_explosion)")] public Node explosionWaterPrefab = null;
//     [Parameter(Title = "Efek Buih Air (Trail)")] public Node waterTrailVFX = null;
//     [Parameter(Title = "Asap Peluncuran (smoke_wisp_2)")] public Node launchSmokeVFX = null;
//     [Parameter(Title = "Sound Meluncur (Snd_meluncur)")] public SoundSource launchSound = null;
//     [Parameter(Title = "Sound Masuk Air (Snd_masukair)")] public SoundSource entryWaterSound = null;
//     [Parameter(Title = "Sound Ledakan (Snd_ledakan)")] public SoundSource explosionSound = null;

//     private float currentSpeed = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isInWater = false;
//     private float flightTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         ResetAllSystems();
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R))
//         {
//             ReloadTorpedo();
//             return;
//         }

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput()) LaunchTorpedo();
//         }

//         if (isLaunched && !isExploded)
//         {
//             flightTimer += Game.IFps;
//             MoveTorpedo();
//             CheckCollision();
//         }
//     }

//     private void LaunchTorpedo()
//     {
//         // Aktifkan asap peluncuran instan di dalam tabung
//         if (launchSmokeVFX != null)
//         {
//             launchSmokeVFX.Enabled = true;
//             SetEmitterState(launchSmokeVFX, true);
//         }

//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();

//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         isInWater = false;
//         flightTimer = 0.0f;
//         currentSpeed = launchPushSpeed;

//         if (launchSound != null) { launchSound.Enabled = true; launchSound.Play(); }
//         Log.Message($"[TORPEDO] MK46 Deployed!\n");
//     }

//     private void MoveTorpedo()
//     {
//         float dt = Game.IFps;
//         vec3 targetDir;

//         // FASE 1: Di Udara (Keluar Tabung)
//         if (!isInWater)
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

//             if (flightTimer < straightAirDuration)
//             {
//                 node.WorldTranslate(targetDir * currentSpeed * dt);
//             }
//             else
//             {
//                 node.WorldTranslate((targetDir * currentSpeed * dt) + (vec3.DOWN * dropSpeed * dt));
//             }

//             // TEPAT SAAT MENYENTUH AIR LAUT
//             if (node.WorldPosition.z <= 0.0f)
//             {
//                 isInWater = true;
//                 currentSpeed = cruiseSpeed * 0.7f;

//                 // --- INTEGRASI UTAMA ANTI-HILANG PATAH PADA ASAP ---
//                 if (launchSmokeVFX != null)
//                 {
//                     // 1. Ambil posisi dunia dan rotasi terakhir partikel asap
//                     dvec3 smokeWorldPos = launchSmokeVFX.WorldPosition;
//                     quat smokeWorldRot = launchSmokeVFX.GetWorldRotation();


//                     // 2. KUNCI: Cabut kaitan bapak dari torpedo agar asap tertinggal mandiri di udara
//                     launchSmokeVFX.Parent = null;
//                     launchSmokeVFX.WorldPosition = smokeWorldPos;
//                     launchSmokeVFX.SetWorldRotation(smokeWorldRot);


//                     // 3. Matikan emisi baru. Partikel lama yang sudah tersembur akan memudar halus sesuai Lifetime-nya
//                     ObjectParticles p = launchSmokeVFX as ObjectParticles ?? launchSmokeVFX.GetChild(0) as ObjectParticles;
//                     if (p != null) p.EmitterEnabled = false;
//                 }

//                 if (entryWaterSound != null) { entryWaterSound.Enabled = true; entryWaterSound.Play(); }

//                 if (explosionWaterPrefab != null)
//                 {
//                     Node instance = explosionWaterPrefab.Clone();
//                     instance.WorldPosition = node.WorldPosition;
//                     instance.Enabled = true;
//                 }

//                 if (waterTrailVFX != null) { waterTrailVFX.Enabled = true; SetEmitterState(waterTrailVFX, true); }
//             }
//         }
//         // FASE 2: Di Dalam Air (Melacak Sasaran)
//         else
//         {
//             if (currentSpeed < cruiseSpeed) currentSpeed += accelerationRate * dt;

//             if (targetNode != null)
//             {
//                 vec3 currentPos = (vec3)node.WorldPosition;
//                 vec3 targetPos = (vec3)targetNode.WorldPosition;

//                 float targetZ = targetPos.z;

//                 if (targetZ >= -1.5f)
//                 {
//                     targetZ = torpedoDepth;
//                     dvec3 currentWorldPos = node.WorldPosition;
//                     currentWorldPos.z = torpedoDepth;
//                     node.WorldPosition = currentWorldPos;
//                 }
//                 else
//                 {
//                     targetZ = targetPos.z;
//                 }

//                 vec3 targetHomingPos = new vec3(targetPos.x, targetPos.y, targetZ);
//                 targetDir = MathLib.Normalize(targetHomingPos - currentPos);
//             }
//             else
//             {
//                 targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//             }

//             vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//             vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//             node.WorldTranslate(smoothDir * currentSpeed * dt);
//             node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//         }
//     }

//     private void CheckCollision()
//     {
//         if (!isLaunched || isExploded || flightTimer < 1.0f) return;

//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 15.0f)
//             {
//                 ShipDamageHandler ship = targetNode.GetComponent<ShipDamageHandler>() ?? targetNode.GetComponentInParent<ShipDamageHandler>();
//                 if (ship != null) ship.ActivateDamage();

//                 ExplodeTorpedo(node.WorldPosition, targetNode, true);
//                 return;
//             }
//         }

//         vec3 p0 = (vec3)node.WorldPosition;
//         vec3 p1 = p0 + (node.GetWorldDirection(MathLib.AXIS.Y) * (currentSpeed * Game.IFps + 3.0f));

//         WorldIntersectionNormal hitInfo = new WorldIntersectionNormal();
//         Node hitObj = World.GetIntersection(p0, p1, hitMask, hitInfo);

//         if (hitObj != null)
//         {
//             if (hitObj == targetNode || hitObj.IsChild(targetNode) || hitObj.Name.Contains("ALUGORO") || hitObj.Name.Contains("BELATI"))
//             {
//                 node.WorldPosition = hitInfo.Point;

//                 ShipDamageHandler ship = hitObj.GetComponent<ShipDamageHandler>() ?? hitObj.GetComponentInParent<ShipDamageHandler>();
//                 if (ship != null) ship.ActivateDamage();

//                 ExplodeTorpedo(hitInfo.Point, hitObj, true);
//                 return;
//             }
//         }

//         if (flightTimer > 35.0f) ExplodeTorpedo(node.WorldPosition, null, false);
//     }

//     private void ExplodeTorpedo(dvec3 pos, Node hitParent, bool hitShip)
//     {
//         if (isExploded) return;
//         isExploded = true;

//         if (launchSound != null) launchSound.Stop();
//         if (entryWaterSound != null) entryWaterSound.Stop();

//         if (explosionSound != null)
//         {
//             SoundSource snd = explosionSound.Clone() as SoundSource;
//             snd.Parent = hitParent;
//             snd.WorldPosition = pos;
//             snd.Enabled = true;
//             snd.Play();
//         }

//         Node activeExplosionPrefab = hitShip ? explosionShipPrefab : explosionWaterPrefab;
//         if (activeExplosionPrefab != null)
//         {
//             Node instance = activeExplosionPrefab.Clone();
//             instance.Parent = hitParent;
//             instance.WorldPosition = pos;
//             instance.Enabled = true;
//         }

//         if (launchSmokeVFX != null) StopVFXSmoothly(launchSmokeVFX);
//         if (waterTrailVFX != null) StopVFXSmoothly(waterTrailVFX);
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadTorpedo()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isInWater = false;
//         flightTimer = 0.0f;
//         currentSpeed = 0.0f;

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);

//         node.Enabled = false;
//         node.Enabled = true;

//         ResetAllSystems();
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
//         if (p != null) { p.EmitterEnabled = false; vfxNode.Parent = null; }
//         else vfxNode.Enabled = false;
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles p = vfxNode as ObjectParticles ?? vfxNode.GetChild(0) as ObjectParticles;
//         if (p != null) p.EmitterEnabled = state;
//     }

//     private void ResetAllSystems()
//     {
//         if (launchSound != null) launchSound.Stop();
//         if (entryWaterSound != null) entryWaterSound.Stop();
//         if (launchSmokeVFX != null) { launchSmokeVFX.Enabled = false; launchSmokeVFX.Parent = node; launchSmokeVFX.Position = vec3.ZERO; SetEmitterState(launchSmokeVFX, true); }
//         if (waterTrailVFX != null) { waterTrailVFX.Enabled = false; waterTrailVFX.Parent = node; waterTrailVFX.Position = vec3.ZERO; SetEmitterState(waterTrailVFX, true); }
//     }

//     private bool CheckInput()
//     {
//         string key = keyBinding.ToUpper().Trim();
//         if (key == "1") return Input.IsKeyDown(Input.KEY.DIGIT_1);
//         if (key == "2") return Input.IsKeyDown(Input.KEY.DIGIT_2);
//         if (key == "3") return Input.IsKeyDown(Input.KEY.DIGIT_3);
//         if (key == "4") return Input.IsKeyDown(Input.KEY.DIGIT_4);
//         if (key == "5") return Input.IsKeyDown(Input.KEY.DIGIT_5);
//         if (key == "6") return Input.IsKeyDown(Input.KEY.DIGIT_6);
//         return false;
//     }
// }
