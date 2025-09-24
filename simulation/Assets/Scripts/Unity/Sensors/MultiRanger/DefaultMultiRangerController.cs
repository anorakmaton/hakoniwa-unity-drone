using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.msgs.geometry_msgs;
using hakoniwa.pdu.unity;
using hakoniwa.sim;
using System;
using UnityEngine;

namespace hakoniwa.objects.core.sensors
{
    public struct MultiRangerParams
    {
        public bool Enabled;
        public float MaxDistance;
        public bool DrawDebugRays;
        public int UpdateCycle;
    }

    public interface IMultiRangerController
    {
        public bool SetParams(MultiRangerParams param);
        public MultiRangerParams GetParams();
        public void DoInitialize(string robot_name, IPduManager pduManager);
        public void DoControl(IPduManager pduManager);
    }

    public class DefaultMultiRangerController : MonoBehaviour, IMultiRangerController
    {
        public bool Enabled = true;
        public float MaxDistance = 5.0f;
        public bool DrawDebugRays = true;
        public int UpdateCycle = 1;

        // MultiRanger sensor directions (前後左右)
        private enum RangerDirection
        {
            Front = 0,  // 前
            Back = 1,   // 後
            Left = 2,   // 左
            Right = 3   // 右
        }

        private const int NUM_DIRECTIONS = 4;
        private float[] distances = new float[NUM_DIRECTIONS];

        public bool SetParams(MultiRangerParams param)
        {
            this.Enabled = param.Enabled;
            this.MaxDistance = param.MaxDistance;
            this.DrawDebugRays = param.DrawDebugRays;
            this.UpdateCycle = param.UpdateCycle;
            return true;
        }

        public MultiRangerParams GetParams()
        {
            MultiRangerParams param = new MultiRangerParams
            {
                Enabled = this.Enabled,
                MaxDistance = this.MaxDistance,
                DrawDebugRays = this.DrawDebugRays,
                UpdateCycle = this.UpdateCycle
            };
            return param;
        }

        private GameObject sensor;
        private string robotName;

        public const string pdu_name_multiranger = "multiranger";

        public void DoInitialize(string robot_name, IPduManager pduManager)
        {
            this.robotName = robot_name;
            this.sensor = this.gameObject;

            INamedPdu pdu = pduManager.CreateNamedPdu(robotName, pdu_name_multiranger);
            if (pdu == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_multiranger})");
            }

            // Initialize Twist message for MultiRanger data
            // linear.x = front, linear.y = back, linear.z = left, angular.x = right
            var multiranger_msg = new hakoniwa.pdu.msgs.geometry_msgs.Twist(pdu);
            multiranger_msg.linear.x = 0.0;
            multiranger_msg.linear.y = 0.0;
            multiranger_msg.linear.z = 0.0;
            multiranger_msg.angular.x = 0.0;
            multiranger_msg.angular.y = 0.0;
            multiranger_msg.angular.z = 0.0;

            pduManager.WriteNamedPdu(pdu);
            pduManager.FlushNamedPdu(pdu);
        }

        private int count = 0;

        public void DoControl(IPduManager pduManager)
        {
            if (this.Enabled == false)
            {
                return;
            }

            this.count++;
            if (this.count < this.UpdateCycle)
            {
                return;
            }
            this.count = 0;

            // Measure distances in all four directions
            Debug.Log($"DrawDebugRays: {DrawDebugRays}");
            MeasureDistances();

            // Update PDU with measured distances
            INamedPdu pdu = pduManager.CreateNamedPdu(robotName, pdu_name_multiranger);
            if (pdu == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_multiranger})");
            }

            var multiranger_msg = new hakoniwa.pdu.msgs.geometry_msgs.Twist(pdu);
            UpdateMultiRangerPdu(multiranger_msg);
            pduManager.WriteNamedPdu(pdu);
            pduManager.FlushNamedPdu(pdu);
        }

        private void MeasureDistances()
        {
            // Get sensor directions in world space
            UnityEngine.Vector3[] directions = new UnityEngine.Vector3[NUM_DIRECTIONS];
            directions[(int)RangerDirection.Front] = sensor.transform.forward;   // 前
            directions[(int)RangerDirection.Back] = -sensor.transform.forward;  // 後
            directions[(int)RangerDirection.Left] = -sensor.transform.right;    // 左
            directions[(int)RangerDirection.Right] = sensor.transform.right;    // 右

            // Measure distance for each direction
            for (int i = 0; i < NUM_DIRECTIONS; i++)
            {
                distances[i] = GetDistanceInDirection(directions[i], (RangerDirection)i);
            }
        }

        private float GetDistanceInDirection(UnityEngine.Vector3 direction, RangerDirection rangerDir)
        {
            RaycastHit hit;
            
            if (Physics.Raycast(sensor.transform.position, direction, out hit, MaxDistance))
            {
                if (DrawDebugRays)
                {
                    Color rayColor = GetRayColor(rangerDir);
                    Debug.DrawRay(sensor.transform.position, direction * hit.distance, rayColor, 0.1f, false);
                }
                return hit.distance;
            }
            else
            {
                if (DrawDebugRays)
                {
                    Color rayColor = GetRayColor(rangerDir);
                    Debug.DrawRay(sensor.transform.position, direction * MaxDistance, rayColor, 0.1f, false);
                }
                return MaxDistance;
            }
        }

        private Color GetRayColor(RangerDirection direction)
        {
            switch (direction)
            {
                case RangerDirection.Front:
                    return Color.red;
                case RangerDirection.Back:
                    return Color.blue;
                case RangerDirection.Left:
                    return Color.green;
                case RangerDirection.Right:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }

        private void UpdateMultiRangerPdu(hakoniwa.pdu.msgs.geometry_msgs.Twist multiranger_msg)
        {
            // Use Twist message to send all four distance measurements
            // linear.x = front distance
            // linear.y = back distance  
            // linear.z = left distance
            // angular.x = right distance
            // angular.y = max distance (for reference)
            // angular.z = timestamp or other metadata
            
            multiranger_msg.linear.x = (double)distances[(int)RangerDirection.Front];
            multiranger_msg.linear.y = (double)distances[(int)RangerDirection.Back];
            multiranger_msg.linear.z = (double)distances[(int)RangerDirection.Left];
            multiranger_msg.angular.x = (double)distances[(int)RangerDirection.Right];
            multiranger_msg.angular.y = (double)this.MaxDistance;
            multiranger_msg.angular.z = (double)Time.time; // timestamp
        }

        // Public methods to get individual distance measurements
        public float GetFrontDistance()
        {
            return distances[(int)RangerDirection.Front];
        }

        public float GetBackDistance()
        {
            return distances[(int)RangerDirection.Back];
        }

        public float GetLeftDistance()
        {
            return distances[(int)RangerDirection.Left];
        }

        public float GetRightDistance()
        {
            return distances[(int)RangerDirection.Right];
        }

        public float[] GetAllDistances()
        {
            return (float[])distances.Clone();
        }

        // Optional: Method to get distance in a specific direction by name
        public float GetDistanceByDirection(string direction)
        {
            switch (direction.ToLower())
            {
                case "front":
                case "forward":
                    return GetFrontDistance();
                case "back":
                case "backward":
                    return GetBackDistance();
                case "left":
                    return GetLeftDistance();
                case "right":
                    return GetRightDistance();
                default:
                    Debug.LogWarning($"Unknown direction: {direction}");
                    return MaxDistance;
            }
        }
    }
}
