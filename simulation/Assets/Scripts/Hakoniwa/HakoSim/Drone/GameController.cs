using System;
using hakoniwa.pdu.interfaces;
using hakoniwa.sim;
using UnityEngine;

namespace hakoniwa.drone.sim
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // シーンをまたいで保持
            }
            else if (!ReferenceEquals(Instance, this)) // Unity 特有の比較の警告を避ける
            {
                Destroy(gameObject);
            }
        }
        private bool is_radio_control = false;

        public string pdu_name_cmd_game = "hako_cmd_game";
        private string robotName;
        private bool[] button_array;

        public bool GetGrabBaggageOn()
        {
            return button_array[DroneControlPdu.game_ops_grab_baggage_button_index];
        }
        public bool GetCameraShotOn()
        {
            return button_array[DroneControlPdu.game_ops_camera_button_index];
        }
        public bool GetCameraMoveUp()
        {
            return button_array[DroneControlPdu.game_ops_camera_move_up_index];
        }
        public bool GetCameraMoveDown()
        {
            return button_array[DroneControlPdu.game_ops_camera_move_down_index];
        }
        public bool GetRadioControlOn()
        {
            return is_radio_control;
        }

        public void DoInitialize(string robot_name, IHakoPdu hakoPdu)
        {
            Debug.Log("GameController Initialize");
            this.robotName = robot_name;
            var ret = hakoPdu.DeclarePduForRead(robot_name, pdu_name_cmd_game);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for read: {robotName} {pdu_name_cmd_game}");
            }
            button_array = new bool[16];
        }
        public void DoControl(IPduManager pduManager)
        {
            IPdu pdu_cmd_game_ctrl = pduManager.ReadPdu(robotName, pdu_name_cmd_game);
            if (pdu_cmd_game_ctrl != null)
            {
                var cmd_game_ctrl = new hakoniwa.pdu.msgs.hako_msgs.GameControllerOperation(pdu_cmd_game_ctrl);
                for (int i = 0; i < cmd_game_ctrl.button.Length; i++)
                {
                    button_array[i] = cmd_game_ctrl.button[i];
                    //Debug.Log($"Button[{i}] = {button_array[i]}");
                }
                if (button_array[DroneControlPdu.game_ops_arm_button_index])
                {
                    is_radio_control = true;
                }
            }
        }
    }
}
