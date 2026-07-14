#region Math Variables
#if UNIGINE_DOUBLE
using Scalar = System.Double;
using Vec2 = Unigine.dvec2;
using Vec3 = Unigine.dvec3;
using Vec4 = Unigine.dvec4;
using Mat4 = Unigine.dmat4;
#else
using Scalar = System.Single;
using Vec2 = Unigine.vec2;
using Vec3 = Unigine.vec3;
using Vec4 = Unigine.vec4;
using Mat4 = Unigine.mat4;
using WorldBoundBox = Unigine.BoundBox;
using WorldBoundSphere = Unigine.BoundSphere;
using WorldBoundFrustum = Unigine.BoundFrustum;
#endif
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;
using System.Runtime.Intrinsics.X86;

[Component(PropertyGuid = "ce85d094de1c49262f2b7a9a79b59af44e14042d")]
public class BuoyancyKapal : Component
{
    [ShowInEditor]
    [ParameterSlider(Min = 0.0f)]
    private float mass = 1.0f;

    [ShowInEditor]
    private Node pointFrontCenter = null;

    [ShowInEditor]
    private Node pointBackLeft = null;

    [ShowInEditor]
    private Node pointBackRight = null;

    private ObjectWaterGlobal water = null;

    private Scalar singleValue = 1.0f;
    private Scalar zeroValue = 0.0f;
    private float tempcommand;
    private int Buoy_flag;

    public float waterHeightOffset = 0.0f;


    // private 
    // private int Object_id;
    // float angka;

    private void Init()
    {
        water = World.GetNodeByType((int)Node.TYPE.OBJECT_WATER_GLOBAL) as ObjectWaterGlobal;
        if (water == null)
            Log.ErrorLine("BuoyComponent Init(): can't find ObjectWaterGlobal on scene!");

        // angka = 0;
    }

