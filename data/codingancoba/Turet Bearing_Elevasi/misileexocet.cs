//======================================= Exocet ================================================================//

// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileExocetMM40 : Component
// {
//     public float speed = 120.0f;
//     public float turnSpeed = 4.5f;
//     public float seaSkimmingHeight = 2.5f;
//     public float launchAscentHeight = 15.0f;

//     [Parameter(Title = "Target (KRI Golok)")]
//     public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")]
//     public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")]
//     public string keyBinding = "1";

//     public Node explosionPrefab = null;
//     public Node waterExplosionPrefab = null;
//     public Node rocketMotorVFX = null;

//     [Parameter(Title = "Asap Awal (Smoke Wisp)")]
//     public Node smokeWisp = null;

//     [Parameter(Title = "Jeda Luncur (Detik)")]
//     public float launchDelay = 8f;

//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;
//         if (smokeWisp != null) smokeWisp.Enabled = false;
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput() && !isPreparing) StartIgnition();

//             if (isPreparing)
//             {
//                 prepareTimer += Game.IFps;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;
//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;
//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = true;
//             SetEmitterState(smokeWisp, true); // Pastikan emitter nyala
//         }
//         Log.Message($"[IGNITION] Missile {keyBinding} warming up...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f;

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = true;

//         // --- PERBAIKAN: Matikan asap secara halus (Smooth) ---
//         StopVFXSmoothly(smokeWisp);

//         Log.Message($"[EXOCET] MM40 Block 3: Missile {keyBinding} Launched!\n");
//     }

//     // Fungsi untuk mematikan Emitter partikel agar sisa asap tidak hilang mendadak
//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;

//         // Coba cari komponen partikel di node itu sendiri atau di anaknya
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null)
//         {
//             // Matikan keran partikel, tapi biarkan partikel yang sudah ada memudar
//             particles.EmitterEnabled = false;
//         }
//         else
//         {
//             vfxNode.Enabled = false; // Jika bukan partikel, baru matikan paksa
//         }
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float desiredHeight = (launchTimer < 1.5f) ? launchAscentHeight : seaSkimmingHeight;
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//         node.WorldTranslate(smoothDir * speed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 10.0f) { Explode(false); return; }
//         }

//         if (selfShipNode != null && launchTimer > 1.2f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 10.0f) { Explode(false); return; }
//         }

//         if (node.WorldPosition.z < -0.5f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;

//         // Asap dimatikan halus saat meledak
//         StopVFXSmoothly(smokeWisp);

//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isPreparing = false;
//         launchTimer = 0.0f;
//         prepareTimer = 0.0f;

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         if (rocketMotorVFX != null) rocketMotorVFX.Enabled = false;

//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = false;
//             SetEmitterState(smokeWisp, true); // Reset emitter agar bisa dipakai lagi
//         }
//         Log.Message($"Exocet System: Missile {keyBinding} Reloaded.\n");
//     }

// private bool CheckInput()
// {
//     Input.KEY targetKey = Input.KEY.DIGIT_1;
//     if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//     else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//     else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//     else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//     else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//     else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//     else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//     return Input.IsKeyDown(targetKey);
// }
// }


//======================================= Yakhont (P-800 Oniks) ===================================================//


// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileExocetSSM : Component
// {
//     public float startSpeed = 20.0f;       // Kecepatan awal saat keluar tabung
//     public float maxSpeed = 350.0f;        // Kecepatan puncak (Supersonik)
//     public float accelerationRate = 60.0f; // Pertambahan kecepatan per detik
//     public float turnSpeed = 2.5f;
//     public float cruiseHeight = 40.0f;
//     public float seaSkimmingHeight = 5.0f;
//     public float attackRange = 300.0f;

//     [Parameter(Title = "Target (KRI Golok)")] public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")] public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")] public string keyBinding = "1";

//     public Node explosionPrefab = null;
//     public Node waterExplosionPrefab = null;
//     public Node vfx_jet_engine = null;
//     public Node smokeWisp = null;
//     public float launchDelay = 5.5f;

//     private float currentSpeed = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         if (smokeWisp != null) smokeWisp.Enabled = false;

//         currentSpeed = startSpeed;
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput() && !isPreparing) StartIgnition();

//             if (isPreparing)
//             {
//                 prepareTimer += Game.IFps;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;

//             // --- LOGIKA KECEPATAN OTOMATIS ---
//             if (currentSpeed < maxSpeed)
//                 currentSpeed += accelerationRate * Game.IFps;

