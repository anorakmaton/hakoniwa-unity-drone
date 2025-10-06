using UnityEngine;
using System.Collections;

public class PointerControl : MonoBehaviour
{
    public bool showInformation = true;

    private const int Margin = 25;

    private HapticPointerBase[] _pointers;
    private PointerSetting _pointerSetting = null;
    private GUIStyle _style = null;
    private GUIStyle _style2 = null;
    private int _currentPointer = 0;
    private string _currentPointerName;

    void Start()
    {
        _pointers = GameObject.FindObjectsOfType(typeof(HapticPointerBase)) as HapticPointerBase[];

        _pointerSetting = GameObject.FindObjectOfType(typeof(PointerSetting)) as PointerSetting;

        GUIStyleState state = new GUIStyleState();
        state.textColor = Color.black;
        _style = new GUIStyle();
        _style.normal = state;
        _style.fontSize = 16;

        state = new GUIStyleState();
        state.textColor = Color.red;
        _style2 = new GUIStyle();
        _style2.normal = state;
        _style2.fontSize = 16;

        showInformation =
            PlayerPrefs.GetInt("PointerControl.ShowInformation", showInformation ? 1 : 0) == 1;
        _currentPointerName =
            PlayerPrefs.GetString("PointerControl.CurrentPointer", "");

        _currentPointer = 0;
        for (int i = 0; i < _pointers.Length; ++i)
            if (_pointers[i].name == _currentPointerName)
            {
                _currentPointer = i;
                break;
            }
    }

    void OnDisable()
    {
        PlayerPrefs.SetInt("PointerControl.ShowInformation", showInformation ? 1 : 0);
        PlayerPrefs.SetString("PointerControl.CurrentPointer", _currentPointerName);
    }

    void OnGUI()
    {
        if (!showInformation || 
            _pointers.Length == 0 ||
            (_pointerSetting != null && _pointerSetting.isOpened))
            return;

        int left = PointerSetting.SettingButtonLeft;
        int top = Margin + (_pointerSetting == null ?
                PointerSetting.SettingButtonTop :
                PointerSetting.SettingButtonTop + PointerSetting.SettingButtonHeight);
        int width = Screen.width - left;
        int height = Screen.height - top;

        GUILayout.BeginArea(new Rect(left, top, width, height));
        GUILayout.Label("Key configuration: ", _style);
        GUILayout.Label("   [ i ]   -  show/hide this information", _style);
        GUILayout.Label("   [   ]   -  hold/release object", _style);
        GUILayout.Label("   [v ]   -  clutch on/off", _style);
        GUILayout.Label("   [c ]   -  execute calibration", _style);
        GUILayout.Label("   [m]   -  select haptic pointer", _style);
        GUILayout.Space(20);

        HapticPointerBase hpb = _pointers[_currentPointer];
        GUILayout.Label("Selected Haptic Pointer:  " + hpb.name, _style);
        if (hpb.Activated)
        {
            GUILayout.Label("   Device serial number: " + hpb.SerialNumber, _style);
            GUILayout.Label("   Device Delta Time: " + (hpb.DeviceDeltaTime * 1000).ToString("f3") + "[ms]", _style);
            GUILayout.Label("   Haptics: " + hpb.Haptics + ", Gravity: " + hpb.Gravity + ", Cascade Control: " + hpb.CascadeControl, _style);
            if (hpb.DebugMode)
            {
                GUILayout.Label("   GPIO: " + hpb.GpioValue, _style);

                int[] encoderCount = new int[8];
                hpb.GetEncoderCount(ref encoderCount);
                GUILayout.Label("   Encoder counts", _style);

                for (int i = 0; i < 8; ++i)
                    GUILayout.Label("      Motor " + (i + 1) + ": " + encoderCount[i], _style);

                HapticPointer hp = hpb as HapticPointer;

                if (hp != null && hp.HoldingObject)
                {
                    GUILayout.Space(10);
                    HoldState hs = hp.HoldingObject.GetComponent<HoldState>();
                    GUILayout.Label(hp.HoldingObject.ToString(), _style);
                    GUILayout.Label(hs.ToString(), _style);
                    GUILayout.Label("   Collision state: " + hs.Collision, _style);
                    GUILayout.Label("   Mass: " + hp.HoldingObject.mass, _style);
                    GUILayout.Label("   Center of mass: " + hp.HoldingObject.centerOfMass.ToString("f4"), _style);
                    GUILayout.Label("   InertiaTensor: " + hp.HoldingObject.inertiaTensor.ToString("f4"), _style);
                    GUILayout.Label("   InertiaTensorRotation: " + hp.HoldingObject.inertiaTensorRotation.ToString("f4"), _style);
                }
            }
        }
        else
        {
            GUILayout.Label("  ... not activated", _style2);
        }
        GUILayout.Space(20);

        GUILayout.Label("Unity delta time: " + (Time.deltaTime * 1000).ToString("f3") + "[ms], Fixed delta time: "
                        + (Time.fixedDeltaTime * 1000).ToString("f3") + "[ms]", _style);

        GUILayout.EndArea();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            showInformation = !showInformation;
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            _currentPointer++;
            if (_currentPointer >= _pointers.Length)
                _currentPointer = 0;
            _currentPointerName = _pointers.Length > 0 ? _pointers[_currentPointer].name : "";
        }

        if (_pointers.Length == 0)
            return;

        HapticPointer hp = _pointers[_currentPointer] as HapticPointer;
        if (hp == null)
            return;

        if (hp.ToggleHold)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!hp.ReleaseObject()) hp.HoldObject();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                hp.HoldObject();
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                hp.ReleaseObject();
            }
        }

        if (hp.ToggleClutch)
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (!hp.ReleaseClutch()) hp.EngageClutch();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                hp.EngageClutch();
            }

            if (Input.GetKeyUp(KeyCode.V))
            {
                hp.ReleaseClutch();
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            hp.Calibrate();
        }

    }
}
