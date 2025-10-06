using UnityEngine;

class PointerParameter
{
    private HapticPointerBase _hapticPointerBase = null;

    private uint _serialNumber;
    private bool _haptics;
    private bool _debugMode;
    private bool _gravity;
    private float _unitySpringK;
    private float _unityDamperB;
    private float _positionScale;
    private float _rotationScale;
    private float _deviceSpringK;
    private float _deviceDamperB;
    private bool _cascadeControl;
    private float _cascadeGain;
    private bool _toggleHold;
    private bool _toggleClutch;
    private int _holdChannel;
    private int _clutchChannel;
    private int _calibrationChannel;
    private static float _fixedDeltaTime = 0.0f;

    public PointerParameter(HapticPointerBase hapticPointerBase)
    {
        _hapticPointerBase = hapticPointerBase;
    }

    public void record()
    {
        _serialNumber = _hapticPointerBase.SerialNumber;
        _haptics = _hapticPointerBase.Haptics;
        _debugMode = _hapticPointerBase.DebugMode;
        _gravity = _hapticPointerBase.Gravity;
        _unitySpringK = _hapticPointerBase.UnitySpringK;
        _unityDamperB = _hapticPointerBase.UnityDamperB;
        _positionScale = _hapticPointerBase.PositionScale;
        _rotationScale = _hapticPointerBase.RotationScale;
        _deviceSpringK = _hapticPointerBase.DeviceSpringK;
        _deviceDamperB = _hapticPointerBase.DeviceDamperB;
        _cascadeControl = _hapticPointerBase.CascadeControl;
        _cascadeGain = _hapticPointerBase.CascadeGain;
        _toggleHold = _hapticPointerBase.ToggleHold;
        _toggleClutch = _hapticPointerBase.ToggleClutch;
        _holdChannel = _hapticPointerBase.HoldChannel;
        _clutchChannel = _hapticPointerBase.ClutchChannel;
        _calibrationChannel = _hapticPointerBase.CalibrationChannel;

        if (_fixedDeltaTime == 0.0f)
            _fixedDeltaTime = HapticPointerBase.FixedDeltaTime;
    }

    public void recall(bool simulation)
    {
        if (simulation)
        {
            _hapticPointerBase.DebugMode = _debugMode;
            _hapticPointerBase.Gravity = _gravity;
            _hapticPointerBase.Haptics = _haptics;
            _hapticPointerBase.UnitySpringK = _unitySpringK;
            _hapticPointerBase.UnityDamperB = _unityDamperB;
            HapticPointer.FixedDeltaTime = _fixedDeltaTime;
        }
        else
        {
            _hapticPointerBase.SerialNumber = _serialNumber;
            _hapticPointerBase.PositionScale = _positionScale;
            _hapticPointerBase.RotationScale = _rotationScale;
            _hapticPointerBase.DeviceSpringK = _deviceSpringK;
            _hapticPointerBase.DeviceDamperB = _deviceDamperB;
            _hapticPointerBase.CascadeControl = _cascadeControl;
            _hapticPointerBase.CascadeGain = _cascadeGain;
            _hapticPointerBase.ToggleHold = _toggleHold;
            _hapticPointerBase.ToggleClutch = _toggleClutch;
            _hapticPointerBase.HoldChannel = _holdChannel;
            _hapticPointerBase.ClutchChannel = _clutchChannel;
            _hapticPointerBase.CalibrationChannel = _calibrationChannel;
        }
    }

    public void serialize()
    {
        PlayerPrefs.SetInt(getKey("SerialNumber"), (int)_hapticPointerBase.SerialNumber);
        PlayerPrefs.SetInt(getKey("Haptics"), _hapticPointerBase.Haptics ? 1 : 0);
        PlayerPrefs.SetInt(getKey("DebugMode"), _hapticPointerBase.DebugMode ? 1 : 0);
        PlayerPrefs.SetInt(getKey("Gravity"), _hapticPointerBase.Gravity ? 1 : 0);
        PlayerPrefs.SetFloat(getKey("UnitySpringK"), _hapticPointerBase.UnitySpringK);
        PlayerPrefs.SetFloat(getKey("UnityDamperB"), _hapticPointerBase.UnityDamperB);
        PlayerPrefs.SetFloat(getKey("PositionScale"), _hapticPointerBase.PositionScale);
        PlayerPrefs.SetFloat(getKey("RotationScale"), _hapticPointerBase.RotationScale);
        PlayerPrefs.SetFloat(getKey("DeviceSpringK"), _hapticPointerBase.DeviceSpringK);
        PlayerPrefs.SetFloat(getKey("DeviceDamperB"), _hapticPointerBase.DeviceDamperB);
        PlayerPrefs.SetInt(getKey("CascadeControl"), _hapticPointerBase.CascadeControl ? 1 : 0);
        PlayerPrefs.SetFloat(getKey("CascadeGain"), _hapticPointerBase.CascadeGain);
        PlayerPrefs.SetInt(getKey("ToggleHold"), _hapticPointerBase.ToggleHold ? 1 : 0);
        PlayerPrefs.SetInt(getKey("ToggleClutch"), _hapticPointerBase.ToggleClutch ? 1 : 0);
        PlayerPrefs.SetInt(getKey("HoldChannel"), _hapticPointerBase.HoldChannel);
        PlayerPrefs.SetInt(getKey("ClutchChannel"), _hapticPointerBase.ClutchChannel);
        PlayerPrefs.SetInt(getKey("CalibrationChannel"), _hapticPointerBase.CalibrationChannel);
        PlayerPrefs.SetFloat(getKey("FixedDeltaTime"), HapticPointer.FixedDeltaTime);
    }