//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     // --- FUNGSI BARU 1: Menangani Persiapan (Asap Awal) ---
//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;
//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = true;
//             SetEmitterState(smokeWisp, true); // Menyalakan keran partikel
//         }
//         Log.Message($"[IGNITION] Missile {keyBinding} warming up...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f;
//         currentSpeed = startSpeed;

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = true;

//         // Mematikan asap awal secara halus (smooth)
//         StopVFXSmoothly(smokeWisp);

//         Log.Message($"[SYSTEM] Missile {keyBinding} Launched! Accelerating to {maxSpeed}...\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float distToTarget = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);

//             // Logika High-Low Profile
//             float desiredHeight = (distToTarget > attackRange) ? cruiseHeight : seaSkimmingHeight;
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);

//         node.WorldTranslate(smoothDir * currentSpeed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 15.0f) { Explode(false); return; }
//         }

//         if (selfShipNode != null && launchTimer > 1.0f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 12.0f) { Explode(false); return; }
//         }

//         if (node.WorldPosition.z < -1.0f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         StopVFXSmoothly(smokeWisp);
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     // --- FUNGSI BARU 2: Mengontrol Emitter Partikel ---
//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         // Jika partikel ada di objek anak (child)
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;
//         SetEmitterState(vfxNode, false); // Matikan produksi partikel baru
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isPreparing = false;
//         launchTimer = 0.0f;
//         prepareTimer = 0.0f;
//         currentSpeed = startSpeed;

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;

//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = false;
//             SetEmitterState(smokeWisp, true); // Reset keran agar siap dipakai lagi
//         }
//     }

//     private bool CheckInput()
//     {
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//         return Input.IsKeyDown(targetKey);
//     }
// }


//===================================== C-705 =============================================================//



// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileC705 : Component
// {
//     public float startSpeed = 10.0f;       // Keluar peluncur pelan
//     public float maxSpeed = 125.0f;        // Top speed Mach 0.8 - 0.9 (Subsonik)
//     public float accelerationRate = 110.0f; // Akselerasi booster sangat kuat di awal
//     public float turnSpeed = 4.0f;         // Lebih lincah manuver dibanding Yakhont

//     public float cruiseHeight = 12.0f;     // C-705 terbang rendah (Mid-altitude)
//     public float seaSkimmingHeight = 4.0f; // Terminal phase: merayap sangat rendah di ombak
//     public float attackRange = 150.0f;     // Jarak mulai menukik ekstrem ke air

//     [Parameter(Title = "Target (KRI Golok)")] public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")] public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")] public string keyBinding = "1";

//     public Node explosionPrefab = null;
//     public Node waterExplosionPrefab = null;
//     public Node vfx_jet_engine = null; // Aktif setelah fase booster
//     public Node smokeWisp = null;      // Smoke Booster (Tebal di awal)
//     public float launchDelay = 4f;   // Jeda ignition C-705

//     private float currentSpeed = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         if (smokeWisp != null) smokeWisp.Enabled = false;

//         currentSpeed = startSpeed;
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput() && !isPreparing) StartIgnition();

//             if (isPreparing)
//             {
//                 prepareTimer += Game.IFps;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;

//             // --- LOGIKA AKSELERASI BOOSTER C-705 ---
//             if (currentSpeed < maxSpeed)
//                 currentSpeed += accelerationRate * Game.IFps;

//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;
//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = true;
//             SetEmitterState(smokeWisp, true);
//         }
//         Log.Message($"[IGNITION] C-705 Missile {keyBinding} Booster Primed...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f;
//         currentSpeed = startSpeed;

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = true;

//         // Mematikan asap booster awal secara halus
//         StopVFXSmoothly(smokeWisp);

//         Log.Message($"[SYSTEM] C-705 {keyBinding} Launched! Sea-Skimming Active.\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float distToTarget = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);

//             // --- LOGIKA SEA SKIMMING C-705 ---
//             // C-705 menjaga ketinggian rendah sejak awal (cruiseHeight)
//             // Lalu menukik ke seaSkimmingHeight saat mendekati target (attackRange)
//             float desiredHeight = (distToTarget > attackRange) ? cruiseHeight : seaSkimmingHeight;

//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         // --- SISTEM PEMANDU (MANUVER LINCAH) ---
//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);

//         node.WorldTranslate(smoothDir * currentSpeed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             // Hitbox C-705 lebih kecil (12m) dibanding Yakhont yang besar
//             if (dist < 12.0f) { Explode(false); return; }
//         }

