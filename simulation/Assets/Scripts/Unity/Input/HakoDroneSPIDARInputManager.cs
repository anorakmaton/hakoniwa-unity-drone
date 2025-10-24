using UnityEngine;
using hakoniwa.objects.core;

// SPIDAR関連のusingディレクティブ
using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;
using UnityEngine.InputSystem;

public class HakoDroneSpidarInputManager : HapticPointerBase, IDroneInput
{
    // ... (既存のpublic変数は変更なし) ...
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
    private Renderer meshRenderer = null;
    private MeshFilter meshFilter = null;
    private bool curMultiHold = false;
    private int curHoldCount = 0;
    private int updateSkipCount = 0;
    public int ArmChannel = 2; // Example channel for "Arm" button
    public int BButtonChannel = 5; // Example channel for "B" button
    public int XButtonChannel = 6; // Example channel for "X" button
    public int YButtonChannel = 7; // Example channel for "Y" button
    
    // [MODIFIED] Sensitivity Settingsを調整
    [Header("Sensitivity Settings")]
    public float maxDisplacement = 2.5f; // スティック入力が最大(-1 or 1)になるDronePointerの移動距離(m)
    public float upDownSensitivity = 1.0f;
    public float forwardBackSensitivity = 1.0f;
    public float rightLeftSensitivity = 1.0f;
    public float yawSensitivity = -1.0f;
    public float yawMaxAngle = 45.0f; 

    [Header("Deadzone Settings")]
    public float positionDeadzone = 0.4f; // Deadzone for normalized position input
    public float rotationDeadzone = 0.1f;

    // [ADDED] DronePointerへの参照と初期状態を保持する変数を追加
    [Header("Drone Control Target")]
    public Rigidbody dronePointer; // インスペクターからDronePointerオブジェクトを設定
    public Transform dronePointerTransform; // インスペクターからDronePointerオブジェクトを設定
    private Vector3 initialDronePointerPosition;
    private Quaternion initialDronePointerRotation;

    public Transform droneBody; // インスペクターからDroneBodyオブジェクトを設定

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

        meshRenderer.material = FreeMaterial;
        meshFilter.mesh = PalmMesh;

        requestInit = !Initialize();
        
