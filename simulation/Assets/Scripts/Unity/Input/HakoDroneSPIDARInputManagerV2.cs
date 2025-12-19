using UnityEngine;
using hakoniwa.objects.core;

// SPIDAR関連のusingディレクティブ
using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class HakoDroneSpidarInputManagerV2 : HapticPointerBase, IDroneInput
{
    [Header("Response Curve Settings")]
    // 初期値として、(0,0)から(1,1)へ向かう緩やかなカーブを設定しておきます
    public AnimationCurve inputResponseCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),      // 開始点 (入力0 -> 出力0, 接線フラット)
        new Keyframe(1f, 1f, 2f, 0f)       // 終了点 (入力1 -> 出力1, 接線急角度)
    );
    public Vector3 currentForce = Vector3.zero; // 現在の力ベクトル

    public Mesh     FistMesh            = null;
    public Mesh     PalmMesh            = null;

    public static HakoDroneSpidarInputManagerV2 Instance { get; private set; }
    public Rigidbody    CollidingObject { get { return collidingObject; } }
    public Vector3      PositionOffset  { get; set; }
    public Quaternion   RotationOffset  { get; set; }
    private SpringDamperModel modelHS = new SpringDamperModel();
    private SpringDamperModel modelDP = new SpringDamperModel();
    private Vector3 clutchedPositionOffset = Vector3.zero;
    private Vector3 clutchedPosition = Vector3.zero;
    private Quaternion clutchedRotation = Quaternion.identity;
    private Pose pose;
    private Pose prevPose;
    private Pose rawPose;
    private Pose dronePose;
    private Pose prevDronePose;
    private Rigidbody collidingObject = null;
    private Renderer meshRenderer = null;
    private MeshFilter meshFilter = null;
    private bool curMultiHold = false;
    private int curHoldCount = 0;
    private int updateSkipCount = 0;
    private float holdStartTime = -1f;
    private int refreshFramesRemaining = 0;
    [Header("Debug")]
    public bool DebugAxisOutput = true;
    public float axisDebugInterval = 1.0f;
    [Header("Button Channel Settings")]
    public int ArmChannel = 2; // Example channel for "Arm" button
    public int BButtonChannel = 5; // Example channel for "B" button
    public int XButtonChannel = 6; // Example channel for "X" button
    public int YButtonChannel = 7; // Example channel for "Y" button
    [Header("Haptic Settings")]
    public bool basicForceFeedback = false;
    [Header("Enable/Disable Targets")]
    public bool enableHapticShield = true;
    public bool enableDronePointer = true;
    // [MODIFIED] Sensitivity Settingsを調整
    [Header("Sensitivity Settings")]
    public float maxDisplacement = 1.0f; // スティック入力が最大(-1 or 1)になるDronePointerの移動距離(m)
    public float upDownSensitivity = 1.0f;
    public float forwardBackSensitivity = 1.0f;
    public float rightLeftSensitivity = 1.0f;
    public float yawSensitivity = 1.0f;
    public float yawMaxAngle = 45.0f; 

    [Header("Deadzone Settings")]
    public float positionDeadzone = 0f; // Deadzone for normalized position input
    public float rotationDeadzone = 0.3f;

    // [ADDED] DronePointerへの参照と初期状態を保持する変数を追加
    [Header("Drone Control Target")]
    public Rigidbody hapticShieldRb; // インスペクターからDronePointerオブジェクトを設定
    public Rigidbody dronePointerRb;
    public Transform droneBody; // インスペクターからDroneBodyオブジェクトを設定
    private Vector2 LeftStickInput = Vector2.zero;
    private Vector2 RightStickInput = Vector2.zero;

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

        modelHS.Clear();

        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        collidingObject = null;
        HoldObject();

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
        bool enabledAny = false;

        if (enableHapticShield)
        {
            if (hapticShieldRb == null)
            {
                Debug.LogWarning("⚠️ DronePointer Rigidbody is not assigned in the Inspector.");
            }
            else
            {
                modelHS.Clear();
                modelHS.SpringK = UnitySpringK;
                modelHS.DamperB = UnityDamperB;
                modelHS.pointerOrigin = dronePose;
                modelHS.rigidbodyOrigin = (Pose)hapticShieldRb;
                enabledAny = true;
            }
        }

        if (enableDronePointer)
        {
            if (dronePointerRb == null)
            {
                Debug.LogWarning("⚠️ DronePointer Rigidbody is not assigned in the Inspector.");
            }
            else
            {
                modelDP.Clear();
                modelDP.SpringK = UnitySpringK;
                modelDP.DamperB = UnityDamperB;
                modelDP.pointerOrigin = pose;
                modelDP.rigidbodyOrigin = (Pose)dronePointerRb;
                enabledAny = true;
            }
        }

        if (enabledAny)
        {
            // 保持開始時刻を記録（定期リフレッシュ用）
            holdStartTime = Time.time;
        }

        return enabledAny;
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
        
        // hapticShieldをドローンの中心（ローカル原点）に移動
        if (enableHapticShield && hapticShieldRb != null)
        {
            // hapticShieldをドローンのローカル原点(0, 0, 0)に移動
            hapticShieldRb.transform.localPosition = Vector3.zero;
            hapticShieldRb.transform.localRotation = Quaternion.identity;
            
            Debug.Log("✅ hapticShield moved to drone center and origin set.");
        }

        // --- 追加: キャリブレーション後にモデルとRigidbodyの原点・速度を同期 ---
        // 非kinematicのRigidbodyには以前のモデル差分からの力が残っている可能性があり、
        // それが原因でキャリブレーション直後に押し出される挙動が発生します。
        // ここで holdingObject が DronePointer であれば、モデルの原点を更新し、
        // 物理速度をクリアして不要な瞬間力を抑えます。
        try
        {
            if (enableHapticShield && hapticShieldRb != null)
            {
                // 現在の pose に合わせて pointerOrigin を更新
                modelHS.pointerOrigin = dronePose;

                // Rigidbody 側の基準（位置・回転）も同期
                modelHS.rigidbodyOrigin = dronePose;

                // 既に動いている速度をゼロにして、キャリブ後の瞬間的な力を抑える
                hapticShieldRb.linearVelocity = Vector3.zero;
                hapticShieldRb.angularVelocity = Vector3.zero;

                // Rigidbody の内部状態を安定させるために transform も確実に同期
                hapticShieldRb.transform.localPosition = Vector3.zero;
                hapticShieldRb.transform.localRotation = Quaternion.identity;

                Debug.Log("🔧 Calibrate: synced model.pointerOrigin and cleared Rigidbody velocities for DronePointer.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Calibrate] sync failed: {ex.Message}");
        }

        // DronePointer Rigidbody のキャリブレーション
        if (enableDronePointer && dronePointerRb != null)
        {
            // 速度をゼロにして瞬間力を抑える
            dronePointerRb.linearVelocity = Vector3.zero;
            dronePointerRb.angularVelocity = Vector3.zero;

            // transform も同期
            dronePointerRb.transform.localPosition = Vector3.zero;
            dronePointerRb.transform.localRotation = Quaternion.identity;

            Debug.Log("🔧 Calibrate: synced modelDP.pointerOrigin and cleared Rigidbody velocities for DronePointer.");
        }

    }
    
    void GetSpidarPose()
    {
        PositionOffset = droneBody.position;
        RotationOffset = droneBody.rotation;
        prevPose = pose;
        prevDronePose = dronePose;
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

        // [FIXED] SPIDAR座標系からUnity座標系への変換
        Vector3 spidarPos = rawPose.position;
        Vector3 unityPos = new Vector3(spidarPos.z, -spidarPos.y, spidarPos.x);

        Vector3 spidarVel = rawPose.velocity;
        Vector3 unityVel = new Vector3(spidarVel.z, -spidarVel.y, spidarVel.x);

        Vector3 spidarAvel = rawPose.angularVelocity;
        Vector3 unityAvel = new Vector3(spidarAvel.z, -spidarAvel.y, spidarAvel.x);

        // 回転も座標系に合わせて変換
        Quaternion spidarRot = rawPose.rotation;
        // X軸とY軸の入れ替え、Y軸反転に対応した回転変換
        Quaternion unityRot = new Quaternion(spidarRot.z, -spidarRot.y, spidarRot.x, spidarRot.w);

        pose.position = RotationOffset * (unityPos + clutchedPositionOffset) + PositionOffset;
        pose.rotation = RotationOffset * unityRot;
        pose.velocity = RotationOffset * unityVel;
        pose.angularVelocity = RotationOffset * unityAvel;
        //Debug.Log($"[SPIDAR][HakoDrone] RawPose Pos: {rawPose.position}, Rot: {rawPose.rotation.eulerAngles}");
        // スティック入力に基づいて transform を更新する
        // 左スティック: x=yaw, y=上下
        // 右スティック: x=左右, y=前後
        LeftStickInput = CalcLeftStickInput();
        RightStickInput = CalcRightStickInput();
        //Debug.Log($"[SPIDAR][HakoDrone] LeftStickInput: {LeftStickInput}, RightStickInput: {RightStickInput}");
        Vector3 localPosFromSticks = new Vector3(
            RightStickInput.x * maxDisplacement * rightLeftSensitivity,
            LeftStickInput.y * maxDisplacement * upDownSensitivity,
            RightStickInput.y * maxDisplacement * forwardBackSensitivity
        );

        Vector3 worldPos = RotationOffset * localPosFromSticks + PositionOffset;

        float yawAngle = LeftStickInput.x * yawMaxAngle * yawSensitivity; // degrees
        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);

        Quaternion worldRot = RotationOffset * yawRot;
        pose.position = worldPos;
        pose.rotation = worldRot;
        transform.position = pose.position;
        transform.rotation = pose.rotation;
        
        dronePose.position = droneBody.position;
        dronePose.rotation = droneBody.rotation;
    }
    void SetSpidarForce()
    {
        if (spidar == null)
            return;

        if (!enableHapticShield && !enableDronePointer)
            return;

        spidar.SetHaptics(Haptics);
        spidar.SetCascadeGain(CascadeGain);

        // デバッグ出力（10秒毎）と定期リフレッシュ（長時間ホールドでデバイスが出力を止める回避）
        // if (Time.time - lastSpidarLogTime >= 10.0f)
        // {
        //     float hold = (holdStartTime > 0f) ? Time.time - holdStartTime : -1f;
        //     string holdStr = (hold >= 0f) ? $"{hold:F1}s" : "not holding";
        //     bool spidarIsNull = (spidar == null);
        //     Debug.Log($"[SPIDAR][HakoDrone] Time={Time.time:F1}, holdStart={holdStartTime:F1}, holdTime={holdStr}, refreshFrames={refreshFramesRemaining}, spidarNull={spidarIsNull}");
        //     lastSpidarLogTime = Time.time;
        // }

        // 120秒以上力覚提示を行うとSPIDARが力覚出力を停止する問題への対策
        const float refreshStart = 110f; // 閾値（秒）
        // 閾値越えで次の5フレーム連続で ClearForce を送る
        if (holdStartTime > 0 && Time.time - holdStartTime > refreshStart && refreshFramesRemaining == 0)
        {
            Debug.Log("[SPIDAR][HakoDrone] Hold refresh triggered: clearing force for 5 frames.");
            refreshFramesRemaining = 5;
            // リフレッシュ実行を記録して次回トリガを防ぐ
            holdStartTime = Time.time;
        }

        if (refreshFramesRemaining > 0)
        {
            try
            {
                spidar.ClearForce();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SPIDAR][HakoDrone] ClearForce failed during refresh: {ex.Message}");
            }
            refreshFramesRemaining--;
            return;
        }

        Vector3 g = Vector3.zero;

        float deviceR2 = spidar.GetGripRadius() * spidar.GetGripRadius();

        float forceScale = DeviceSpringK / (UnitySpringK * PositionScale);
        float torqueScale = DeviceSpringK / (UnitySpringK * RotationScale);

        // hapticShieldRbからの力を計算
        Vector3 f = Vector3.zero;
        Vector3 t = Vector3.zero;

        // hapticShieldRbからの力を計算
        if (enableHapticShield && hapticShieldRb != null)
        {
            f += -modelHS.CalcForce(dronePose, hapticShieldRb) * forceScale;
            t += -modelHS.CalcTorque(dronePose, hapticShieldRb) * torqueScale * deviceR2;
        }

        // dronePointerRbからの力も加える
        if (enableDronePointer && dronePointerRb != null)
        {
            Vector3 fDP = -modelDP.CalcForce(pose, dronePointerRb) * forceScale;
            Vector3 tDP = -modelDP.CalcTorque(pose, dronePointerRb) * torqueScale * deviceR2;
            f += fDP;
            t += tDP;
        }

        currentForce = f;

        f = Quaternion.Inverse(RotationOffset) * f;
        t = Quaternion.Inverse(RotationOffset) * t;

        // --- 明示的な軸逆変換（GetSpidarPose の変換 unity = (z, -y, x) の逆） ---
        Vector3 spidarForceVec = new Vector3(f.z, -f.y, f.x);
        Vector3 spidarTorqueVec = new Vector3(t.z, -t.y, t.x);

        f = spidarForceVec;
        t = spidarTorqueVec;

        float forceK = DeviceSpringK;
        float forceB = DeviceDamperB;

        float torqueK = DeviceSpringK * deviceR2;
        float torqueB = DeviceDamperB * deviceR2;

        // SPIDARの基準点へ戻る力を加える
        if (basicForceFeedback)
        {
            Vector3 spidarForce = -rawPose.position * 1.0f;
            Vector3 spidarTorque =  -new Vector3(rawPose.rotation.x, rawPose.rotation.y, rawPose.rotation.z) * 0.5f;
            
            f += spidarForce;
            t += spidarTorque;
        }
        //Debug.Log($"🔧 SPIDAR Force: {f}, Torque: {t}");
        spidar.SetForce(Converter.Convert(f + g), forceK, forceB, Converter.Convert(t), torqueK, torqueB, false, CascadeControl);
    }

    void SetObjectForce()
    {
        float droneTimeStep = Time.fixedDeltaTime * Application.targetFrameRate;

        bool applied = false;

        /////////////////////////////////////
        ////// hapticShieldRbに力を適用 //////
        /////////////////////////////////////
        if (enableHapticShield && hapticShieldRb != null)
        {
            Pose temp = Pose.Lerp(prevDronePose, dronePose, droneTimeStep);
            Vector3 droneForce = hapticShieldRb.mass * modelHS.CalcForce(temp, hapticShieldRb);
            Vector3 droneTorque = hapticShieldRb.inertiaTensor.magnitude * 4 * modelHS.CalcTorque(temp, hapticShieldRb);

            if (curMultiHold)
            {
                droneForce /= (float)curHoldCount;
                droneTorque /= (float)curHoldCount;
            }

            hapticShieldRb.AddForce(droneForce);
            hapticShieldRb.AddTorque(droneTorque);
            applied = true;
        }

        /////////////////////////////////////
        ////// dronePointerRbに力を適用 //////
        /////////////////////////////////////
        if (enableDronePointer && dronePointerRb != null)
        {
            Pose temp = Pose.Lerp(prevPose, pose, droneTimeStep);
            Vector3 droneForce = dronePointerRb.mass * modelDP.CalcForce(temp, dronePointerRb);
            Vector3 droneTorque = dronePointerRb.inertiaTensor.magnitude * 4 * modelDP.CalcTorque(temp, dronePointerRb);

            dronePointerRb.AddForce(droneForce);
            dronePointerRb.AddTorque(droneTorque);
            applied = true;
        }

        if (!applied) return;
    }

    private float ApplyDeadzone(float value, float threshold)
    {
        if (Mathf.Abs(value) < threshold)
        {
            return 0f;
        }
        return Mathf.Sign(value) * (Mathf.Abs(value) - threshold) / (1.0f - threshold);
    }
    // カーブを適用するメソッド
    private float ApplyResponseCurve(float value)
    {
        // 入力の絶対値に対してカーブを評価 (0.0 ～ 1.0)
        float absValue = Mathf.Abs(value);
        float curvedValue = inputResponseCurve.Evaluate(absValue);

        // 符号を元に戻して返す (-1.0 ～ 1.0)
        return Mathf.Sign(value) * curvedValue;
    }

    // [MODIFIED] GetLeftStickInputの実装
    public Vector2 CalcLeftStickInput()
    {
        // SPIDAR の pose を基準にドローン局所座標へ変換して入力を計算する
        if (spidar == null) return Vector2.zero;

        // ドローン局所座標 = RotationOffset^-1 * (pose.position - PositionOffset)
        Vector3 droneLocalPos = Quaternion.Inverse(RotationOffset) * (pose.position - PositionOffset);

        // --- 上下移動 (Y-axis) ---
        float relativeY = droneLocalPos.y;
        float normalizedY = Mathf.Clamp(relativeY / maxDisplacement, -1.0f, 1.0f);
        float processedUpDown = ApplyDeadzone(normalizedY, positionDeadzone);

        // --- ヨー操作 (Yaw) ---
        Quaternion localRot = Quaternion.Inverse(RotationOffset) * pose.rotation;
        float yaw = localRot.eulerAngles.y;
        if (yaw > 180f) yaw -= 360f;
        float normalizedYaw = Mathf.Clamp(yaw / yawMaxAngle, -1.0f, 1.0f);
        float processedYaw = ApplyDeadzone(normalizedYaw, rotationDeadzone);

        // if (DebugAxisOutput)
        // {
        //     Debug.Log($"[DroneInput][LeftStick] yaw={yaw:F1} normalizedYaw={normalizedYaw:F3} processedYaw={processedYaw:F3} upDown={processedUpDown:F3}");
        // }

        return new Vector2(
            Mathf.Clamp(processedYaw * yawSensitivity, -1.0f, 1.0f), // 正が右回転
            Mathf.Clamp(processedUpDown * upDownSensitivity, -1.0f, 1.0f)
        );
    }

    // GetRightStickInputの実装
    public Vector2 CalcRightStickInput()
    {
        // SPIDAR の pose を基準にドローン局所座標へ変換して入力を計算する
        if (spidar == null) return Vector2.zero;

        Vector3 droneLocalPos = Quaternion.Inverse(RotationOffset) * (pose.position - PositionOffset);

        // --- 左右移動 (X-axis) ---
        float relativeX = droneLocalPos.x;
        float normalizedX = Mathf.Clamp(relativeX / maxDisplacement, -1.0f, 1.0f);
        float processedLeftRight = ApplyDeadzone(normalizedX, positionDeadzone);
        processedLeftRight = ApplyResponseCurve(processedLeftRight);

        // --- 前後移動 (Z-axis) ---
        float relativeZ = droneLocalPos.z;
        float normalizedZ = Mathf.Clamp(relativeZ / maxDisplacement, -1.0f, 1.0f);
        float processedForwardBack = ApplyDeadzone(normalizedZ, positionDeadzone);
        processedForwardBack = ApplyResponseCurve(processedForwardBack);

        //Debug.Log($"[RightStick] In:{normalizedX:F2} Dead:{processedLeftRight:F2} Out:{processedLeftRight:F2}");
        
        return new Vector2(
            Mathf.Clamp(processedLeftRight, -1.0f, 1.0f),
            Mathf.Clamp(processedForwardBack, -1.0f, 1.0f)
        );
    }

    public Vector2 GetLeftStickInput()
    {
        return LeftStickInput;
    }
    public Vector2 GetRightStickInput()
    {
        return RightStickInput;
    }

    public bool IsAButtonPressed() {
        var value = GetGpioDown(ArmChannel);
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