//         if (selfShipNode != null && launchTimer > 1.2f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 10.0f) { Explode(false); return; }
//         }

//         if (node.WorldPosition.z < -0.5f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         StopVFXSmoothly(smokeWisp);
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;
//         SetEmitterState(vfxNode, false);
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isPreparing = false;
//         launchTimer = 0.0f;
//         prepareTimer = 0.0f;
//         currentSpeed = startSpeed;

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         if (smokeWisp != null)
//         {
//             smokeWisp.Enabled = false;
//             SetEmitterState(smokeWisp, true);
//         }
//         Log.Message($"C-705 System: Missile {keyBinding} Reloaded.\n");
//     }

//     private bool CheckInput()
//     {
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//         return Input.IsKeyDown(targetKey);
//     }
// }



//===================================== Harpoon ============================================================//




// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileHarpoon : Component
// {
//     public float maxSpeed = 130.0f;
//     public float accelerationRate = 150.0f;
//     public float turnSpeed = 3.5f;
//     public float seaSkimmingHeight = 3.0f;

//     [Parameter(Title = "Target (KRI Golok)")] public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")] public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")] public string keyBinding = "1";

//     public Node explosionPrefab = null;
//     public Node waterExplosionPrefab = null;

//     [Parameter(Title = "Asap Utama (Trail Smoke)")]
//     public Node smokeTrail = null;

//     [Parameter(Title = "Efek Api Mesin Jet")]
//     public Node vfx_jet_engine = null;

//     public float launchDelay = 4f;

//     private float currentSpeed = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         // Simpan posisi awal untuk reload
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         // Pastikan semua efek mati saat awal (di deck)
//         ResetAllVFX();
//     }

//     void Update()
//     {
//         // Tombol R untuk Reload
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             // Cek Input Tembak
//             if (CheckInput() && !isPreparing) StartIgnition();

//             // Logika Jeda Ignition
//             if (isPreparing)
//             {
//                 prepareTimer += Game.IFps;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;
//             // Akselerasi bertahap
//             if (currentSpeed < maxSpeed)
//                 currentSpeed += accelerationRate * Game.IFps;

//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;

//         // Aktifkan asap saat ancang-ancang
//         if (smokeTrail != null)
//         {
//             smokeTrail.Enabled = true;
//             SetEmitterState(smokeTrail, true);
//         }
//         Log.Message($"[IGNITION] Missile {keyBinding} warming up...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();

//         node.Parent = null; // Lepas dari kapal, VFX tetap ikut karena mereka Child dari Rudal
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f;

//         // Nyalakan api jet saat meluncur
//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = true;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//         Log.Message($"[SYSTEM] Missile {keyBinding} GO!\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             // Mode Sea Skimming
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, seaSkimmingHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//         node.WorldTranslate(smoothDir * currentSpeed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         // Cek target
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 12.0f) { Explode(false); return; }
//         }
//         // Cek kapal sendiri (Friendly fire) setelah 1.2 detik
//         if (selfShipNode != null && launchTimer > 1.2f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 10.0f) { Explode(false); return; }
//         }
//         // Cek air
//         if (node.WorldPosition.z < -0.5f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         if (isExploded) return;
//         isExploded = true;

//         // 1. Simpan posisi ledakan terakhir sebelum rudal pindah
//         dvec3 explosionPos = node.WorldPosition;

//         // 2. Tampilkan Ledakan (Clone)
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;
//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = explosionPos;
//             instanceExplosion.Enabled = true;
//         }

//         // 3. TANGANI ASAP AGAR TIDAK MENGIKUTI KE -10000
//         if (smokeTrail != null)
//         {
//             // MATIKAN EMITTER DULU sebelum pindah posisi
//             StopVFXSmoothly(smokeTrail);

//             // LEPAS DARI PARENT agar sisa asap tetap di lokasi tabrakan
//             smokeTrail.Parent = null;

//             // PAKSA posisi asap tetap di lokasi ledakan agar tidak ikut teleport
//             smokeTrail.WorldPosition = explosionPos;
//         }

//         if (vfx_jet_engine != null)
//         {
//             StopVFXSmoothly(vfx_jet_engine);
//             vfx_jet_engine.Parent = null; // Lepas juga agar tidak ikut teleport
//         }

//         // 4. BUANG RUDAL (Sudah aman karena asap sudah tidak punya Parent ke rudal)
//         node.WorldPosition = new dvec3(0, 0, -10000);

