using System.Collections;
using System.Collections.Generic;
using Unigine;

#if UNIGINE_DOUBLE
using Vec3 = Unigine.dvec3;
using Vec4 = Unigine.dvec4;
using Mat4 = Unigine.dmat4;
#else
using Vec3 = Unigine.vec3;
using Vec4 = Unigine.vec4;
using Mat4 = Unigine.mat4;
#endif

[Component(PropertyGuid = "19eb171dd23b50e3072257ad15a44ab85b46c59f")]
public class move : Component
{
    public float movement_speed = 15.5f;
    public float rotation_speed = 15.0f;
    public Node propellerL;
    public Node propellerR;
    public float propeller_speed = 5000.0f;
    float current_speed = 0.0f;
    float current_turn = 0.0f;
    public bool EngineOn = false;
    public float CurrentSpeedMS = 0.0f;
    public int CurrentGear = 0;
    public int throttleLevel = 0;
    public int maxThrottle = 5;
    float throttleSmooth = 0.0f;
    float inputDelay = 0.2f;
    float inputTimer = 0.0f;
    public float accelerationFactor = 0.5f;
    public float turnAccelerationFactor = 0.5f;

    void Init()
    {
        ControlsApp.SetStateKey(Controls.STATE_AUX_0, Input.KEY.I);
        ControlsApp.SetStateKey(Controls.STATE_AUX_1, Input.KEY.J);
        ControlsApp.SetStateKey(Controls.STATE_AUX_2, Input.KEY.K);
        ControlsApp.SetStateKey(Controls.STATE_AUX_3, Input.KEY.L);

        propellerL = node.FindNode("PropellerL");
        propellerR = node.FindNode("PropellerR");
    }

    void Update()
    {
        float ifps = Game.IFps;

        Mat4 transform = node.WorldTransform;
        Vec3 direction = transform.GetColumn3(1);

        Mat4 rotation = Mat4.IDENTITY;
        Vec3 delta_movement = new Vec3(0.0f);
        bool update_transform = false;

        if (!EngineOn)
        {
            throttleLevel = 0;
        }

        float target_speed = (throttleLevel / (float)maxThrottle) * movement_speed;

        current_speed = MathLib.Lerp(current_speed, target_speed, accelerationFactor * ifps);

        delta_movement = direction * current_speed * ifps;

        if (MathLib.Abs(current_speed) > 0.01f)
            update_transform = true;

        float target_turn = 0.0f;

        if (EngineOn)
        {
            if (Input.IsKeyPressed(Input.KEY.J))
                target_turn = rotation_speed;

            if (Input.IsKeyPressed(Input.KEY.L))
                target_turn = -rotation_speed;
        }

        current_turn = MathLib.Lerp(current_turn, target_turn, turnAccelerationFactor * ifps);

        // float speed_factor = MathLib.Clamp(MathLib.Abs(current_speed) / movement_speed, 0.0f, 1.0f);
        // float min_turn_factor = 0.2f;

        // speed_factor = MathLib.Max(speed_factor, min_turn_factor);
        // current_turn *= speed_factor;

        if (MathLib.Abs(current_turn) > 0.01f)
        {
            float idle_turn_multiplier = 0.5f;
            if (MathLib.Abs(current_turn) > 0.01f)
            {
                rotation.SetRotateZ(current_turn * idle_turn_multiplier * ifps);
                update_transform = true;
            }
        }
        else
        {
            float speed_factor = MathLib.Clamp(MathLib.Abs(current_speed) / movement_speed, 0.0f, 1.0f);

            float final_turn = current_turn * speed_factor;

            if (MathLib.Abs(final_turn) > 0.01f)
            {
                rotation.SetRotateZ(final_turn * ifps);
                update_transform = true;
            }
        }

        if (update_transform)
        {
            transform = transform * rotation;
            transform.SetColumn3(3, transform.GetColumn3(3) + delta_movement);
            node.WorldTransform = transform;
        }

        if (EngineOn && MathLib.Abs(current_speed) > 0.01f)
        {
            float spin = propeller_speed * ifps;

            if (throttleLevel < 0)
                spin = -spin;

            quat rot = new quat(0, 1, 0, spin);

            if (propellerL != null)
                propellerL.SetWorldRotation(propellerL.GetWorldRotation() * rot);

            if (propellerR != null)
                propellerR.SetWorldRotation(propellerR.GetWorldRotation() * rot);
        }

        CurrentSpeedMS = current_speed;
        CurrentGear = throttleLevel;

        // Visualizer.RenderDirection(node.WorldPosition, new vec3(direction), new vec4(1.0f, 0.0f, 0.0f, 1.0f), 0.1f, false);

        // float target_speed = 0.0f;

        // if (ControlsApp.GetState(Controls.STATE_AUX_0) == 1)
        // {
        //     target_speed = movement_speed;
        // }
        // else if (ControlsApp.GetState(Controls.STATE_AUX_2) == 1)
        // {
        //     target_speed = -movement_speed;
        // }

        // current_speed = MathLib.Lerp(current_speed, target_speed, accelerationFactor * ifps);

        // delta_movement = direction * current_speed * ifps;

        // if (MathLib.Abs(current_speed) > 0.01f)
        // {
        //     update_transform = true;
        // }

        // float target_turn = 0.0f;

        // if (ControlsApp.GetState(Controls.STATE_AUX_1) == 1)
        // {
        //     target_turn = rotation_speed;
        // }
        // else if (ControlsApp.GetState(Controls.STATE_AUX_3) == 1)
        // {
        //     target_turn = -rotation_speed;
        // }

        // current_turn = MathLib.Lerp(current_turn, target_turn, turnAccelerationFactor * ifps);

        // if (MathLib.Abs(current_turn) > 0.01f)
        // {
        //     rotation.SetRotateZ(current_turn * ifps);
        //     update_transform = true;
        // }

        //===================================================================================================

        // if (ControlsApp.GetState(Controls.STATE_AUX_0) == 1)
        // {
        //     delta_movement = direction * movement_speed * ifps;
        //     update_transform = true;
        // }

        // if (ControlsApp.GetState(Controls.STATE_AUX_1) == 1)
        // {
        //     rotation.SetRotateZ(rotation_speed * ifps);
        //     update_transform = true;
        // }

        // if (ControlsApp.GetState(Controls.STATE_AUX_2) == 1)
        // {
        //     delta_movement = -direction * movement_speed * ifps;
        //     update_transform = true;
        // }

        // if (ControlsApp.GetState(Controls.STATE_AUX_3) == 1)
        // {
        //     rotation.SetRotateZ(-rotation_speed * ifps);
        //     update_transform = true;
        // }

        //===================================================================================================

        // if (update_transform)
        // {
        //     transform = transform * rotation;
        //     transform.SetColumn3(3, transform.GetColumn3(3) + delta_movement);

        //     node.WorldTransform = transform;
        // }

        // // pergerakan propeller
        // if (delta_movement.Length > 0.0f)

        // {
        //     quat rot = new quat(0, 1, 0, propeller_speed * ifps);

        //     if (propellerL != null)
        //     {
        //         propellerL.SetWorldRotation(propellerL.GetWorldRotation() * rot);
        //     }

        //     if (propellerR != null)
        //     {
        //         propellerR.SetWorldRotation(propellerR.GetWorldRotation() * rot);
        //     }
    }
}
