using UnityEngine;
using hakoniwa.objects.core;

// SPIDAR関連のusingディレクティブ
using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;
using UnityEngine.InputSystem;

public class HakoDroneSpidarInputManager : HapticPointerBase, IDroneInput
{
    public bool     TransmitOnCollide   = false;
    public bool     TransmitOnHold      = false;
    public Material HoldingMaterial     = null;
    public Material CollidingMaterial   = null;
    public Material FreeMaterial        = null;
    public Mesh     FistMesh            = null;
    public Mesh     PalmMesh            = null;

    public static HakoDroneSpidarInputManager Instance { get; private set; }
   public Rigidbody    CollidingObject { get { return collidingObject; } }
    public Vector3      PositionOffset  { get; set; }
    public Quaternion   RotationOffset  { get; set; }
    private SpringDamperModel model = new SpringDamperModel();
    private Vector3 clutchedPositionOffset = Vector3.zero;
    private Vector3 clutchedPosition = Vector3.zero;
    private Quaternion clutchedRotation = Quaternion.identity;
    private bool clutchEngaged = true;
    private Pose pose;
    private Pose prevPose;
    private Pose rawPose;

    private uint triggerEnterCount = 0;
    private Rigidbody collidingObject = null;
    private Rigidbody holdingObject = null;
    private Rigidbody transmitObject = null;
    private Renderer meshRenderer = null;
    private MeshFilter meshFilter = null;
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
    }
    void Start()
    {
        PositionOffset = transform.position;
        RotationOffset = transform.rotation;

        model.Clear();

        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        collidingObject = null;
        holdingObject = null;
        transmitObject = null;

        meshRenderer.material = FreeMaterial;
        meshFilter.mesh = PalmMesh;

        requestInit = !Initialize();
    }


    void Update()
    {
        if (updateSkipCount > 0)
        {
            updateSkipCount--;
            return;
        }   
        // if (FixedDeltaTime != Time.fixedDeltaTime)
        //     Time.fixedDeltaTime = FixedDeltaTime;

        if (GetGpioDown(CalibrationChannel))
            Calibrate();
    }

    void FixedUpdate()
    {
        if (spidar != null)
        {
            GetSpidarPose();
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
            meshRenderer.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
        }

        return flag;
    }

    public void Calibrate()
    {
        clutchEngaged = false;

        if (spidar != null)
            spidar.Calibrate();

        clutchedPositionOffset = Vector3.zero;
        clutchedPosition = Vector3.zero;
        clutchedRotation = Quaternion.identity;
        clutchEngaged = true;
        Debug.Log("✅ SPIDAR Calibrated. Initial Pose captured.");
    }
    
    void GetSpidarPose()
    {
        prevPose = pose;

        SpidarVector pos, vel, avel;
        SpidarQuaternion rot;

        pos = vel = avel = SpidarVector.zero;
        rot = SpidarQuaternion.identity;

        if (spidar != null)
            spidar.GetPose(out pos, out rot, out vel, out avel);

        rawPose.position = Converter.ScaleUp(Converter.Convert(pos), PositionScale);
        rawPose.rotation = Converter.ScaleUp(Converter.Convert(rot), RotationScale);
        rawPose.velocity = Converter.ScaleUp(Converter.Convert(vel), PositionScale);
        rawPose.angularVelocity = Converter.ScaleUp(Converter.Convert(avel), RotationScale);

        if (clutchEngaged)
        {
            pose.position = RotationOffset * (rawPose.position + clutchedPositionOffset) + PositionOffset;
            pose.rotation = RotationOffset * rawPose.rotation;
            pose.velocity = RotationOffset * rawPose.velocity;
            pose.angularVelocity = RotationOffset * rawPose.angularVelocity;
        }
        else
        {
            pose.position = RotationOffset * clutchedPosition + PositionOffset;
            pose.rotation = RotationOffset * clutchedRotation;
            pose.velocity = Vector3.zero;
            pose.angularVelocity = Vector3.zero;
        }

        transform.localPosition = pose.position;
        transform.localRotation = pose.rotation;
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
        float yaw = pose.rotation.y;
        if (yaw > 180) yaw -= 360;
        
        float normalizedYaw = Mathf.Clamp(yaw / yawMaxAngle, -1.0f, 1.0f);
        float processedYaw = ApplyDeadzone(normalizedYaw, rotationDeadzone);

        // --- Up/Down (Position Y) ---
        float upDown = pose.position.y * positionScale;
        float processedUpDown = Mathf.Abs(upDown) > positionDeadzone ? upDown : 0f;
        //Debug.Log($"axis[0]: {processedYaw * yawSensitivity}, axis[1]: {processedUpDown * upDownSensitivity}");
        return new Vector2(0.0f, processedUpDown * upDownSensitivity); //processedYaw * yawSensitivity
    }

    public Vector2 GetRightStickInput()
    {
        if (spidar == null) return Vector2.zero;

        // --- Forward/Back (Position Z) & Right/Left (Position X) ---
        float forwardBack = pose.position.z * positionScale;
        float rightLeft = pose.position.x * positionScale;

        float processedForwardBack = Mathf.Abs(forwardBack) > positionDeadzone ? forwardBack : 0f;
        float processedRightLeft = Mathf.Abs(rightLeft) > positionDeadzone ? rightLeft : 0f;
        Debug.Log($"axis[0]: {processedRightLeft * rightLeftSensitivity}, axis[1]: {processedForwardBack * forwardBackSensitivity}");
        return new Vector2(processedRightLeft * rightLeftSensitivity, processedForwardBack * forwardBackSensitivity);
    }

    // --- Button Mappings (Example) ---
    public bool IsAButtonPressed() { return GetGpioDown(ArmChannel); }
    public bool IsBButtonPressed() { return GetGpioDown(BButtonChannel); } // Button 2
    public bool IsXButtonPressed() { return GetGpioDown(XButtonChannel); } // Button 3
    public bool IsYButtonPressed() { return GetGpioDown(YButtonChannel); } // Button 4
    // Release events can be implemented similarly if needed
    public bool IsAButtonReleased() { return GetGpioUp(ArmChannel); }
    public bool IsBButtonReleased() { return GetGpioUp(BButtonChannel); }
    public bool IsXButtonReleased() { return GetGpioUp(XButtonChannel); }
    public bool IsYButtonReleased() { return GetGpioUp(YButtonChannel); }
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
