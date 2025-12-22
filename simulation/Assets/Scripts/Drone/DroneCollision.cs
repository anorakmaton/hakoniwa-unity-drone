using UnityEngine;
using hakoniwa.drone.service;
using hakoniwa.objects.core;
using System; // Action用に必要

namespace hakoniwa.drone
{
    public class DroneImpulseCollision
    {
        public bool collision;
        public bool isTargetStatic;
        public Vector3 targetVelocity;
        public Vector3 targetAngularVelocity;
        public Vector3 targetEuler;
        public Vector3 selfContactVector;
        public Vector3 targetContactVector;
        public Vector3 targetInertia;
        public Vector3 normal;
        public double targetMass;
        public double restitutionCoefficient;

        public DroneImpulseCollision(DroneImpulseCollision c)
        {
            collision = c.collision;
            isTargetStatic = c.isTargetStatic;
            targetVelocity = c.targetVelocity;
            targetAngularVelocity = c.targetAngularVelocity;
            targetEuler = c.targetEuler;
            selfContactVector = c.selfContactVector;
            targetContactVector = c.targetContactVector;
            targetInertia = c.targetInertia;
            normal = c.normal;
            targetMass = c.targetMass;
            restitutionCoefficient = c.restitutionCoefficient;
        }
        public DroneImpulseCollision() { }
    }


    [RequireComponent(typeof(BoxCollider))]
    public class DroneCollision : MonoBehaviour
    {
        [SerializeField]
        private LayerMask collisionLayer; // 衝突を検出するレイヤー
        [SerializeField]
        private bool isHakoniwa = false;
        public DroneControl vibration;
        private IDroneInput vibrationObject;

        // ★追加: 衝突を外部（実験マネージャー等）に通知するイベント
        public Action<Collider> OnCollisionEnterAction;

        private DroneImpulseCollision impluse_collision = new DroneImpulseCollision();
        public DroneImpulseCollision GetImpulseCollision()
        {
            DroneImpulseCollision ret = new DroneImpulseCollision(impluse_collision);
            impluse_collision.collision = false;
            return ret;
        }

        public GameObject pos_obj;

        private int index;
        public void SetIndex(int inx)
        {
            this.index = inx;
        }

