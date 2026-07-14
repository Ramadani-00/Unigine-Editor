using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "96e800a7ff3500c424b45063db8ebf5018ecd2f9")]
public class bearing : Component
{
    public float turnSpeed = 40.0f;

    private void Update()
    {

        float ifps = Game.IFps;

        if (Input.IsKeyPressed(Input.KEY.N))
        {
            quat rot = new quat(0, 0, 1, turnSpeed * ifps);
            node.SetWorldRotation(node.GetWorldRotation() * rot);
        }

        if (Input.IsKeyPressed(Input.KEY.M))
        {
            quat rot = new quat(0, 0, 1, -turnSpeed * ifps);
            node.SetWorldRotation(node.GetWorldRotation() * rot);
        }
    }
}