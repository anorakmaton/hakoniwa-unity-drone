using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.msgs.sensor_msgs;
using hakoniwa.pdu.unity;
using hakoniwa.sim;
using System;
using UnityEngine;

namespace hakoniwa.objects.core.sensors
{
    public struct ZRangerParams
    {
        public bool Enabled;
        public float MaxDistance;
        public bool DrawDebugRays;
        public int UpdateCycle;
    }

    public interface IZRangerController
    {
        public bool SetParams(ZRangerParams param);
        public ZRangerParams GetParams();
        public void DoInitialize(string robot_name, IPduManager pduManager);
        public void DoControl(IPduManager pduManager);
    }

    public class DefaultZRangerController : MonoBehaviour, IZRangerController
    {
        public bool Enabled = true;
        public float MaxDistance = 5.0f;
        public bool DrawDebugRays = true;
        public int UpdateCycle = 1;

        private float downDistance = 0.0f;

        public bool SetParams(ZRangerParams param)
        {
            this.Enabled = param.Enabled;
            this.MaxDistance = param.MaxDistance;
            this.DrawDebugRays = param.DrawDebugRays;
            this.UpdateCycle = param.UpdateCycle;
            return true;
        }

        public ZRangerParams GetParams()
        {
            ZRangerParams param = new ZRangerParams
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

        public const string pdu_name_zranger = "zranger";

        public void DoInitialize(string robot_name, IPduManager pduManager)
        {
            this.robotName = robot_name;
            this.sensor = this.gameObject;

            INamedPdu pdu = pduManager.CreateNamedPdu(robotName, pdu_name_zranger);
            if (pdu == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_zranger})");
            }

            // Initialize ZRanger message
            var zranger_msg = new hakoniwa.pdu.msgs.sensor_msgs.ZRanger(pdu);
            zranger_msg.z_range = 0.0f;
            zranger_msg.min_range = 0.0f;
            zranger_msg.max_range = this.MaxDistance;
            zranger_msg.field_of_view = 0.0f;
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

            // Measure distance in downward direction
            MeasureDownwardDistance();

            // Update PDU with measured distance
            INamedPdu pdu = pduManager.CreateNamedPdu(robotName, pdu_name_zranger);
            if (pdu == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_zranger})");
            }

            var zranger_msg = new hakoniwa.pdu.msgs.sensor_msgs.ZRanger(pdu);
            UpdateZRangerPdu(zranger_msg);
            pduManager.WriteNamedPdu(pdu);
            pduManager.FlushNamedPdu(pdu);
        }

        private void MeasureDownwardDistance()
        {
            // Get downward direction in world space
            UnityEngine.Vector3 downDirection = -sensor.transform.up; // 下方向

            // Measure distance in downward direction
            downDistance = GetDistanceInDirection(downDirection);
            //Debug.Log($"[ZRanger] Down: {downDistance:F2}");
        }

        private float GetDistanceInDirection(UnityEngine.Vector3 direction)
        {
            RaycastHit hit;
            
            if (Physics.Raycast(sensor.transform.position, direction, out hit, MaxDistance))
            {
                if (DrawDebugRays)
                {
                    Debug.DrawRay(sensor.transform.position, direction * hit.distance, Color.cyan, 0.1f, false);
                }
                return hit.distance;
            }
            else
            {
                if (DrawDebugRays)
                {
                    Debug.DrawRay(sensor.transform.position, direction * MaxDistance, Color.cyan, 0.1f, false);
                }
                return MaxDistance;
            }
        }

        private void UpdateZRangerPdu(hakoniwa.pdu.msgs.sensor_msgs.ZRanger zranger_msg)
        {
            zranger_msg.z_range = downDistance;
            zranger_msg.min_range = 0.0f;
            zranger_msg.max_range = this.MaxDistance;
            zranger_msg.field_of_view = 60.0f; // 仮の視野角値
        }

        // Public method to get downward distance measurement
        public float GetDownDistance()
        {
            return downDistance;
        }

        // Optional: Method to get distance with alias names
        public float GetDistanceByDirection(string direction)
        {
            switch (direction.ToLower())
            {
                case "down":
                case "downward":
                case "z":
                case "below":
                    return GetDownDistance();
                default:
                    Debug.LogWarning($"Unknown direction for ZRanger: {direction}");
                    return MaxDistance;
            }
        }
    }
}