//         Log.Message("Missile Impact! Trail left at site to fade...\n");
//     }



//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;

//         // Cari komponen ObjectParticles di Node tersebut atau di anak pertamanya
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null)
//         {
//             // KUNCI: Jangan matikan Node, matikan keran partikelnya saja
//             particles.EmitterEnabled = false;
//         }
//         else
//         {
//             // Jika bukan partikel (misal lampu/mesh api), baru matikan total
//             vfxNode.Enabled = false;
//         }
//     }


//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }

//     private void ResetAllVFX()
//     {
//         if (smokeTrail != null) { smokeTrail.Enabled = false; SetEmitterState(smokeTrail, true); }
//         if (vfx_jet_engine != null) { vfx_jet_engine.Enabled = false; SetEmitterState(vfx_jet_engine, true); }
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         isPreparing = false;
//         launchTimer = 0.0f;
//         prepareTimer = 0.0f;
//         currentSpeed = 0;

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         // Pasang kembali asap ke rudal dan reset emitter-nya
//         if (smokeTrail != null)
//         {
//             smokeTrail.Parent = node; // Jadikan anak rudal lagi
//             smokeTrail.Position = vec3.ZERO; // Kembalikan ke posisi belakang rudal
//             smokeTrail.Enabled = false;
//             SetEmitterState(smokeTrail, true); // Buka kembali keran partikel untuk tembakan berikutnya
//         }

//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = false;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//     }


//     private bool CheckInput()
//     {
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//         return Input.IsKeyDown(targetKey);
//     }
// }


//====================== dELAY bENTAR ===========================

// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileHarpoon : Component
// {
//     public float startSpeed = 60.0f;       // Kecepatan instan saat keluar tabung (Booster)
//     public float maxSpeed = 150.0f;       // Kecepatan puncak (Sustainer)
//     public float accelerationRate = 30.0f; // Akselerasi bertahap
//     public float turnSpeed = 3.5f;
//     public float seaSkimmingHeight = 3.0f;

//     [Parameter(Title = "Target (KRI Golok)")] public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")] public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")] public string keyBinding = "1";

//     public Node explosionPrefab = null;
//     public Node waterExplosionPrefab = null;
//     [Parameter(Title = "Asap Utama (Booster Smoke)")] public Node smokeTrail = null;
//     [Parameter(Title = "Efek Api Mesin Jet")] public Node vfx_jet_engine = null;

//     public float launchDelay = 3.8f;       // Jeda ignition (Realistis: 1 detik)
//     public float boosterLifetime = 3.0f;   // Berapa detik asap tebal muncul

//     private float currentSpeed = 0.0f;
//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private bool isPreparing = false;
//     private float prepareTimer = 0.0f;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();
//         ResetAllVFX();
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput() && !isPreparing) StartIgnition();

//             if (isPreparing)
//             {
//                 prepareTimer += Game.IFps;
//                 if (prepareTimer >= launchDelay) Launch();
//             }
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;

//             // 1. Logika Akselerasi
//             if (currentSpeed < maxSpeed)
//                 currentSpeed += accelerationRate * Game.IFps;

//             // 2. Transisi Booster ke Sustainer (Asap menipis setelah terbang)
//             if (launchTimer > boosterLifetime)
//             {
//                 StopVFXSmoothly(smokeTrail);
//             }

//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void StartIgnition()
//     {
//         isPreparing = true;
//         prepareTimer = 0.0f;
//         if (smokeTrail != null)
//         {
//             smokeTrail.Enabled = true;
//             SetEmitterState(smokeTrail, true);
//         }
//         Log.Message($"[IGNITION] Missile {keyBinding} warming up...\n");
//     }

//     private void Launch()
//     {
//         isPreparing = false;
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();

//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f;
//         currentSpeed = startSpeed; // Berikan hentakan awal (Booster power)

//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = true;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//         Log.Message($"[SYSTEM] Missile {keyBinding} Launched at {startSpeed} speed!\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, seaSkimmingHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else targetDir = node.GetWorldDirection(MathLib.AXIS.Y);

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//         node.WorldTranslate(smoothDir * currentSpeed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 15.0f) { Explode(false); return; }
//         }

//         if (selfShipNode != null && launchTimer > 1.2f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 12.0f) { Explode(false); return; }
//         }

//         if (node.WorldPosition.z < -1.0f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         if (isExploded) return;
//         isExploded = true;

//         dvec3 explosionPos = node.WorldPosition;

