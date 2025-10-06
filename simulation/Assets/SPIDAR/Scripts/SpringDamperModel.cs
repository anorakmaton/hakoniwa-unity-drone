//
// SpringDamperModel.cs
//

using UnityEngine;
using System.Collections;

//
// class SpringDamperModel
//
public class SpringDamperModel
{
    public float SpringK = 0;
    public float DamperB = 0;

    public Pose pointerOrigin = Pose.zero;
    public Pose rigidbodyOrigin = Pose.zero;

    //
    public SpringDamperModel()
    {
        this.Clear();
    }

    //
    public void Clear()
    {
        SpringK = 0;
        DamperB = 0;

        pointerOrigin = Pose.zero;
        rigidbodyOrigin = Pose.zero;
    }

    //
    public Vector3 CalcForce(Pose pointer, Rigidbody rigidbody)
    {
        Vector3 pointerPosition = pointer.position - pointerOrigin.position;
        Vector3 rigidbodyPosition = rigidbody.position - rigidbodyOrigin.position;

        Vector3 position = pointerPosition - rigidbodyPosition;
        Vector3 velocity = pointer.velocity - rigidbody.linearVelocity;

        return SpringK * position + DamperB * velocity;
    }

    //
    public Vector3 CalcTorque(Pose pointer, Rigidbody rigidbody)
    {
        Quaternion pointerRotation = QuaternionUtility.Rotate(pointerOrigin.rotation, pointer.rotation);
        Quaternion rigidbodyRotation = QuaternionUtility.Rotate(rigidbodyOrigin.rotation, rigidbody.rotation);

        Quaternion qd;

        if (Quaternion.Dot(pointerRotation, rigidbodyRotation) > 0)
        {
            qd = QuaternionUtility.Subtract(pointerRotation, rigidbodyRotation);
        }
        else
        {
            qd = QuaternionUtility.Add(pointerRotation, rigidbodyRotation);
        }

        Quaternion conj = QuaternionUtility.Conjugated(pointerRotation);

        Quaternion temp = qd * conj;

        Vector3 angle = Vector3.zero;

        angle.x = temp.x * 2.0f;
        angle.y = temp.y * 2.0f;
        angle.z = temp.z * 2.0f;

        if (angle.x > Mathf.PI) angle.x -= Mathf.PI * 2;
        else if (angle.x < -Mathf.PI) angle.x += Mathf.PI * 2;

        if (angle.y > Mathf.PI) angle.y -= Mathf.PI * 2;
        else if (angle.y < -Mathf.PI) angle.y += Mathf.PI * 2;

        if (angle.z > Mathf.PI) angle.z -= Mathf.PI * 2;
        else if (angle.z < -Mathf.PI) angle.z += Mathf.PI * 2;

        Vector3 angularVelocity = pointer.angularVelocity - rigidbody.angularVelocity;

        return SpringK * angle + DamperB * angularVelocity;
    }
} // end of class SpringDamperModel
