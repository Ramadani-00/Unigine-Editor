using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "9cab68ca670977959b529ffe4c431fd4fd24d178")]
public class elevasi : Component
{
    public float turnSpeed = 40.0f;

    private void Update()
    {

        float ifps = Game.IFps;

        if (Input.IsKeyPressed(Input.KEY.V))
        {
            quat rot = new quat(1, 0, 0, turnSpeed * ifps);
            node.SetWorldRotation(node.GetWorldRotation() * rot);
        }

        if (Input.IsKeyPressed(Input.KEY.B))
        {
            quat rot = new quat(1, 0, 0, -turnSpeed * ifps);
            node.SetWorldRotation(node.GetWorldRotation() * rot);
        }
    }
}