//         // 1. Tampilkan Efek Ledakan
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;
//         if (effectPrefab != null)
//         {
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = explosionPos;
//             instanceExplosion.Enabled = true;
//         }

//         // 2. STOP ASAP & Lepas Parent SEBELUM rudal teleport ke -10000
//         if (smokeTrail != null)
//         {
//             StopVFXSmoothly(smokeTrail);
//             smokeTrail.Parent = null;
//             smokeTrail.WorldPosition = explosionPos; // Paksa tetap di titik ledak
//         }
//         if (vfx_jet_engine != null)
//         {
//             StopVFXSmoothly(vfx_jet_engine);
//             vfx_jet_engine.Parent = null;
//         }

//         // 3. Sembunyikan Rudal
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     private void StopVFXSmoothly(Node vfxNode)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = false;
//         else vfxNode.Enabled = false;
//     }

//     private void SetEmitterState(Node vfxNode, bool state)
//     {
//         if (vfxNode == null) return;
//         ObjectParticles particles = vfxNode as ObjectParticles;
//         if (particles == null && vfxNode.NumChildren > 0)
//             particles = vfxNode.GetChild(0) as ObjectParticles;

//         if (particles != null) particles.EmitterEnabled = state;
//     }

//     private void ResetAllVFX()
//     {
//         if (smokeTrail != null)
//         {
//             smokeTrail.Enabled = false;
//             smokeTrail.Parent = node;
//             smokeTrail.Position = vec3.ZERO;
//             SetEmitterState(smokeTrail, true);
//         }
//         if (vfx_jet_engine != null)
//         {
//             vfx_jet_engine.Enabled = false;
//             vfx_jet_engine.Parent = node;
//             vfx_jet_engine.Position = vec3.ZERO;
//             SetEmitterState(vfx_jet_engine, true);
//         }
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false; isExploded = false; isPreparing = false;
//         launchTimer = 0.0f; prepareTimer = 0.0f; currentSpeed = 0;

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         ResetAllVFX();
//         Log.Message($"Missile {keyBinding} Reloaded.\n");
//     }

//     private bool CheckInput()
//     {
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//         return Input.IsKeyDown(targetKey);
//     }
// }





//=============================================================================================================//

// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileExocetSSM : Component
// {
//     // --- PERFORMANCE PARAMETERS (Karakteristik Yakhont P-800) ---
//     public float speed = 350.0f;          // Mach 2.5+ (Supersonik untuk menembus pertahanan lawan)
//     public float turnSpeed = 2.5f;        // Inersia manuver tinggi karena kecepatan supersonik

//     // --- LOGIKA HIGH-LOW PROFILE (Ciri Khas Oniks) ---
//     // Fase HIGH: Terbang tinggi untuk menghemat bahan bakar & jangkauan sensor luas
//     public float cruiseHeight = 40.0f;
//     // Fase LOW: Sea Skimming (Terbang di bawah garis radar musuh)
//     public float seaSkimmingHeight = 5.0f;
//     // Terminal Phase: Jarak di mana rudal mulai menukik (Dive) dari High ke Low
//     public float attackRange = 300.0f;

//     [Parameter(Title = "Target (KRI Golok)")]
//     public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")]
//     public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")]
//     public string keyBinding = "1";

//     public Node explosionPrefab = null;
//     public Node waterExplosionPrefab = null;
//     public Node vfx_jet_engine = null;

//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();
//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//     }

//     void Update()
//     {
//         // 1. Cek fitur Reload (Tombol R)
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         // 2. Jika belum meluncur, cek input tombol tembak
//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput()) Launch();
//         }

//         // 3. Jika sudah meluncur dan belum meledak, gerakkan rudal
//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps;
//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private void Launch()
//     {
//         // Memindahkan rudal dari parent (deck kapal) ke World Space
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();

//         node.Parent = null; // Lepas dari kapal pengangkut
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f; // Reset timer untuk jarak aman (Safe Distance)

//         // Aktifkan efek api mesin jet
//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = true;

//         Log.Message($"[SYSTEM] Missile {keyBinding} Launched!\n");
//     }


//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float distToTarget = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);

//             // --- EKSEKUSI LOGIKA HIGH-LOW PROFILE ---
//             // ASLINYA: Rudal Oniks terbang pada 14.000m (High) lalu turun ke 10m (Low).
//             // DI SINI: Jika jarak jauh (> attackRange), rudal mengejar 'cruiseHeight'.
//             // Jika sudah dekat (<= attackRange), rudal mengejar 'seaSkimmingHeight'.
//             float desiredHeight = (distToTarget > attackRange) ? cruiseHeight : seaSkimmingHeight;

