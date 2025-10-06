//
// HoldState.cs
//

using UnityEngine;

public class HoldState : MonoBehaviour
{
    public float InertiaTensor { get { return avgInertiaTensor; } }

    public bool Collision { get { return collision; } }
    public bool CollisionEnter { get { collisionEnterChecked = true; return collisionEnter; } }
    public MonoBehaviour Owner { get; set; }

    private bool collision = true;
    private bool collisionEnter = false;
    private bool collisionEnterChecked = false;

    private uint collisionCount = 0;
    private const uint COLLISION_THRESHOLD = 2;

    private float maxAngularVelocity = 0;
    private Vector3 inertiaTensor = Vector3.zero;
    private float avgInertiaTensor = 0;

    void Start()
    {

    }

    void FixedUpdate()
    {
        // オブジェクトの角等の小さな点で衝突した際に振動を回避するための処理
        if (collisionCount > 0)
        {
            --collisionCount;

            if (collisionCount == 0) collision = false;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (!collision)
        {
            collisionEnter = true;
            collisionEnterChecked = false;
        }
        collision = true;
        collisionCount = 0;
    }

    void OnCollisionStay(Collision other)
    {
        if (collisionEnterChecked) collisionEnter = false;

        collision = true;
        collisionCount = 0;
    }

    void OnCollisionExit(Collision other)
    {
        collisionEnter = false;
        collisionCount = COLLISION_THRESHOLD;
    }

    public void OnHoldObject()
    {
        Rigidbody body = GetComponent<Rigidbody>();

        if (body)
        {
            //
            maxAngularVelocity = body.maxAngularVelocity;
            body.maxAngularVelocity = float.MaxValue;

            //
            inertiaTensor = body.inertiaTensor;

            Vector3 it = Vector3.zero;

            it.x = it.y = it.z = avgInertiaTensor = (inertiaTensor.x + inertiaTensor.y + inertiaTensor.z) / 3;

            body.inertiaTensor = it;
        }
    }

    public void OnReleaseObject()
    {
        Rigidbody body = GetComponent<Rigidbody>();

        collision = false;
        collisionEnter = false;
        collisionEnterChecked = false;
        body.maxAngularVelocity = maxAngularVelocity;
        body.inertiaTensor = inertiaTensor;
    }

    public void CancelCollision()
    {
        collision = false;
        collisionEnter = false;
        collisionEnterChecked = false;
    }

} // end of class HoldState.

// end of file.
