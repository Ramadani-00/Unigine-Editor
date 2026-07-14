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

[Component(PropertyGuid = "5c7287d287d91c66bc5f6e504b55dceee5bb3072")]
public class majumanual : Component
{
    // define the movement speed
    public float movement_speed = 18.0f;
    // define the rotation speed
    public float rotation_speed = 30.0f;

    private void Init()
    {
        // check if the key is pressed and update the state of the specified control
        // you can use 'I', 'O', 'P' keys
        ControlsApp.SetStateKey(Controls.STATE_AUX_0, Input.KEY.T);
        ControlsApp.SetStateKey(Controls.STATE_AUX_1, Input.KEY.F);
        ControlsApp.SetStateKey(Controls.STATE_AUX_2, Input.KEY.H);
    }

    private void Update()
    {
        // get the frame duration
        float ifps = Game.IFps;

        // enable visualizer
        Visualizer.Enabled = true;

        // get the current world transform matrix of the mesh
        Mat4 transform = node.WorldTransform;

        // get the direction vector of the mesh from the second column of the transformation matrix
        Vec3 direction = transform.GetColumn3(1);

        // initialize rotation and movement and update flag
        Mat4 rotation = Mat4.IDENTITY;
        Vec3 delta_movement = new Vec3(0.0f);
        bool update_transform = false;

        // render the direction vector for visual clarity
        Visualizer.RenderDirection(node.WorldPosition, new vec3(direction), new vec4(1.0f, 0.0f, 0.0f, 1.0f), 0.1f, false);

        // check if the control key for movement is pressed
        if (ControlsApp.GetState(Controls.STATE_AUX_0) == 1)
        {
            // calculate the delta of movement
            delta_movement = direction * movement_speed * ifps;
            update_transform = true;
        }

        // check if the control key for left rotation is pressed
        if (ControlsApp.GetState(Controls.STATE_AUX_1) == 1)
        {
            // set the node's left rotation along the Z axis
            rotation.SetRotateZ(rotation_speed * ifps);
            update_transform = true;
        }

        // check if the control key for right rotation is pressed
        else if (ControlsApp.GetState(Controls.STATE_AUX_2) == 1)
        {
            // set the node's right rotation along the Z axis
            rotation.SetRotateZ(-rotation_speed * ifps);
            update_transform = true;
        }

        // update transformation if necessary
        if (update_transform)
        {
            // combine transformations: movement + rotation
            transform = transform * rotation;
            transform.SetColumn3(3, transform.GetColumn3(3) + delta_movement);

            // set the resulting transformation
            node.WorldTransform = transform;
        }
    }

}