//             // Menciptakan target bayangan di udara (Waypoints) agar navigasi smooth
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);

//             // Mencari vektor arah dari posisi rudal sekarang ke titik bayangan tersebut
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else
//         {
//             // Jika target tidak ada (Otomatis), terbang lurus mengikuti arah peluncur
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         // --- SISTEM PEMANDU (GUIDANCE SYSTEM) ---
//         // Menggunakan Linear Interpolation (Lerp) untuk efek inersia manuver.
//         // Rudal supersonik tidak bisa belok patah, harus membentuk kurva melengkung.
//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);

//         node.WorldTranslate(smoothDir * speed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         // 1. TERMINAL IMPACT (Tabrakan dengan Target)
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             // Hitbox 15m mensimulasikan dampak kinetik dari rudal seberat 3 ton
//             if (dist < 15.0f) { Explode(false); return; }
//         }

//         // 2. SAFE DISTANCE & FRIENDLY FIRE
//         // launchTimer > 1.0f adalah 'Arming Time' (Waktu Pengaktifan Hulu Ledak).
//         // Mencegah rudal meledak prematur saat masih di dekat deck KRI Belati.
//         if (selfShipNode != null && launchTimer > 1.0f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 12.0f) { Explode(false); return; }
//         }

//         // 3. SEA COLLISION
//         // Jika rudal terbang terlalu rendah (di bawah air), otomatis meledak (VFX Splash)
//         if (node.WorldPosition.z < -1.0f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             // SISTEM CLONING: Menciptakan instance ledakan baru secara independen.
//             // Memastikan tiap rudal punya ledakannya sendiri meski meledak bersamaan.
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;

//         // PENTING: Membuang rudal ke koordinat yang sangat jauh (Z = -10000)
//         // agar tidak terlihat di map namun data objeknya tetap aman untuk fungsi Reload
//         node.WorldPosition = new dvec3(0, 0, -10000);
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         launchTimer = 0.0f; // Reset sistem keamanan hulu ledak

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         Log.Message($"System: Missile {keyBinding} Reloaded and Active.\n");
//     }

//     private bool CheckInput()
//     {
//         // Pemetaan tombol angka 1-8 pada keyboard (DIGIT)
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//         return Input.IsKeyDown(targetKey);
//     }
// }

//=========================================== Tutup ==================================================================//


// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileExocetSSM : Component
// {
//     public float speed = 350.0f;
//     public float turnSpeed = 2.5f;
//     public float seaSkimmingHeight = 5.0f;
//     public float cruiseHeight = 40.0f;

//     [Parameter(Title = "Target (KRI Golok)")]
//     public Node targetNode = null;
//     [Parameter(Title = "Kapal Sendiri (KRI Belati)")]
//     public Node selfShipNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")]
//     public string keyBinding = "1";

//     [Parameter(Title = "Efek Ledakan Kapal")]
//     public Node explosionPrefab = null;
//     [Parameter(Title = "Efek Ledakan Air")]
//     public Node waterExplosionPrefab = null;
//     [Parameter(Title = "Efek Api Mesin Jet")]
//     public Node vfx_jet_engine = null;

//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private float launchTimer = 0.0f;

//     private Node initialParent;
//     private dvec3 initialLocalPosition;
//     private quat initialLocalRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         initialLocalPosition = node.Position;
//         initialLocalRotation = node.GetRotation();

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//     }

//     void Update()
//     {
//         // Tombol R untuk Reload semua rudal
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput()) Launch();
//         }

//         if (isLaunched && !isExploded)
//         {
//             launchTimer += Game.IFps; // Hitung waktu sejak meluncur
//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private bool CheckInput()
//     {
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;
//         return Input.IsKeyDown(targetKey);
//     }

//     private void Launch()
//     {
//         dvec3 worldPos = node.WorldPosition;
//         quat worldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = worldPos;
//         node.SetWorldRotation(worldRot);

//         isLaunched = true;
//         launchTimer = 0.0f; // Reset timer saat meluncur
//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = true;
//         Log.Message($"Missile {keyBinding} Launched!\n");
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition;
//             float distToTarget = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);

