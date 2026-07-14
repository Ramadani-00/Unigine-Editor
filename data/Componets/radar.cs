using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "54236af33b1875083aca2b8e3f1b306e7aaab2b3")]
public class radar : Component
{
    public float radarSpeed = 120.0f;

    private void Update()
    {
        float ifps = Game.IFps;
        quat rot = new quat(0, 0, 1, -radarSpeed * ifps);
        node.SetRotation(node.GetRotation() * rot);
    }
}