    private void UpdatePhysics()
    {
        // angka = 1;
        float massLerpC = mass * 0.01f;
        if (mass == 0.0f)
            massLerpC = 0.00001f;

        if (water)
        {
            if (node.ObjectBodyRigid.Enabled)
            {
                // get water height in entity position
                Vec3 point0 = pointFrontCenter.WorldPosition;
                Vec3 point1 = pointBackLeft.WorldPosition;
                Vec3 point2 = pointBackRight.WorldPosition;

                //create basis by 3 point
                Vec3 tmpZ = MathLib.Normalize(MathLib.Cross(MathLib.Normalize(point1 - point0), MathLib.Normalize(point2 - point0)));
                if (MathLib.Angle(tmpZ, Vec3.UP) > 90)
                    tmpZ = -tmpZ;
                Vec3 tmpY = MathLib.Normalize(point1 - point0);
                Vec3 tmpX = tmpY;
                tmpX = MathLib.Cross(tmpY, tmpZ).Normalized;
                tmpY = MathLib.Cross(tmpZ, tmpX).Normalized;
                Mat4 oldBasis = Mat4.IDENTITY;
                oldBasis.Translate = point0;
                oldBasis.SetColumn3(0, tmpX);
                oldBasis.SetColumn3(1, tmpY);
                oldBasis.SetColumn3(2, tmpZ);

                // find height of each point
                Scalar h0 = water.FetchHeight(new Vec3(point0.x, point0.y, 0.0f)) - (Scalar)waterHeightOffset;
                Scalar h1 = water.FetchHeight(new Vec3(point1.x, point1.y, 0.0f)) - (Scalar)waterHeightOffset;
                Scalar h2 = water.FetchHeight(new Vec3(point2.x, point2.y, 0.0f)) - (Scalar)waterHeightOffset;

                // // find height of each point
                // Scalar h0 = water.FetchHeight(new Vec3(point0.x, point0.y, 0.0f));
                // Scalar h1 = water.FetchHeight(new Vec3(point1.x, point1.y, 0.0f));
                // Scalar h2 = water.FetchHeight(new Vec3(point2.x, point2.y, 0.0f));

                // Scalar lerpK = Game.IFps * Ombak.GlobalBuoancy / massLerpC;
                Scalar lerpK = Game.IFps * 0.1f / massLerpC;
                Scalar diff = MathLib.Max(MathLib.Abs((h0 - point0.z) + (h1 - point1.z) + (h2 - point2.z)), singleValue);
                lerpK = MathLib.Clamp(lerpK * diff, zeroValue, singleValue);

                point0.z = MathLib.Lerp(point0.z, h0, lerpK);
                point1.z = MathLib.Lerp(point1.z, h1, lerpK);
                point2.z = MathLib.Lerp(point2.z, h2, lerpK);

                // calculate new basis for changed point
                tmpZ = MathLib.Normalize(MathLib.Cross(MathLib.Normalize(point1 - point0), MathLib.Normalize(point2 - point0)));
                tmpY = MathLib.Normalize(point1 - point0);
                tmpX = MathLib.Cross(tmpY, tmpZ).Normalized;
                tmpY = MathLib.Cross(tmpZ, tmpX).Normalized;
                Mat4 newBasis = Mat4.IDENTITY;
                newBasis.Translate = point0;
                newBasis.SetColumn3(0, tmpX);
                newBasis.SetColumn3(1, tmpY);
                newBasis.SetColumn3(2, tmpZ);

                // calculate translation from old basis to new basis
                Mat4 translationBasis = MathLib.Mul(newBasis, MathLib.Inverse(oldBasis));

                Mat4 nodeTransform = node.ObjectBodyRigid.Transform;
                // apply transformation to node transform
                Mat4 newTransform = MathLib.Mul(translationBasis, nodeTransform);

                BodyRigid rb = node.ObjectBodyRigid;

                // linear velocity
                vec3 nextVelocity = rb.LinearVelocity + Physics.Gravity * Physics.IFps;

                Vec3 deltaPos = newTransform.Translate - nodeTransform.Translate;
                vec3 addVelocity = new vec3(deltaPos / Physics.IFps);

                rb.AddLinearImpulse(vec3.UP * rb.Mass * (-nextVelocity + addVelocity * 0.5f));

                // angular velocity
                vec3 angularVelocity = rb.AngularVelocity;

                quat deltaRot = newTransform.GetRotate() * MathLib.Inverse(nodeTransform.GetRotate());
                float quatAngle = 0.0f;
                vec3 quatAxis = vec3.ZERO;
                GetAxisAngle(deltaRot, out quatAxis, out quatAngle);

                vec3 addAngularVelocity = quatAxis * (quatAngle / Physics.IFps);

                rb.AddAngularImpulse((-angularVelocity + addAngularVelocity * 0.01f) * rb.Inertia * 0.1f);
            }

            else if (node.ObjectBodyRigid.Enabled == false)
            {
                Mat4 nodeTransform = node.WorldTransform;

                // get water height in entity position
                Vec3 point0 = pointFrontCenter.WorldPosition;
                Vec3 point1 = pointBackLeft.WorldPosition;
                Vec3 point2 = pointBackRight.WorldPosition;

                //create basis by 3 point
                Vec3 tmpZ = MathLib.Normalize(MathLib.Cross(MathLib.Normalize(point1 - point0), MathLib.Normalize(point2 - point0)));
                if (MathLib.Angle(tmpZ, Vec3.UP) > 90)
                    tmpZ = -tmpZ;
                Vec3 tmpY = MathLib.Normalize(point1 - point0);
                Vec3 tmpX = tmpY;
                tmpX = MathLib.Cross(tmpY, tmpZ).Normalized;
                tmpY = MathLib.Cross(tmpZ, tmpX).Normalized;
                Mat4 oldBasis = Mat4.IDENTITY;
                oldBasis.Translate = point0;
                oldBasis.SetColumn3(0, tmpX);
                oldBasis.SetColumn3(1, tmpY);
                oldBasis.SetColumn3(2, tmpZ);


                // find height of each point
                Scalar h0 = water.FetchHeight(new Vec3(point0.x, point0.y, 0.0f));
                Scalar h1 = water.FetchHeight(new Vec3(point1.x, point1.y, 0.0f));
                Scalar h2 = water.FetchHeight(new Vec3(point2.x, point2.y, 0.0f));

                Scalar lerpK = Game.IFps * 0.25f / massLerpC;
                Scalar diff = MathLib.Max(MathLib.Abs((h0 - point0.z) + (h1 - point1.z) + (h2 - point2.z)), singleValue);
                lerpK = MathLib.Clamp(lerpK * diff, zeroValue, singleValue);

                point0.z = MathLib.Lerp(point0.z, h0, lerpK);
                point1.z = MathLib.Lerp(point1.z, h1, lerpK);
                point2.z = MathLib.Lerp(point2.z, h2, lerpK);

                // calculate new basis for changed point
                tmpZ = MathLib.Normalize(MathLib.Cross(MathLib.Normalize(point1 - point0), MathLib.Normalize(point2 - point0)));
                tmpY = MathLib.Normalize(point1 - point0);
                tmpX = MathLib.Cross(tmpY, tmpZ).Normalized;
                tmpY = MathLib.Cross(tmpZ, tmpX).Normalized;
                Mat4 newBasis = Mat4.IDENTITY;
                newBasis.Translate = point0;
                newBasis.SetColumn3(0, tmpX);
                newBasis.SetColumn3(1, tmpY);
                newBasis.SetColumn3(2, tmpZ);

                // calculate translation from old basis to new basis
                Mat4 translationBasis = translationBasis = MathLib.Mul(newBasis, MathLib.Inverse(oldBasis));

                // apply transformation to node transform
                Mat4 newTransform = MathLib.Mul(translationBasis, nodeTransform);

                node.WorldTransform = newTransform;
            }

        }

    }

    static private void GetAxisAngle(quat rot, out vec3 axis, out float angle)
    {
        float ilength = MathLib.Rsqrt(rot.x * rot.x + rot.y * rot.y + rot.z * rot.z);
        axis = new vec3(rot.x * ilength, rot.y * ilength, rot.z * ilength);
        angle = MathLib.Acos(MathLib.Clamp(rot.w, -1.0f, 1.0f)) * MathLib.RAD2DEG * 2.0f;
        if (angle > 180.0f)
            angle -= 360.0f;

        axis = new vec3(axis.x, axis.y, axis.z);
    }
}