//             // Logika Ketinggian (Cruise -> Sea Skimming)
//             float desiredHeight = (distToTarget > 300.0f) ? cruiseHeight : seaSkimmingHeight;
//             vec3 adjustedTargetPos = new vec3(targetPos.x, targetPos.y, desiredHeight);
//             targetDir = MathLib.Normalize(adjustedTargetPos - currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);
//         node.WorldTranslate(smoothDir * speed * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         // 1. Tabrakan dengan Target
//         if (targetNode != null)
//         {
//             float dist = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (dist < 15.0f) { Explode(false); return; }
//         }

//         // 2. Tabrakan dengan Kapal Sendiri (Friendly Fire)
//         // Hanya cek jika sudah terbang lebih dari 1 detik (Safe Distance)
//         if (selfShipNode != null && launchTimer > 1.0f)
//         {
//             float distToSelf = (float)MathLib.Distance(node.WorldPosition, selfShipNode.WorldPosition);
//             if (distToSelf < 12.0f) { Explode(false); return; }
//         }

//         // 3. Tabrakan dengan Air
//         if (node.WorldPosition.z < -1.0f) Explode(true);
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         Node effectPrefab = hitWater ? waterExplosionPrefab : explosionPrefab;

//         if (effectPrefab != null)
//         {
//             // CLONE agar tidak rebutan antar rudal
//             Node instanceExplosion = effectPrefab.Clone();
//             instanceExplosion.Parent = null;
//             instanceExplosion.WorldPosition = node.WorldPosition;
//             instanceExplosion.Enabled = true;
//         }

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         node.WorldPosition = new dvec3(0, 0, -10000); // Sembunyikan
//     }

//     public void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;
//         launchTimer = 0.0f; // Reset waktu agar tidak meledak di deck saat muncul

//         node.Parent = initialParent;
//         node.Position = (vec3)initialLocalPosition;
//         node.SetRotation(initialLocalRotation);
//         node.Enabled = true;

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         Log.Message($"Missile {keyBinding} Reloaded.\n");
//     }
// }


//==========================================================================================================================

// using System;
// using Unigine;

// [Component(PropertyGuid = "a0413c781d497e824f06bf8cc2c32163ca8c3376")]
// public class MissileExocetSSM : Component
// {
//     public float speed = 100.0f;
//     public float turnSpeed = 5.0f;
//     public float waterLevel = 0.0f;
//     public float seaSkimmingHeight = 2.0f;

//     [Parameter(Title = "Target (KRI Golok)")]
//     public Node targetNode = null;
//     [Parameter(Title = "Tombol Tembak (1-8)")]
//     public string keyBinding = "1";

//     [Parameter(Title = "Efek Ledakan Kapal")]
//     public Node explosion1 = null;
//     [Parameter(Title = "Efek Ledakan Air")]
//     public Node water_explosion = null;
//     [Parameter(Title = "Efek Api Mesin Jet")]
//     public Node vfx_jet_engine = null;

//     // --- FITUR BARU: SISTEM TERBAKAR ---
//     [Parameter(Title = "Efek Api KRI Golok (VFX Fire)")]
//     public Node shipFireEffect = null; // Masukkan api yang menempel di KRI Golok ke sini

//     private static int hitCount = 0; // Menggunakan 'static' agar jumlah hit tersimpan meski rudal di-reload
//     private int maxHitToBurn = 6;
//     // ------------------------------------

//     private bool isLaunched = false;
//     private bool isExploded = false;
//     private vec3 currentVelocity = vec3.ZERO;
//     private Node initialParent;
//     private dvec3 localPosition;
//     private quat localRotation;

//     protected override void OnReady()
//     {
//         initialParent = node.Parent;
//         localPosition = node.Position;
//         localRotation = node.GetRotation();

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         // Jangan disable shipFireEffect di sini agar tidak mematikan api yang sudah menyala dari hit sebelumnya
//     }

//     void Update()
//     {
//         if (Input.IsKeyDown(Input.KEY.R)) ReloadMissile();

//         if (!isLaunched && !isExploded)
//         {
//             if (CheckInput()) Launch();
//         }

//         if (isLaunched && !isExploded)
//         {
//             MoveMissile();
//             CheckCollision();
//         }
//     }

//     private bool CheckInput()
//     {
//         Input.KEY targetKey = Input.KEY.DIGIT_1;
//         if (keyBinding == "2") targetKey = Input.KEY.DIGIT_2;
//         else if (keyBinding == "3") targetKey = Input.KEY.DIGIT_3;
//         else if (keyBinding == "4") targetKey = Input.KEY.DIGIT_4;
//         else if (keyBinding == "5") targetKey = Input.KEY.DIGIT_5;
//         else if (keyBinding == "6") targetKey = Input.KEY.DIGIT_6;
//         else if (keyBinding == "7") targetKey = Input.KEY.DIGIT_7;
//         else if (keyBinding == "8") targetKey = Input.KEY.DIGIT_8;

