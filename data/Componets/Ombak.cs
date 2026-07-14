using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "47fdd3a09e1c46d1ce0a42d28fd498422592356e")]
public class Ombak : Component
{

    ObjectWaterGlobal water = null;
    void Init()
    {
        // write here code to be called on component initialization
        water = World.GetNodeByType((int)Node.TYPE.OBJECT_WATER_GLOBAL) as ObjectWaterGlobal;
    }

    void Update()
    {
        // write here code to be called before updating each render frame
        if (Input.IsKeyUp(Input.KEY.DIGIT_1))
            water.Beaufort = 1;

        if (Input.IsKeyUp(Input.KEY.DIGIT_2))
            water.Beaufort = 2;

        if (Input.IsKeyUp(Input.KEY.DIGIT_3))
            water.Beaufort = 3;

        if (Input.IsKeyUp(Input.KEY.DIGIT_4))
            water.Beaufort = 4;

        if (Input.IsKeyUp(Input.KEY.DIGIT_5))
            water.Beaufort = 5;

        if (Input.IsKeyUp(Input.KEY.DIGIT_6))
            water.Beaufort = 6;

        if (Input.IsKeyUp(Input.KEY.DIGIT_7))
            water.Beaufort = 7;

        if (Input.IsKeyUp(Input.KEY.DIGIT_8))
            water.Beaufort = 8;

        if (Input.IsKeyUp(Input.KEY.DIGIT_9))
            water.Beaufort = 9;
    }
}