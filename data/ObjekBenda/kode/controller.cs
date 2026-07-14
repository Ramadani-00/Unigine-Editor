using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "3dc35811ab929a1dd060d5901c43dc53601754c9")]
public class controller : Component
{
    Node box1, box2, box3, box4, box5;
    dvec3 pos1, pos2, pos3, pos4, pos5;
    dvec3 target1, target2, target3, target4, target5;

    bool isMerged = true;
    bool keyPressed = false;

    float speed = 5.0f;
    float smoothSpeed = 5.0f;

    void Init()
    {
        box1 = World.GetNodeByName("Box_1");
        box2 = World.GetNodeByName("Box_2");
        box3 = World.GetNodeByName("Box_3");
        box4 = World.GetNodeByName("Box_4");
        box5 = World.GetNodeByName("Box_5");

        // simpan posisi awal
        pos1 = box1.WorldPosition;
        pos2 = box2.WorldPosition;
        pos3 = box3.WorldPosition;
        pos4 = box4.WorldPosition;
        pos5 = box5.WorldPosition;

        target1 = pos1;
        target2 = pos2;
        target3 = pos3;
        target4 = pos4;
        target5 = pos5;

    }

    void Update()
    {
        if (Input.IsKeyPressed(Input.KEY.M))
        {
            if (!keyPressed)
            {
                isMerged = !isMerged;

                if (!isMerged)
                {
                    float spread = 3.0f;

                    // Menentukan posisi target baru secara acak di sekitar posisi awal
                    // Setiap box akan menyebar (pecah) dengan offset random di sumbu X dan Y
                    // Nilai random diambil antara -spread sampai +spread
                    // Z = 0 supaya tetap di bidang yang sama (tidak maju/mundur)
                    target1 = pos1 + new dvec3(Game.GetRandomFloat(-spread, spread), Game.GetRandomFloat(-spread, spread), 0);
                    target2 = pos2 + new dvec3(Game.GetRandomFloat(-spread, spread), Game.GetRandomFloat(-spread, spread), 0);
                    target3 = pos3 + new dvec3(Game.GetRandomFloat(-spread, spread), Game.GetRandomFloat(-spread, spread), 0);
                    target4 = pos4 + new dvec3(Game.GetRandomFloat(-spread, spread), Game.GetRandomFloat(-spread, spread), 0);
                    target5 = pos5 + new dvec3(Game.GetRandomFloat(-spread, spread), Game.GetRandomFloat(-spread, spread), 0);
                }

                else
                {
        
                    // Mengembalikan semua box ke posisi awal (formasi semula)
                    // target di-set sama dengan posisi awal yang sudah disimpan di awal (pos1 - pos5)
                    // nantinya box akan bergerak halus (lerp) menuju posisi ini
                    target1 = pos1;
                    target2 = pos2;
                    target3 = pos3;
                    target4 = pos4;
                    target5 = pos5;

                    // Reset rotasi semua box ke kondisi awal (tanpa rotasi)
                    // quat.IDENTITY berarti rotasi default (0 derajat di semua sumbu)
                    // Digunakan agar saat box kembali menyatu, posisinya rapi dan tidak miring
                    box1.SetWorldRotation(quat.IDENTITY);
                    box2.SetWorldRotation(quat.IDENTITY);
                    box3.SetWorldRotation(quat.IDENTITY);
                    box4.SetWorldRotation(quat.IDENTITY);
                    box5.SetWorldRotation(quat.IDENTITY);

                }

                keyPressed = true;
            }
        }
        else keyPressed = false;

        float t = Game.IFps * smoothSpeed;

        // Menggerakkan setiap box secara halus menuju posisi target menggunakan Lerp
        // Lerp (Linear Interpolation) membuat perpindahan tidak langsung lompat (smooth)
        // box akan bergerak sedikit demi sedikit dari posisi sekarang ke target
        // nilai t menentukan seberapa cepat pergerakan (dipengaruhi FPS dan smoothSpeed)
        box1.WorldPosition = MathLib.Lerp(box1.WorldPosition, target1, t);
        box2.WorldPosition = MathLib.Lerp(box2.WorldPosition, target2, t);
        box3.WorldPosition = MathLib.Lerp(box3.WorldPosition, target3, t);
        box4.WorldPosition = MathLib.Lerp(box4.WorldPosition, target4, t);
        box5.WorldPosition = MathLib.Lerp(box5.WorldPosition, target5, t);

        if (isMerged)
        {
            dvec3 move = new dvec3(0, 0, 0);

            if (Input.IsKeyPressed(Input.KEY.I)) move.y += 1;
            if (Input.IsKeyPressed(Input.KEY.K)) move.y -= 1;
            if (Input.IsKeyPressed(Input.KEY.J)) move.x -= 1;
            if (Input.IsKeyPressed(Input.KEY.L)) move.x += 1;

            move *= speed * Game.IFps;

            // box3.WorldPosition += move;
            // dvec3 center = box3.WorldPosition;

            // geser semua target (bukan langsung posisi!)
            target1 += move;
            target2 += move;
            target3 += move;
            target4 += move;
            target5 += move;
        }
        else
        {
            if (Input.IsKeyPressed(Input.KEY.DIGIT_1))
                target1 += new dvec3(0, speed * Game.IFps, 0);

            if (Input.IsKeyPressed(Input.KEY.DIGIT_2))
                target2 += new dvec3(0, speed * Game.IFps, 0);

            if (Input.IsKeyPressed(Input.KEY.DIGIT_3))
                target3 += new dvec3(0, speed * Game.IFps, 0);

            if (Input.IsKeyPressed(Input.KEY.DIGIT_4))
                target4 += new dvec3(0, speed * Game.IFps, 0);

            if (Input.IsKeyPressed(Input.KEY.DIGIT_5))
                target5 += new dvec3(0, speed * Game.IFps, 0);
        }
    }
}