//         return Input.IsKeyDown(targetKey);
//     }

//     private void Launch()
//     {
//         dvec3 currentWorldPos = node.WorldPosition;
//         quat currentWorldRot = node.GetWorldRotation();
//         node.Parent = null;
//         node.WorldPosition = currentWorldPos;
//         node.SetWorldRotation(currentWorldRot);

//         isLaunched = true;
//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = true;
//     }

//     private void MoveMissile()
//     {
//         float dt = Game.IFps;
//         vec3 currentPos = (vec3)node.WorldPosition;
//         vec3 targetDir;

//         if (targetNode != null)
//         {
//             vec3 targetPos = (vec3)targetNode.WorldPosition + new vec3(0, 0, 4.0f);
//             targetDir = MathLib.Normalize(targetPos - currentPos);
//         }
//         else
//         {
//             targetDir = node.GetWorldDirection(MathLib.AXIS.Y);
//         }

//         float currentHeight = currentPos.z - waterLevel;
//         if (currentHeight < seaSkimmingHeight)
//         {
//             float forceUp = (seaSkimmingHeight - currentHeight) * 2.0f;
//             targetDir.z += forceUp;
//         }

//         targetDir = MathLib.Normalize(targetDir);
//         vec3 forward = node.GetWorldDirection(MathLib.AXIS.Y);
//         vec3 smoothDir = MathLib.Lerp(forward, targetDir, turnSpeed * dt);

//         currentVelocity = smoothDir * speed;
//         node.WorldTranslate(currentVelocity * dt);
//         node.SetWorldDirection(smoothDir, vec3.UP, MathLib.AXIS.Y);
//     }

//     private void CheckCollision()
//     {
//         if (targetNode != null)
//         {
//             float distance = (float)MathLib.Distance(node.WorldPosition, targetNode.WorldPosition);
//             if (distance < 10.0f)
//             {
//                 // HIT TERDETEKSI!
//                 hitCount++;
//                 Log.Message($"Target TERKENA! Total Hit: {hitCount}\n");

//                 Explode(false);
//                 return;
//             }
//         }

//         if (node.WorldPosition.z < (waterLevel - 0.5f))
//         {
//             Explode(true);
//         }
//     }

//     void Explode(bool hitWater)
//     {
//         isExploded = true;
//         dvec3 hitPos = node.WorldPosition;

//         Node activeExplosion = hitWater ? water_explosion : explosion1;

//         if (activeExplosion != null)
//         {
//             activeExplosion.Parent = null;
//             activeExplosion.WorldPosition = hitPos;
//             activeExplosion.Enabled = true;
//         }

//         // --- CEK APAKAH SUDAH 4x HIT ---
//         if (!hitWater && hitCount >= maxHitToBurn)
//         {
//             if (shipFireEffect != null) shipFireEffect.Enabled = true;
//             Log.Message("KRI GOLOK TERBAKAR !\n");
//         }

//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;
//         node.WorldPosition = new dvec3(0, 0, -10000);
//         // node.Enabled = false; // Tetap biarkan Enabled agar bisa Reload
//     }

//     void ReloadMissile()
//     {
//         isLaunched = false;
//         isExploded = false;

//         // 1. Reset hitungan menjadi 0 kembali
//         hitCount = 0;

//         // 2. Padamkan api di kapal musuh
//         if (shipFireEffect != null)
//             shipFireEffect.Enabled = false;

//         // 3. Reset posisi rudal ke KRI Belati
//         node.Parent = initialParent;
//         node.Position = (vec3)localPosition;
//         node.SetRotation(localRotation);
//         node.Enabled = true;

//         // 4. Reset efek-efek rudal
//         if (explosion1 != null)
//         {
//             explosion1.Parent = node;
//             explosion1.Enabled = false;
//             explosion1.Position = vec3.ZERO;
//         }
//         if (water_explosion != null)
//         {
//             water_explosion.Parent = node;
//             water_explosion.Enabled = false;
//             water_explosion.Position = vec3.ZERO;
//         }
//         if (vfx_jet_engine != null) vfx_jet_engine.Enabled = false;

//         Log.Message("Sistem Reset Total: Rudal siap & KRI Golok pulih.\n");
//     }

// }