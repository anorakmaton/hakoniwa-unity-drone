//
// Pose.cs 
//

using UnityEngine;
using System.Collections;

using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;

//
// struct Pose
//
public struct Pose
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;

    //
    public static explicit operator Pose(Rigidbody rb)
    {
        Pose temp;
        temp.position = rb.position;
        temp.rotation = rb.rotation;
        temp.velocity = rb.linearVelocity;
        temp.angularVelocity = rb.angularVelocity;
        return temp;
    }

    //
    public static Pose zero
    {
        get
        {
            Pose temp;

            temp.position = Vector3.zero;
            temp.rotation = Quaternion.identity;
            temp.velocity = Vector3.zero;
            temp.angularVelocity = Vector3.zero;

            return temp;
        }
    }

    //
    public static Pose Lerp(Pose from, Pose to, float t)
    {
        Pose temp;

        if (t < 0) t = 0;
        if (t > 1) t = 1;

        temp.position = Vector3.Lerp(from.position, to.position, t);
        temp.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);
        temp.velocity = Vector3.Lerp(from.velocity, to.velocity, t);
        temp.angularVelocity = Vector3.Lerp(from.angularVelocity, to.angularVelocity, t);

        return temp;
    }

} // end of struct Pose.

//
// struct QuaternionUtility
//
public struct QuaternionUtility
{
    //
    public static Quaternion Rotate(Quaternion from, Quaternion to)
    {
        if (Quaternion.Dot(from, to) > 0)
        {
            return to * Quaternion.Inverse(from);
        }
        else
        {
            Quaternion temp = Minus(from);

            return to * Quaternion.Inverse(temp);
        }
    }

    //
    public static Quaternion Conjugated(Quaternion q)
    {
        q.x = -q.x;
        q.y = -q.y;
        q.z = -q.z;

        return q;
    }

    //
    public static Quaternion Minus(Quaternion q)
    {
        q.w = -q.w;
        q.x = -q.x;
        q.y = -q.y;
        q.z = -q.z;

        return q;
    }

    //
    public static Quaternion Subtract(Quaternion lhs, Quaternion rhs)
    {
        lhs.w -= rhs.w;
        lhs.x -= rhs.x;
        lhs.y -= rhs.y;
        lhs.z -= rhs.z;

        return lhs;
    }

    //
    public static Quaternion Add(Quaternion lhs, Quaternion rhs)
    {
        lhs.w += rhs.w;
        lhs.x += rhs.x;
        lhs.y += rhs.y;
        lhs.z += rhs.z;

        return lhs;
    }

    //
    public static Quaternion Derivative(Vector3 v, Quaternion q)
    {
        Quaternion temp;
        temp.w = 0;
        temp.x = v.x;
        temp.y = v.y;
        temp.z = v.z;

        temp *= q;

        temp.w *= 0.5f;
        temp.x *= 0.5f;
        temp.y *= 0.5f;
        temp.z *= 0.5f;

        return temp;
    }

    //
    public static Quaternion Unit(Quaternion q)
    {
        Vector4 temp;

        temp.w = q.w;
        temp.x = q.x;
        temp.y = q.y;
        temp.z = q.z;

        temp = temp.normalized;

        q.w = temp.w;
        q.x = temp.x;
        q.y = temp.y;
        q.z = temp.z;

        return q;
    }

} // end of struct QuaternionUtility.

//
// struct Converter
//
public struct Converter
{
    //
    public static Vector3 ScaleUp(Vector3 src, float scale)
    {
        return src * scale;
    }

    //
    public static Quaternion ScaleUp(Quaternion src, float scale)
    {
        float angle;
        Vector3 axis;

        src.ToAngleAxis(out angle, out axis);

        angle *= scale;

        return Quaternion.AngleAxis(angle, axis);
    }

    //
    public static SpidarVector Convert(Vector3 rhs)
    {
        SpidarVector lhs;

        lhs.x = rhs.x;
        lhs.y = rhs.y;
        lhs.z = rhs.z;

        return lhs;
    }

    //
    public static SpidarQuaternion Convert(Quaternion rhs)
    {
        SpidarQuaternion lhs;

        lhs.x = rhs.x;
        lhs.y = rhs.y;
        lhs.z = rhs.z;
        lhs.w = rhs.w;

        return lhs;
    }

    //
    public static Vector3 Convert(SpidarVector rhs)
    {
        Vector3 lhs;

        lhs.x = rhs.x;
        lhs.y = rhs.y;
        lhs.z = rhs.z;

        return lhs;
    }

    //
    public static Quaternion Convert(SpidarQuaternion rhs)
    {
        Quaternion lhs;

        lhs.x = rhs.x;
        lhs.y = rhs.y;
        lhs.z = rhs.z;
        lhs.w = rhs.w;

        return lhs;
    }

} // end of struct Converter.

// end of file.