        private void Awake()
        {
            // BoxCollider をトリガーとして設定
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            if (pos_obj == null)
            {
                pos_obj = this.gameObject;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (vibration != null)
            {
                if (vibrationObject == null)
                {
                    vibrationObject = vibration.GetDroneInput();
                    if (vibrationObject != null)
                    {
                        Debug.Log("Vibration is enabled");
                    }
                    else
                    {
                        Debug.Log("Vibration is disabled");
                    }
                }
            }

            // レイヤーマスクに基づいて対象をフィルタリング
            if (IsLayerInMask(other.gameObject.layer, collisionLayer))
            {
                // ★追加: イベント発火（購読者がいれば通知）
                OnCollisionEnterAction?.Invoke(other);

                TargetColliderInfo info = TargetColliderInfo.GetInfo(other);
                if (info != null)
                {
                    Debug.Log("Info: " + this.pos_obj.name + " collided with " + info.GetName());
                    HandleTriggerImpulseCollision(info, other);
                }
                if (vibrationObject != null)
                {
                    vibrationObject.DoVibration(
                        isRightHand: true,
                        frequency: 0.9f,   // 低めで重い感触
                        amplitude: 1.0f,   // 強め
                        durationSec: 0.2f  // 短く強調
                     );
                    vibrationObject.DoVibration(
                        isRightHand: false,
                        frequency: 0.9f,   // 低めで重い感触
                        amplitude: 1.0f,   // 強め
                        durationSec: 0.2f  // 短く強調
                     );
                }
            }
        }
        private Vector3 ConvertToRosVector(Vector3 unityVector)
        {
            return new Vector3(
                unityVector.z,
                -unityVector.x,
                unityVector.y
            );
        }

        private Vector3 ConvertToRosAngular(Vector3 unityAngular)
        {
            return new Vector3(
                -unityAngular.z,
                unityAngular.x,
                -unityAngular.y
            );
        }


        private void HandleTriggerImpulseCollision(TargetColliderInfo info, Collider other)
        {
            // Calculate Contact point in world frame
            Vector3 contactPoint = other.ClosestPoint(this.pos_obj.transform.position);
            Debug.Log($"Contact Point (World Frame): {contactPoint}");

            // Calculate collision relative vector
            Vector3 selfContactVector = contactPoint - this.pos_obj.transform.position;
            Debug.Log($"Center of Drone: {this.pos_obj.transform.position}");
            Debug.Log($"Collision Relative Vector: {selfContactVector}");
            float epsilon = 0.0001f;

            if (Mathf.Abs(selfContactVector.x) < epsilon &&
                Mathf.Abs(selfContactVector.y) < epsilon &&
                Mathf.Abs(selfContactVector.z) < epsilon)
            {
                Debug.Log("Invalid conditions");
                return;
            }
            // Calculate TargetVelocity
            Vector3 targetVelocity = info.Velocity;

            // Calculate TargetAngularVelocity
            Vector3 targetAngularVelocity = info.AngularVelocity;

            Vector3 targetContactVector = contactPoint - info.Position;
            // Calculate normal
            //Vector3 normal = info.GetNormal(contactPoint, this.pos_obj.transform.position);
            //Vector3 normal = info.GetNormalSphere(this.pos_obj.transform.position);
            //Vector3 normal = (-selfContactVector).normalized;
            Vector3 normal = (targetVelocity - selfContactVector).normalized;


            Vector3 targetEuler = Vector3.zero;

            if (isHakoniwa)
            {
                impluse_collision.collision = true;
                impluse_collision.isTargetStatic = info.IsStatic;
                impluse_collision.targetVelocity = ConvertToRosVector(targetVelocity);
                impluse_collision.targetAngularVelocity = ConvertToRosAngular(targetAngularVelocity);
                impluse_collision.targetEuler = ConvertToRosAngular(targetEuler);
                impluse_collision.selfContactVector = ConvertToRosVector(selfContactVector);
                impluse_collision.targetContactVector = ConvertToRosVector(targetContactVector);
                impluse_collision.targetInertia = info.Inertia;
                impluse_collision.normal = ConvertToRosVector(normal);
                impluse_collision.targetMass = info.Mass;
                impluse_collision.restitutionCoefficient = info.RestitutionCoefficient;
            }
            else
            {
                DroneServiceRC.PutImpulseByCollision(
                    this.index,
                    info.IsStatic,
                    ConvertToRosVector(targetVelocity),
                    ConvertToRosAngular(targetAngularVelocity),
                    ConvertToRosAngular(targetEuler),
                    ConvertToRosVector(selfContactVector),
                    ConvertToRosVector(targetContactVector),
                    info.Inertia,
                    ConvertToRosVector(normal),
                    info.Mass,
                    info.RestitutionCoefficient
                );
            }
            Debug.Log($"Impulse collision handled with {other.name}\n" +
              $"Index: {this.index}, IsStatic: {info.IsStatic}\n" +
              $"TargetVelocity: {ConvertToRosVector(targetVelocity)}\n" +
              $"TargetAngularVelocity: {ConvertToRosAngular(targetAngularVelocity)}\n" +
              $"TargetEuler: {ConvertToRosAngular(targetEuler)}\n" +
              $"SelfContactVector: {ConvertToRosVector(selfContactVector)}\n" +
              $"TargetContactVector: {ConvertToRosVector(targetContactVector)}\n" +
              $"Inertia: {info.Inertia}\n" +
              $"Normal: {ConvertToRosVector(normal)}\n" +
              $"Mass: {info.Mass}, RestitutionCoefficient: {info.RestitutionCoefficient}");

            //Debug.Log($"Impulse collision handled with {other.name}");
        }

        private bool IsLayerInMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) > 0;
        }
    }
}