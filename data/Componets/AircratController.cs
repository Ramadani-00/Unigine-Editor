using System;
using Unigine;
//using Unigine.Math;

[Component(PropertyGuid = "7b4c38cbe7953c906cdfe59f1663811e5d5608a0")]
public class AircraftController : Component
{
    public float move_speed = 20.0f;
    public float turn_speed = 60.0f;
    public float lift_force = 15.0f;
    public float gravity = 5.0f;

    private bool isFlying = false;
    private float current_speed = 0.0f;

    void Update()
    {
        float ifps = Game.IFps;

        float forward = 0.0f;
        float turn = 0.0f;

        if (Input.IsKeyPressed(Input.KEY.T)) forward += 1.0f; // maju
        if (Input.IsKeyPressed(Input.KEY.G)) forward -= 1.0f; // mundur
        if (Input.IsKeyPressed(Input.KEY.F)) turn += 1.0f;    // kiri
        if (Input.IsKeyPressed(Input.KEY.H)) turn -= 1.0f;    // kanan

        node.Rotate(0, 0, turn * turn_speed * ifps);

        // SPEED
        current_speed = forward * move_speed;
        vec3 forward_dir = (vec3)node.WorldTransform.AxisY;
        if (!isFlying)
        {
            node.Translate(forward_dir * current_speed * ifps);

            // TAKEOFF (V)
            if (Input.IsKeyPressed(Input.KEY.V) && current_speed > 10.0f)
            {
                isFlying = true;
            }
        }
        else
        {
            node.Translate(forward_dir * current_speed * ifps);

            // NAIK (V)
            if (Input.IsKeyPressed(Input.KEY.V))
            {
                node.Translate(0, 0, lift_force * ifps);
            }

            // TURUN (B)
            if (Input.IsKeyPressed(Input.KEY.B))
            {
                node.Translate(0, 0, -lift_force * ifps);
            }

            // GRAVITASI
            node.Translate(0, 0, -gravity * ifps);

            // LANDING OTOMATIS
            if (node.WorldPosition.z < 2.0f)
            {
                isFlying = false;
            }
        }
    }
}