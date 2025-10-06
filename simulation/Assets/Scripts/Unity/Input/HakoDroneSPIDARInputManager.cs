using UnityEngine;
using hakoniwa.objects.core;

// SPIDAR関連のusingディレクティブ
using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;

public class HakoDroneSpidarInputManager : HapticPointerBase, IDroneInput
{
    public static HakoDroneSpidarInputManager Instance { get; private set; }

    private Vector3 initialSpidarPosition;
    private Quaternion initialSpidarRotation;

    private Vector3 currentSpidarPosition;
    private Quaternion currentSpidarRotation;
    //private Renderer meshRenderer = null;
    private int updateSkipCount = 0;

    private uint prevGpioState = 0;
    public int ArmChannel = 2; // Example channel for "Arm" button
    public int BButtonChannel = 5; // Example channel for "B" button
    public int XButtonChannel = 6; // Example channel for "X" button
    public int YButtonChannel = 7; // Example channel for "Y" button
    
    // --- Public parameters for Inspector tuning ---
    [Header("Sensitivity Settings")]
    public float positionScale = 100.0f; // SPIDAR position scale
    public float upDownSensitivity = 5.0f;
    public float forwardBackSensitivity = 1.0f;
    public float rightLeftSensitivity = 1.0f;
    public float yawSensitivity = -1.0f;
    public float yawMaxAngle = 45.0f; // Max angle for yaw input

    [Header("Deadzone Settings")]
    public float positionDeadzone = 0.01f; // Deadzone in meters
    public float rotationDeadzone = 0.1f; // Deadzone for normalized rotation

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }

    void Update()
    {
        if (updateSkipCount > 0)
        {
            updateSkipCount--;
            return;
        }
        if (spidar != null)
        {
            UpdateSpidarPose();
        }
    }

    /// <summary>
    /// SPIDARの初期化を明示的に実行する．
    /// 初期化に成功した場合はtrueを返す．
    /// </summary> 
    /// <returns>
    /// true: 成功
    /// false: 失敗
    /// </returns> 
    public override bool Initialize()
    {
        Debug.Log("🔵 Initializing SPIDAR...");
        bool flag = base.Initialize();
        Debug.Log("🔵 SPIDAR Initialize: " + flag);
        if (spidar != null)
        {
            updateSkipCount = 100;
            //meshRenderer.enabled = true;
        }
        else
        {
            //meshRenderer.enabled = false;
        }

        return flag;
    }

    public void Calibrate()
    {
        if (spidar == null) return;
        
        SpidarVector pos;
        SpidarQuaternion rot;
        spidar.GetPose(out pos, out rot, out _, out _);
        
        initialSpidarPosition = new Vector3(pos.x, pos.y, pos.z);
        initialSpidarRotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);
        
        Debug.Log("✅ SPIDAR Calibrated. Initial Pose captured.");
    }
    
    private void UpdateSpidarPose()
    {
        SpidarVector pos;
        SpidarQuaternion rot;
        spidar.GetPose(out pos, out rot, out _, out _);
        
        currentSpidarPosition = new Vector3(pos.x, pos.y, pos.z);
        currentSpidarRotation = new Quaternion(rot.x, rot.y, rot.z, rot.w);
    }
    
    private float ApplyDeadzone(float value, float threshold)
    {
        if (Mathf.Abs(value) < threshold)
        {
            return 0f;
        }
        return Mathf.Sign(value) * (Mathf.Abs(value) - threshold) / (1.0f - threshold);
    }
    
    public Vector2 GetLeftStickInput()
    {
        if (spidar == null) return Vector2.zero;
        
        // --- Yaw (Rotation) ---
        // Calculate relative rotation from calibrated orientation
        Quaternion relativeRotation = Quaternion.Inverse(initialSpidarRotation) * currentSpidarRotation;
        float yaw = relativeRotation.eulerAngles.y;
        if (yaw > 180) yaw -= 360;
        
        float normalizedYaw = Mathf.Clamp(yaw / yawMaxAngle, -1.0f, 1.0f);
        float processedYaw = ApplyDeadzone(normalizedYaw, rotationDeadzone);

        // --- Up/Down (Position Y) ---
        float upDown = (currentSpidarPosition.y - initialSpidarPosition.y) * positionScale;
        float processedUpDown = Mathf.Abs(upDown) > positionDeadzone ? upDown : 0f;
        
        return new Vector2(processedYaw * yawSensitivity, processedUpDown * upDownSensitivity);
    }

    public Vector2 GetRightStickInput()
    {
        if (spidar == null) return Vector2.zero;

        // --- Forward/Back (Position Z) & Right/Left (Position X) ---
        float forwardBack = (currentSpidarPosition.z - initialSpidarPosition.z) * positionScale;
        float rightLeft = (currentSpidarPosition.x - initialSpidarPosition.x) * positionScale;

        float processedForwardBack = Mathf.Abs(forwardBack) > positionDeadzone ? forwardBack : 0f;
        float processedRightLeft = Mathf.Abs(rightLeft) > positionDeadzone ? rightLeft : 0f;

        return new Vector2(processedRightLeft * rightLeftSensitivity, processedForwardBack * forwardBackSensitivity);
    }

    // private bool GetSpidarButtonDown(int channel)
    // {
    //     if (spidar == null) return false;
        
    //     // Note: SPIDAR GPIO seems to be 1 for released, 0 for pressed.
    //     uint currentGpioState = spidar.GetGpioValue();
    //     bool wasPressed = (prevGpioState & (1 << channel)) == 0;
    //     bool isPressed = (currentGpioState & (1 << channel)) == 0;
        
    //     return isPressed && !wasPressed;
    // }

    void LateUpdate()
    {
        // Update previous state at the end of the frame
        if (spidar != null)
        {
            prevGpioState = spidar.GetGpioValue();
        }
    }

    // --- Button Mappings (Example) ---
    public bool IsAButtonPressed() {
        var value = GetGpioDown(ArmChannel);
        if (value) {
            Debug.Log("1🔵 SPIDAR Arm Button Pressed.");
        }
        return value; // Button 1
    }
    public bool IsBButtonPressed() { return GetGpioDown(BButtonChannel); } // Button 2
    public bool IsXButtonPressed() { return GetGpioDown(XButtonChannel); } // Button 3
    public bool IsYButtonPressed() { return GetGpioDown(YButtonChannel); } // Button 4
    // Release events can be implemented similarly if needed
    public bool IsAButtonReleased() { return GetGpioUp(0); }
    public bool IsBButtonReleased() { return GetGpioUp(1); }
    public bool IsXButtonReleased() { return GetGpioUp(2); }
    public bool IsYButtonReleased() { return GetGpioUp(3); }
    public bool IsUpButtonPressed() { return false; }
    public bool IsUpButtonReleased() { return false; }
    public bool IsDownButtonPressed() { return false; }
    public bool IsDownButtonReleased() { return false; }
    public void DoVibration(bool isRightHand, float frequency, float amplitude, float durationSec)
    {
        // SPIDAR does not support vibration; method left empty intentionally.
    }
    public void StopVibration(bool isRightHand)
    {
        // SPIDAR does not support vibration; method left empty intentionally.
    }

    private void OnDestroy()
    {
        if (spidar != null)
        {
            spidar.Stop();
            spidar.Dispose();
            Debug.Log("✅ SPIDAR Terminated.");
        }
    }
}
