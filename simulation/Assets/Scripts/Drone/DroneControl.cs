using hakoniwa.drone.sim;
using hakoniwa.objects.core;
using hakoniwa.objects.core.sensors;
using hakoniwa.pdu.interfaces;
using UnityEngine;

namespace hakoniwa.drone
{
    public enum DroneControlInputType
    {
        PS4,
        Xbox,
        Xr,
        SPIDAR,
        SPIDARV2
    }
    public interface IDroneControlOp
    {
        public void DoInitialize(string robotName);
        public int PutUpButton(int index, int value);
        public int PutDownButton(int index, int value);
        public int PutRadioControlButton(int index, int value);
        public int PutFlightModeChangeButton(int index, int value);
        public int PutHorizontal(int index, double value);
        public int PutForward(int index, double value);
        public int PutHeading(int index, double value);
        public int PutVertical(int index, double value);
        public bool GetMagnetRequest(out bool magnet_on);
        public void PutMagnetStatus(bool magnet_on, bool contact_on);
        public void DoFlush();
    }

    public class DroneControl : MonoBehaviour
    {
        public string robotName = "Drone";
        public DroneControlInputType input_type;
        public double stick_strength = 0.1;
        public double stick_yaw_strength = 1.0;
        private IDroneInput controller_input;
        public bool magnet_on = false;
        public GameObject grabberObject;
        private IBaggageGrabber grabber;
        public bool forceGrab = false;
        private IDroneControlOp droneControlOp = null;
        public bool isPlayer = true;
        [Header("Wind Simulation Settings")]
        public bool enableWind = false;
        public float windStrength = 0.3f;   // 風の強さ（最大入力に対する割合）
        public float windFrequency = 0.5f;  // 風向きが変わる速さ
        public Vector2 currentWind = Vector2.zero; // 現在の風ベクトル

        [Header("Control Restrictions")]
        public bool restrictHorizontalControl = false; // Takeoffフェーズ用: trueの場合、上昇下降(Throttle)以外の操作を無効化する

        public IDroneInput GetDroneInput()
        {
            return controller_input;
        }

        public bool IsMagnetOn()
        {
            return magnet_on;
        }

        // 入力タイプを設定し、controller_inputを初期化する
        public void SetInputType(DroneControlInputType type)
        {
            input_type = type;
            InitializeControllerInput();
        }

        private void Awake()
        {
            droneControlOp = this.GetComponentInChildren<IDroneControlOp>();
            droneControlOp.DoInitialize(robotName);
        }

        private void Start()
        {
            if (grabberObject != null)
            {
                grabber = grabberObject.GetComponentInChildren<IBaggageGrabber>();
                Debug.Log("grabber: " + grabber);
            }
            else
            {
                Debug.Log("Gabber is not found.");
            }

            InitializeControllerInput();
        }

        private void InitializeControllerInput()
        {
            if (input_type == DroneControlInputType.PS4)
            {
                controller_input = HakoDroneInputManager.Instance;
            }
            else if (input_type == DroneControlInputType.Xbox)
            {
                //TODO
            }
            else if (input_type == DroneControlInputType.SPIDAR)
            {
                controller_input = HakoDroneSpidarInputManager.Instance;
            }
            else if (input_type == DroneControlInputType.SPIDARV2)
            {
                controller_input = HakoDroneSpidarInputManagerV2.Instance;
            }
            else
            {
                controller_input = HakoDroneXrInputManager.Instance;
            }
        }

        public float move_step = 1.0f;  // ̓̃Xebv
        private float camera_move_button_time_duration = 0f;
        public float camera_move_button_threshold_speedup = 1.0f;
        public bool is_pressed_up = false;
        public bool is_pressed_down = false;

