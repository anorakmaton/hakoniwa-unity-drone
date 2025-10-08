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
        if (FixedDeltaTime != Time.fixedDeltaTime)
            Time.fixedDeltaTime = FixedDeltaTime;

        if (GetGpioDown(CalibrationChannel))
            Calibrate();
    }

    void FixedUpdate()
    {
        if (spidar != null)
        {
            GetSpidarPose();
            SetSpidarForce();
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        ++triggerEnterCount;

        collidingObject = collider.GetComponentInParent<Rigidbody>();

        // 接触時の視覚的フィードバック
        if (meshRenderer != null && CollidingMaterial != null)
        {
            meshRenderer.material = CollidingMaterial;
        }

        // 詳細なデバッグ情報
        Debug.Log($"🔶 SPIDAR OnTriggerEnter: {collider.name} (Layer: {collider.gameObject.layer})");
        Debug.Log($"🔶 TriggerEnterCount: {triggerEnterCount}");
        Debug.Log($"🔶 CollidingObject: {(collidingObject ? collidingObject.name : "null")}");
    }

    void OnTriggerExit(Collider collider)
    {
        --triggerEnterCount;
        if (triggerEnterCount > 0) return;

        collidingObject = null;

        // 接触終了時の視覚的フィードバック
        if (meshRenderer != null && FreeMaterial != null)
        {
            meshRenderer.material = FreeMaterial;
        }

        Debug.Log("🔷 SPIDAR released from object");
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
        
        // Colliderの設定を確認・追加
        SetupCollider();
        
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

    /// <summary>
    /// OnTriggerEnter/Exitが動作するようにColliderを設定
    /// </summary>
    private void SetupCollider()
    {
        // 既存のColliderをチェック
        Collider existingCollider = GetComponent<Collider>();
        
        if (existingCollider == null)
        {
            // Colliderがない場合は球体Colliderを追加
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.05f; // 5cm の半径
            Debug.Log("🔶 Added SphereCollider for SPIDAR touch detection");
        }
        else
        {
            // 既存のColliderをTriggerに設定
            existingCollider.isTrigger = true;
            Debug.Log("🔶 Set existing Collider as Trigger for SPIDAR touch detection");
        }

        // Rigidbodyの確認・追加
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 物理的に動かされないように設定
            rb.useGravity = false; // 重力の影響を受けない
            Debug.Log("🔶 Added kinematic Rigidbody for SPIDAR collision detection");
        }
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

    void SetSpidarForce()
    {
        if (spidar == null)
            return;

        spidar.SetHaptics(Haptics);
        spidar.SetCascadeGain(CascadeGain);

        // 接触していない場合は重力のみ
        if (collidingObject == null || !clutchEngaged)
        {
            Vector3 g = Vector3.zero;
            if (Gravity)
            {
                // 基本的な重力フィードバック
                g = Vector3.down * 0.5f; // 軽い重力感覚
                g = Quaternion.Inverse(RotationOffset) * g;
                spidar.SetForce(Converter.Convert(g), 0, 0, Converter.Convert(Vector3.zero), 0, 0, true, false);
            }
            else
            {
                spidar.ClearForce(true);
            }
            return;
        }

        // 接触時の反力計算
        float deviceR2 = spidar.GetGripRadius() * spidar.GetGripRadius();

        // 接触反力のパラメータ
        float contactSpringK = DeviceSpringK * 2.0f; // 接触時は強めの反力
        float contactDamperB = DeviceDamperB * 1.5f;

        // 単純な位置ベースの反力（押し返し）
        Vector3 contactForce = Vector3.zero;
        if (collidingObject != null)
        {
            // SPIDARが物体に侵入している深度に応じた反力
            Collider objectCollider = collidingObject.GetComponent<Collider>();
            if (objectCollider != null)
            {
                Vector3 closestPoint = objectCollider.ClosestPoint(transform.position);
                Vector3 penetration = transform.position - closestPoint;
                
                if (penetration.magnitude > 0.001f) // 侵入している場合
                {
                    contactForce = -penetration.normalized * Mathf.Min(penetration.magnitude * contactSpringK, contactSpringK);
                }
            }
        }

        // 重力も加算
        Vector3 gravity = Vector3.zero;
        if (Gravity)
        {
            gravity = Vector3.down * 0.5f;
        }

        Vector3 totalForce = contactForce + gravity;
        totalForce = Quaternion.Inverse(RotationOffset) * totalForce;

        spidar.SetForce(Converter.Convert(totalForce), contactSpringK, contactDamperB, 
                       Converter.Convert(Vector3.zero), contactSpringK * deviceR2, contactDamperB * deviceR2, 
                       false, CascadeControl);
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

        PointerParameter parameter = new PointerParameter(this);
        parameter.serialize();
    }
}