    public void deserialize()
    {
        _hapticPointerBase.SerialNumber = (uint)
            PlayerPrefs.GetInt(getKey("SerialNumber"), (int)_hapticPointerBase.SerialNumber);
        _hapticPointerBase.Haptics = 
            PlayerPrefs.GetInt(getKey("Haptics"), _hapticPointerBase.Haptics ? 1 : 0) == 1;
        _hapticPointerBase.DebugMode = 
            PlayerPrefs.GetInt(getKey("DebugMode"), _hapticPointerBase.DebugMode ? 1 : 0) == 1;
        _hapticPointerBase.Gravity =
            PlayerPrefs.GetInt(getKey("Gravity"), _hapticPointerBase.Gravity ? 1 : 0) == 1;
        _hapticPointerBase.UnitySpringK = 
            PlayerPrefs.GetFloat(getKey("UnitySpringK"), _hapticPointerBase.UnitySpringK);
        _hapticPointerBase.UnityDamperB = 
            PlayerPrefs.GetFloat(getKey("UnityDamperB"), _hapticPointerBase.UnityDamperB);
        _hapticPointerBase.PositionScale = 
            PlayerPrefs.GetFloat(getKey("PositionScale"), _hapticPointerBase.PositionScale);
        _hapticPointerBase.RotationScale =
            PlayerPrefs.GetFloat(getKey("RotationScale"), _hapticPointerBase.RotationScale);
        _hapticPointerBase.DeviceSpringK = 
            PlayerPrefs.GetFloat(getKey("DeviceSpringK"), _hapticPointerBase.DeviceSpringK);
        _hapticPointerBase.DeviceDamperB = 
            PlayerPrefs.GetFloat(getKey("DeviceDamperB"), _hapticPointerBase.DeviceDamperB);
        _hapticPointerBase.CascadeControl = 
            PlayerPrefs.GetInt(getKey("CascadeControl"), _hapticPointerBase.CascadeControl ? 1 : 0) == 1;
        _hapticPointerBase.CascadeGain = 
            PlayerPrefs.GetFloat(getKey("CascadeGain"), _hapticPointerBase.CascadeGain);
        _hapticPointerBase.ToggleHold =
            PlayerPrefs.GetInt(getKey("ToggleHold"), _hapticPointerBase.ToggleHold ? 1 : 0) == 1;
        _hapticPointerBase.ToggleClutch = 
            PlayerPrefs.GetInt(getKey("ToggleClutch"), _hapticPointerBase.ToggleClutch ? 1 : 0) == 1;
        _hapticPointerBase.HoldChannel =
            PlayerPrefs.GetInt(getKey("HoldChannel"), _hapticPointerBase.HoldChannel);
        _hapticPointerBase.ClutchChannel = 
            PlayerPrefs.GetInt(getKey("ClutchChannel"), _hapticPointerBase.ClutchChannel);
        _hapticPointerBase.CalibrationChannel = 
            PlayerPrefs.GetInt(getKey("CalibrationChannel"), _hapticPointerBase.CalibrationChannel);
        HapticPointer.FixedDeltaTime = 
            PlayerPrefs.GetFloat(getKey("FixedDeltaTime"), HapticPointer.FixedDeltaTime);
    }

    public void check()
    {
        HapticPointerBase.FixedDeltaTime = Mathf.Clamp(HapticPointer.FixedDeltaTime, 0.001f, 0.02f);
        _hapticPointerBase.UnitySpringK = Mathf.Clamp(_hapticPointerBase.UnitySpringK, 0, System.Single.MaxValue);
        _hapticPointerBase.UnityDamperB = Mathf.Clamp(_hapticPointerBase.UnityDamperB, 0, System.Single.MaxValue);
        _hapticPointerBase.PositionScale = Mathf.Clamp(_hapticPointerBase.PositionScale, 1, System.Single.MaxValue);
        _hapticPointerBase.RotationScale = Mathf.Clamp(_hapticPointerBase.RotationScale, 1, System.Single.MaxValue);
        _hapticPointerBase.DeviceSpringK = Mathf.Clamp(_hapticPointerBase.DeviceSpringK, 0, System.Single.MaxValue);
        _hapticPointerBase.DeviceDamperB = Mathf.Clamp(_hapticPointerBase.DeviceDamperB, 0, System.Single.MaxValue);
        _hapticPointerBase.CascadeGain = Mathf.Clamp(_hapticPointerBase.CascadeGain, 0, System.Single.MaxValue);
        _hapticPointerBase.HoldChannel = Mathf.Clamp(_hapticPointerBase.HoldChannel, 0, 8);
        _hapticPointerBase.ClutchChannel = Mathf.Clamp(_hapticPointerBase.ClutchChannel, 0, 8);
        _hapticPointerBase.CalibrationChannel = Mathf.Clamp(_hapticPointerBase.CalibrationChannel, 0, 8);
    }

    string getKey(string keyBase)
    {
        return _hapticPointerBase.name + "_" + keyBase;
    }
}