        public void HandleCameraControl(ICameraController camera_controller, IPduManager pduManager)
        {
            {
                /*
                 * Camera Image Rc request
                 */
                if (controller_input.IsYButtonPressed() && pduManager != null)
                {
                    Debug.Log("SHOT!!");
                    camera_controller.Scan();
                    camera_controller.WriteCameraDataPdu(pduManager);
                }
                if (controller_input.IsUpButtonPressed())
                {
                    is_pressed_up = true;
                }
                else if (controller_input.IsUpButtonReleased())
                {
                    is_pressed_up = false;
                }
                if (is_pressed_up)
                {
                    camera_move_button_time_duration += Time.fixedDeltaTime;
                    if (camera_move_button_time_duration > camera_move_button_threshold_speedup)
                    {
                        camera_controller.RotateCamera(-move_step * 3f);
                    }
                    else
                    {
                        camera_controller.RotateCamera(-move_step);
                    }

                }
                if (controller_input.IsDownButtonPressed())
                {
                    is_pressed_down = true;
                }
                else if (controller_input.IsDownButtonReleased())
                {
                    is_pressed_down = false;
                }


                if (is_pressed_down)
                {
                    camera_move_button_time_duration += Time.fixedDeltaTime;
                    if (camera_move_button_time_duration > camera_move_button_threshold_speedup)
                    {
                        camera_controller.RotateCamera(move_step * 3f);
                    }
                    else
                    {
                        camera_controller.RotateCamera(move_step);
                    }
                }

                if (!is_pressed_down && !is_pressed_up)
                {
                    camera_move_button_time_duration = 0f;
                }
            }
        }
        public void HandleInput()
        {
            UpdateWind();
            Vector2 leftStick = controller_input.GetLeftStickInput();
            Vector2 rightStick = controller_input.GetRightStickInput();

            // [変更] 水平操作制限が有効な場合、入力値をマスクする
            if (restrictHorizontalControl)
            {
                // 左スティックのX（Yaw/旋回）を無効化、Y（Throttle/上昇下降）は維持
                leftStick.x = 0f;
                // 右スティック（Pitch/Roll/水平移動）を完全に無効化
                rightStick = Vector2.zero;
            }

            if (enableWind)
            {
                // 風の影響を右スティック入力に加算
                rightStick.x += currentWind.x;
                rightStick.y += currentWind.y;
                // 入力範囲を-1.0から1.0にクランプ
                rightStick.x = Mathf.Clamp(rightStick.x, -1.0f, 1.0f);
                rightStick.y = Mathf.Clamp(rightStick.y, -1.0f, 1.0f);
            }
            float horizontal = rightStick.x;
            float forward = rightStick.y;
            float yaw = leftStick.x;
            float pitch = leftStick.y;
            bool mag_on = false;
            bool mag_req = droneControlOp.GetMagnetRequest(out mag_on);
            if (controller_input.IsAButtonPressed())
            {
                droneControlOp.PutRadioControlButton(0, 1);
            }
            else if (controller_input.IsAButtonReleased())
            {
                droneControlOp.PutRadioControlButton(0, 0);
            }
            if (controller_input.IsXButtonPressed())
            {
                droneControlOp.PutFlightModeChangeButton(0, 1);
            }
            else if (controller_input.IsXButtonReleased())
            {
                droneControlOp.PutFlightModeChangeButton(0, 0);
            }
            if (controller_input.IsBButtonReleased())
            {
                magnet_on = IsMagnetOn() ? false : true;
                if (grabber != null)
                {
                    if (magnet_on)
                    {
                        grabber.Grab(forceGrab);
                    }
                    else
                    {
                        grabber.Release();
                    }
                }

            }
            else if (mag_req)
            {
                if (mag_on)
                {
                    Debug.Log("Request: mag_on");
                    grabber.Grab(forceGrab);
                }
                else
                {
                    Debug.Log("Request: mag_off");
                    grabber.Release();
                }
                magnet_on = mag_on;
            }
            else if (controller_input.IsUpButtonPressed())
            {
                droneControlOp.PutUpButton(0, 1);
            }
            else if (controller_input.IsUpButtonReleased())
            {
                droneControlOp.PutUpButton(0, 0);
            }
            else if (controller_input.IsDownButtonPressed())
            {
                droneControlOp.PutDownButton(0, 1);
            }
            else if (controller_input.IsDownButtonReleased())
            {
                droneControlOp.PutDownButton(0, 0);
            }
            if (grabber != null)
            {
                droneControlOp.PutMagnetStatus(magnet_on, grabber.IsGrabbed());
            }
            droneControlOp.PutHorizontal(0, horizontal * stick_strength);
            droneControlOp.PutForward(0, -forward * stick_strength);
            droneControlOp.PutHeading(0, yaw * stick_yaw_strength);
            droneControlOp.PutVertical(0, -pitch * stick_strength);

            droneControlOp.DoFlush();
        }

        void UpdateWind()
        {
            if (!enableWind)
            {
                currentWind = Vector2.zero;
                return;
            }

            // 時間経過で滑らかに変化するノイズを生成 (0.0~1.0 -> -1.0~1.0 に変換)
            float noiseX = Mathf.PerlinNoise(Time.time * windFrequency, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0f, Time.time * windFrequency) * 2f - 1f;

            currentWind = new Vector2(noiseX, noiseY) * windStrength;
            //Debug.Log($"🌬️ Wind Updated: {currentWind}");
        }

    }
}