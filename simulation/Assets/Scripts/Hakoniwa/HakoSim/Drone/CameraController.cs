using System;
using hakoniwa.drone.sim;
using hakoniwa.objects.core.sensors;
using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.unity;
using hakoniwa.sim;
using UnityEngine;
using UnityEngine.UI;

namespace hakoniwa.drone.sim
{
    public class CameraController : MonoBehaviour
    {
        private string pdu_name_cmd_camera = "hako_cmd_camera";
        private string pdu_name_camera_data = "hako_camera_data";
        private string pdu_name_cmd_camera_move = "hako_cmd_camera_move";
        private string pdu_name_camera_info = "hako_cmd_camera_info";
        private ICameraController controller;


        public float move_step = 1.0f;  // 一回の動きのステップ量
        private float camera_move_button_time_duration = 0f;
        public float camera_move_button_threshold_speedup = 1.0f;

        private string robotName;

        private void Awake()
        {
            this.controller = this.GetComponentInChildren<ICameraController>();
            if (this.controller == null)
            {
                throw new Exception("Can not find ICameraController");
            }
            controller.Initialize();
        }

        void LateUpdate()
        {
            this.controller.UpdateCameraAngle();
        }

        public void DoInitialize(string robot_name, IHakoPdu hakoPdu)
        {
            this.robotName = robot_name;
            if (controller == null)
            {
                throw new Exception("Can not find ICameraController");
            }
            this.controller.DelclarePdu(robotName, null);
            var ret = hakoPdu.DeclarePduForRead(robotName, pdu_name_cmd_camera);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for read: {robotName} {pdu_name_cmd_camera}");
            }
            ret = hakoPdu.DeclarePduForRead(robotName, pdu_name_cmd_camera_move);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for read: {robotName} {pdu_name_cmd_camera_move}");
            }
            ret = hakoPdu.DeclarePduForWrite(robotName, pdu_name_camera_data);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for write: {robotName} {pdu_name_camera_data}");
            }
            ret = hakoPdu.DeclarePduForWrite(robotName, pdu_name_camera_info);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for write: {robotName} {pdu_name_camera_info}");
            }
        }

        public void DoControl(IPduManager pduManager)
        {
            /*
             * Camera Image Request
             */
            this.controller.CameraImageRequest(pduManager);
            /*
             * Camera Move Request
             */
            this.controller.CameraMoveRequest(pduManager);

            var game_controller = GameController.Instance;
            if (game_controller && game_controller.GetRadioControlOn())
            {
                /*
                 * Camera Image Rc request
                 */
                if (game_controller.GetCameraShotOn())
                {
                    Debug.Log("SHOT!!");
                    this.controller.Scan();
                    this.controller.WriteCameraDataPdu(pduManager);
                }

                if (game_controller.GetCameraMoveUp())
                {
                    camera_move_button_time_duration += Time.fixedDeltaTime;
                    if (camera_move_button_time_duration > camera_move_button_threshold_speedup)
                    {
                        this.controller.RotateCamera(-move_step * 3f);
                    }
                    else
                    {
                        this.controller.RotateCamera(-move_step);
                    }

                }

                if (game_controller.GetCameraMoveDown())
                {
                    camera_move_button_time_duration += Time.fixedDeltaTime;
                    if (camera_move_button_time_duration > camera_move_button_threshold_speedup)
                    {
                        this.controller.RotateCamera(move_step * 3f);
                    }
                    else
                    {
                        this.controller.RotateCamera(move_step);
                    }
                }
                if (!game_controller.GetCameraMoveDown() && !game_controller.GetCameraMoveUp())
                {
                    camera_move_button_time_duration = 0f;
                }
            }

            this.controller.UpdateCameraImageTexture();
        }
       
    }
}