        // // [MODIFIED] Start時に一度キャリブレーションを実行
        // // これにより、起動時のDronePointerの位置が原点になる
        // if (spidar != null)
        // {
        //     Calibrate();
        // }
    }

    public bool HoldObject()
    {
        meshFilter.mesh = FistMesh;
     
        if (holdingObject != null || collidingObject == null || !collidingObject.CompareTag("DronePointer"))
            return false;
        Debug.Log($"✅ Holding object: {collidingObject.name}");
        holdingObject = collidingObject;
        
        // [ADDED] DronePointerを掴んだ時に、それが操作対象であることを確認
        if (holdingObject == dronePointer)
        {
            Debug.Log("✅ DronePointer is now being controlled.");
        }

        HoldState hs = GetHoldState();
        hs.OnHoldObject();

        model.Clear();
        model.SpringK = UnitySpringK;
        model.DamperB = UnityDamperB;
        model.pointerOrigin = pose;
        model.rigidbodyOrigin = (Pose)holdingObject;

        meshRenderer.material = HoldingMaterial;

        return true;
    }

    public bool ReleaseObject()
    {
        meshFilter.mesh = PalmMesh;

        if (holdingObject == null)
            return false;

        // DronePointerは常に物理演算を維持（kinematic設定は不要）
        Debug.Log($"🔧 Released object: {holdingObject.name}");

        if (spidar != null)
            spidar.ClearForce();

        HoldState hs = GetHoldState();
        hs.OnReleaseObject();
        RemoveHoldState();

        holdingObject = null;

        model.Clear();

        if (collidingObject != null)
        {
            meshRenderer.material = CollidingMaterial;
        }
        else
        {
            meshRenderer.material = FreeMaterial;
        }

        return true;
    }
    public bool EngageClutch()
    {
        if (clutchEngaged)
            return false;

        clutchedPositionOffset = clutchedPosition - rawPose.position;

        pose.position = RotationOffset * (rawPose.position + clutchedPositionOffset) + PositionOffset;
        pose.rotation = RotationOffset * rawPose.rotation;
        pose.velocity = Vector3.zero;
        pose.angularVelocity = Vector3.zero;

        prevPose = pose;

        if (holdingObject != null)
        {
            Quaternion q = QuaternionUtility.Rotate(model.pointerOrigin.rotation, RotationOffset * clutchedRotation);
            model.pointerOrigin.rotation = Quaternion.Inverse(q) * pose.rotation;
        }
        clutchEngaged = true;

        return true;
    }
    public bool ReleaseClutch()
    {
        if (!clutchEngaged)
            return false;

        clutchedPosition = rawPose.position + clutchedPositionOffset;
        clutchedRotation = rawPose.rotation;
        clutchEngaged = false;

        return true;
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

        // スペースキーが押されたときにもCalibrateを実行
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Calibrate();

        if (ToggleClutch)
        {
            if (GetGpioDown(ClutchChannel) && !ReleaseClutch())
                EngageClutch();
        }
        else
        {
            if (GetGpioDown(ClutchChannel))
                ReleaseClutch();

            if (GetGpioUp(ClutchChannel))
                EngageClutch();
        }
    }

    void FixedUpdate()
    {
        if (spidar != null)
        {
            GetSpidarPose();
            SetSpidarForce();
            SetObjectForce();
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        ++triggerEnterCount;

        collidingObject = collider.GetComponentInParent<Rigidbody>();

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

        if (meshRenderer != null && FreeMaterial != null)
        {
            meshRenderer.material = FreeMaterial;
        }
    }
    public override bool Initialize()
    {
        bool flag = base.Initialize();
        if (spidar != null)
        {
            updateSkipCount = 100;
            if (meshRenderer != null) meshRenderer.enabled = true;
        }
        else
        {
            if (meshRenderer != null) meshRenderer.enabled = false;
        }
        return flag;
    }


    // [MODIFIED] Calibrateメソッドを拡張
    public void Calibrate()
    {
        // HapticPointer (SPIDAR自身) のキャリブレーション
        if (spidar == null) return;

        spidar.Calibrate();
        
        // DronePointerをドローンの中心（ローカル原点）に移動
        if (dronePointer != null)
        {
            // DronePointerをドローンのローカル原点(0, 0, 0)に移動
            dronePointer.transform.localPosition = Vector3.zero;
            dronePointer.transform.localRotation = Quaternion.identity;
            
            // 移動後の位置を初期位置として保存
            initialDronePointerPosition = dronePointer.position;
            initialDronePointerRotation = dronePointer.rotation;
            
            Debug.Log("✅ DronePointer moved to drone center and origin set.");
        } else {
            Debug.LogWarning("⚠️ DronePointer is not assigned in the Inspector.");
        }
    }
    
    // ... (GetSpidarPose, SetSpidarForce, SetObjectForce, GetHoldState, ApplyDeadzone, RemoveHoldStateは変更なし) ...
    void GetSpidarPose()
    {
        prevPose = pose;

        SpidarVector pos, vel, avel;
        SpidarQuaternion rot;

        pos = vel = avel = SpidarVector.zero;
        rot = SpidarQuaternion.identity;

        if (spidar != null)
            spidar.GetPose(out pos, out rot, out vel, out avel);

        // [FIXED] SPIDAR座標系からUnity座標系への変換
        Vector3 spidarPos = Converter.Convert(pos);
        Vector3 unityPos = new Vector3(spidarPos.z, -spidarPos.y, spidarPos.x);
        
        Vector3 spidarVel = Converter.Convert(vel);
        Vector3 unityVel = new Vector3(spidarVel.z, -spidarVel.y, spidarVel.x);

        Vector3 spidarAvel = Converter.Convert(avel);
        Vector3 unityAvel = new Vector3(spidarAvel.z, -spidarAvel.y, spidarAvel.x);

        // 回転も座標系に合わせて変換
        Quaternion spidarRot = Converter.Convert(rot);
        // X軸とY軸の入れ替え、Y軸反転に対応した回転変換
        Quaternion unityRot = new Quaternion(spidarRot.z, -spidarRot.y, spidarRot.x, spidarRot.w);

        rawPose.position = Converter.ScaleUp(unityPos, PositionScale);
        rawPose.rotation = Converter.ScaleUp(unityRot, RotationScale);
        rawPose.velocity = Converter.ScaleUp(unityVel, PositionScale);
        rawPose.angularVelocity = Converter.ScaleUp(unityAvel, RotationScale);
        
        // ドローンの動きも反映
        if (droneBody != null)
        {
            // ドローンの位置と回転を取得
            Vector3 dronePosition = droneBody.position;
            Quaternion droneRotation = droneBody.rotation;

            // DronePointerの位置と回転にドローンの変化を加算
            rawPose.position += new Vector3(dronePosition.x, dronePosition.y, dronePosition.z);
            //rawPose.rotation = droneRotation * rawPose.rotation;
        }
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

        if (holdingObject == null || !clutchEngaged) return;

        HoldState hs = GetHoldState();

        Vector3 g = Vector3.zero;

        if (Gravity)
        {
            g = Vector3.down * holdingObject.mass * 9.81f;
            g = Quaternion.Inverse(RotationOffset) * g;
        }

        if (!hs.Collision && !curMultiHold)
        {
            if (Gravity)
            {
                spidar.SetForce(Converter.Convert(g), 0, 0, Converter.Convert(Vector3.zero), 0, 0, true, false);
            }
            else
            {
                spidar.ClearForce(true);
            }
            return;
        }

        float deviceR2 = spidar.GetGripRadius() * spidar.GetGripRadius();

        float forceScale = DeviceSpringK / (UnitySpringK * PositionScale);
        float torqueScale = DeviceSpringK / (UnitySpringK * RotationScale);

        Vector3 f = -model.CalcForce(pose, holdingObject) * forceScale;
        Vector3 t = -model.CalcTorque(pose, holdingObject) * torqueScale * deviceR2;

        f = Quaternion.Inverse(RotationOffset) * f;
        t = Quaternion.Inverse(RotationOffset) * t;

        float forceK = DeviceSpringK;
        float forceB = DeviceDamperB;

        float torqueK = DeviceSpringK * deviceR2;
        float torqueB = DeviceDamperB * deviceR2;

        // [FIXED] SPIDAR座標系からUnity座標系への変換
        Vector3 spidarF = new Vector3(f.z, -f.y, f.x);
        Vector3 spidarT = new Vector3(t.z, -t.y, t.x);
        spidar.SetForce(Converter.Convert(spidarF + g), forceK, forceB, Converter.Convert(spidarT), torqueK, torqueB, false, CascadeControl);
    }

    void SetObjectForce()
    {
        if (holdingObject == null) return;

        // DronePointerの場合は特別な処理（物理演算を維持して壁衝突を処理）
        if (holdingObject.CompareTag("DronePointer"))
        {
            // 通常の物理力を適用
            float droneTimeStep = Time.fixedDeltaTime * Application.targetFrameRate;
            Pose dronePose = Pose.Lerp(prevPose, pose, droneTimeStep);

            Vector3 droneForce = holdingObject.mass * model.CalcForce(dronePose, holdingObject);
            Vector3 droneTorque = holdingObject.inertiaTensor.magnitude * 4 * model.CalcTorque(dronePose, holdingObject);

            if (curMultiHold)
            {
                droneForce /= (float)curHoldCount;
                droneTorque /= (float)curHoldCount;
            }

            holdingObject.AddForce(droneForce);
            holdingObject.AddTorque(droneTorque);
            
            return;
        }

        // 通常のオブジェクト（DronePointer以外）は従来通りの物理力適用
        float timeStep = Time.fixedDeltaTime * Application.targetFrameRate;
        Pose temp = Pose.Lerp(prevPose, pose, timeStep);

        Vector3 force = holdingObject.mass * model.CalcForce(temp, holdingObject); 
        Vector3 torque = holdingObject.inertiaTensor.magnitude * 4 * model.CalcTorque(temp, holdingObject);

        if (curMultiHold)
        {
            force /= (float)curHoldCount;
            torque /= (float)curHoldCount;
        }

        holdingObject.AddForce(force);
        holdingObject.AddTorque(torque);
    }
    HoldState GetHoldState()
    {
        GameObject obj = holdingObject.gameObject;
        HoldState [] hsList = obj.GetComponents<HoldState>();
        for (int i = 0; i < hsList.Length; ++i)
            if (hsList[i].Owner == this)
                return hsList[i];
        HoldState hs = obj.AddComponent<HoldState>();
        hs.Owner = this;
        return hs;
    }
    private float ApplyDeadzone(float value, float threshold)
    {
        if (Mathf.Abs(value) < threshold)
        {
            return 0f;
        }
        return Mathf.Sign(value) * (Mathf.Abs(value) - threshold) / (1.0f - threshold);
    }
    void RemoveHoldState()
    {
        GameObject obj = holdingObject.gameObject;
        HoldState[] hsList = obj.GetComponents<HoldState>();
        for (int i = 0; i < hsList.Length; ++i)
            if (hsList[i].Owner == this)
                Destroy(hsList[i]);
    }


    // [MODIFIED] GetLeftStickInputの実装
    public Vector2 GetLeftStickInput()
    {
        if (dronePointer == null) return Vector2.zero;

        // --- 上下移動 (Y-axis) ---
        float relativeY = dronePointerTransform.localPosition.y;
        // Normalize displacement to -1 ~ 1 range
        float normalizedY = Mathf.Clamp(relativeY / maxDisplacement, -1.0f, 1.0f);
        float processedUpDown = ApplyDeadzone(normalizedY, positionDeadzone);

        // --- ヨー操作 (Yaw) ---
        // ドローン本体とDronePointerのローカル回転の差分からYawを計算
        float droneYaw = 0f;
        if (droneBody != null)
        {
            // ドローン本体のYaw（Y軸回りの回転角度）
            droneYaw = droneBody.rotation.eulerAngles.y;
        }
        float pointerYaw = dronePointerTransform.rotation.eulerAngles.y;
        float yawDiff = droneYaw - pointerYaw;
        if (yawDiff > 180f) yawDiff -= 360f;
        if (yawDiff < -180f) yawDiff += 360f;

        float normalizedYaw = Mathf.Clamp(yawDiff / yawMaxAngle, -1.0f, 1.0f);
        float processedYaw = ApplyDeadzone(normalizedYaw, rotationDeadzone);

        Debug.Log($"🔧 DronePointer YawDiff: {yawDiff} degrees (Pointer: {pointerYaw}, Drone: {droneYaw}), value: {Mathf.Clamp(processedYaw * yawSensitivity, -1.0f, 1.0f)}");
        // Apply sensitivity and clamp the final value to the -1 ~ 1 range
        return new Vector2(
            Mathf.Clamp(processedYaw * yawSensitivity, -1.0f, 1.0f), // 正が右回転
            Mathf.Clamp(processedUpDown * upDownSensitivity, -1.0f, 1.0f)
        );
    }

    // [MODIFIED] GetRightStickInputの実装
    public Vector2 GetRightStickInput()
    {
        if (dronePointer == null) return Vector2.zero;

        // --- 左右移動 (X-axis) ---
        float relativeX = dronePointerTransform.localPosition.x;
        // Normalize displacement to -1 ~ 1 range
        float normalizedX = Mathf.Clamp(relativeX / maxDisplacement, -1.0f, 1.0f);
        float processedLeftRight = ApplyDeadzone(normalizedX, positionDeadzone);

        // --- 前後移動 (Z-axis) ---
        float relativeZ = dronePointerTransform.localPosition.z;
        // Normalize displacement to -1 ~ 1 range
        float normalizedZ = Mathf.Clamp(relativeZ / maxDisplacement, -1.0f, 1.0f);
        float processedForwardBack = ApplyDeadzone(normalizedZ, positionDeadzone);
        
        // Apply sensitivity and clamp the final value to the -1 ~ 1 range
        return new Vector2(
            Mathf.Clamp(processedLeftRight * rightLeftSensitivity, -1.0f, 1.0f),
            Mathf.Clamp(processedForwardBack * forwardBackSensitivity, -1.0f, 1.0f)
        );
    }

    public bool IsAButtonPressed() {
        var value = GetGpioDown(ArmChannel);
        if (ToggleHold)
        {
            if (value && !ReleaseObject())
                HoldObject();
        }
        else
        {
            if (value)
                HoldObject();
        }
        return value;
    }
    public bool IsBButtonPressed() { return GetGpioDown(BButtonChannel); } 
    public bool IsXButtonPressed() { return GetGpioDown(XButtonChannel); }
    public bool IsYButtonPressed() { return GetGpioDown(YButtonChannel); }
    public bool IsAButtonReleased() { return GetGpioUp(ArmChannel); }
    public bool IsBButtonReleased() { return GetGpioUp(BButtonChannel); }
    public bool IsXButtonReleased() { return GetGpioUp(XButtonChannel); }
    public bool IsYButtonReleased() { return GetGpioUp(YButtonChannel); }
    public bool IsUpButtonPressed() { return false; }
    public bool IsUpButtonReleased() { return false; }
    public bool IsDownButtonPressed() { return false; }
    public bool IsDownButtonReleased() { return false; }
    public void DoVibration(bool isRightHand, float frequency, float amplitude, float durationSec){}
    public void StopVibration(bool isRightHand){}
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

