//
// HapticPointer.cs 
//
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.IO;

using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;

//
// class HapticPointer
//
public class HapticDronePointer : HapticPointerBase
{
    public bool     TransmitOnCollide   = false;
    public bool     TransmitOnHold      = false;
    public Material HoldingMaterial     = null;
    public Material CollidingMaterial   = null;
    public Material FreeMaterial        = null;
    public Mesh     FistMesh            = null;
    public Mesh     PalmMesh            = null;

    //
    public Rigidbody    CollidingObject { get { return collidingObject; } }
    public Rigidbody    HoldingObject   { get { return holdingObject; } }
    public Vector3      PositionOffset  { get; set; }
    public Quaternion   RotationOffset  { get; set; }

    //
    private SpringDamperModel model = new SpringDamperModel();

    private bool clutchEngaged = true;
    private Vector3 clutchedPositionOffset = Vector3.zero;
    private Vector3 clutchedPosition = Vector3.zero;
    private Quaternion clutchedRotation = Quaternion.identity;

    private Pose pose;
    private Pose prevPose;
    private Pose rawPose;

    private uint triggerEnterCount = 0;
    private Rigidbody collidingObject = null;
    private Rigidbody holdingObject = null;
    private Rigidbody transmitObject = null;

    private Renderer meshRenderer = null;
    private MeshFilter meshFilter = null;

    private bool curMultiHold = false;
    private bool prvMultiHold = false;
    private int curHoldCount = 0;

    private int updateSkipCount = 0;
    private Hashtable stockMaterial = new Hashtable();


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
        bool flag = base.Initialize();

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
    /// 触れている剛体オブジェクトを掴む．
    /// オブジェクトを掴むことに成功した場合はtrueを返す．
    /// 触れているオブジェクトがない場合や，すでにオブジェクトを掴んでいる場合はなにもせずにfalseを返す．
    /// </summary> 
    /// <returns>
    /// true: 成功
    /// false: 失敗
    /// </returns> 
    public bool HoldObject()
    {
        meshFilter.mesh = FistMesh;

        if (holdingObject != null || collidingObject == null)
            return false;

        holdingObject = collidingObject;

        HoldState hs = GetHoldState();
        hs.OnHoldObject();

        model.Clear();
        model.SpringK = UnitySpringK;
        model.DamperB = UnityDamperB;
        model.pointerOrigin = pose;
        model.rigidbodyOrigin = (Pose)holdingObject;

        meshRenderer.material = HoldingMaterial;

        TransmitObject(holdingObject, TransmitOnHold);

        return true;
    }

    /// <summary>
    /// 掴んでいるオブジェクトを放す．
    /// 成功した場合はtrueを返し，オブジェクトを掴んでいない場合は何もせずにfalseを返す．
    /// </summary> 
    /// <returns>
    /// true: 成功
    /// false: 失敗
    /// </returns>   
    public bool ReleaseObject()
    {
        meshFilter.mesh = PalmMesh;

        if (holdingObject == null)
            return false;

        if (spidar != null)
            spidar.ClearForce();

        HoldState hs = GetHoldState();
        hs.OnReleaseObject();
        RemoveHoldState();

        holdingObject = null;

        model.Clear();

        TransmitObject(null, false);

        if (collidingObject != null)
        {
            meshRenderer.material = CollidingMaterial;
            TransmitObject(collidingObject, TransmitOnCollide);
        }
        else
        {
            meshRenderer.material = FreeMaterial;
        }

        return true;
    }

    /// <summary>
    /// クラッチをつなぎ，クラッチが離れている間の姿勢の変化によるフセット値を更新する．
    /// 成功した場合はtrueを返し，クラッチが離れていない場合は何もせずにfalseを返す．
    /// </summary> 
    /// <returns>
    /// true: 成功
    /// false: 失敗
    /// </returns>  
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

    /// <summary>
    /// クラッチを離し，その際のグリップの姿勢を保存する．
    /// 成功した場合はtrueを返し，クラッチがすでに離れている場合は何もせずにfalseを返す．
    /// </summary> 
    /// <returns>
    /// true: 成功
    /// false: 失敗
    /// </returns>  
    public bool ReleaseClutch()
    {
        if (!clutchEngaged)
            return false;

        clutchedPosition = rawPose.position + clutchedPositionOffset;
        clutchedRotation = rawPose.rotation;
        clutchEngaged = false;

        return true;
    }

    /// <summary>
    /// デバイスのグリップ姿勢のキャリブレーションを行う．
    /// 安全のため，オブジェクトを掴んでいる場合は離し，クラッチによるオフセット値もクリアする．
    /// </summary> 
    public void Calibrate()
    {
        if (holdingObject != null)
            ReleaseObject();

        clutchEngaged = false;

        if (spidar != null)
            spidar.Calibrate();

        clutchedPositionOffset = Vector3.zero;
        clutchedPosition = Vector3.zero;
        clutchedRotation = Quaternion.identity;
        clutchEngaged = true;
    }

    //
    // private functions
    //

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

    void OnDestroy()
    {
        ReleaseObject();

        if (spidar != null)
        {
            spidar.Stop();
            spidar.Dispose();
        }

        PointerParameter parameter = new PointerParameter(this);
        parameter.serialize();
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

        if (ToggleHold)
        {
            if (GetGpioDown(HoldChannel) && !ReleaseObject())
                HoldObject();
        }
        else
        {
            if (GetGpioDown(HoldChannel))
                HoldObject();

            if (GetGpioUp(HoldChannel))
                ReleaseObject();
        }

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
        CheckMultiHold();
        GetDronePose();
        SetSpidarForce();
        SetObjectForce();
    }

    void OnTriggerEnter(Collider collider)
    {
        ++triggerEnterCount;

        collidingObject = collider.GetComponentInParent<Rigidbody>();

        if (holdingObject == null)
        {
            meshRenderer.material = CollidingMaterial;
            TransmitObject(collidingObject, TransmitOnCollide);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        --triggerEnterCount;
        if (triggerEnterCount > 0) return;

        collidingObject = null;

        if (holdingObject == null)
        {
            meshRenderer.material = FreeMaterial;
            TransmitObject(null, false);
        }
    }

    void CheckMultiHold()
    {
        if (holdingObject == null)
        {
            curHoldCount = 0;
            curMultiHold = false;
            prvMultiHold = false;
            return;
        }

        GameObject obj = holdingObject.gameObject;

        HoldState[] hsList = obj.GetComponents<HoldState>();

        if (prvMultiHold && !curMultiHold)
            for (int i = 0; i < hsList.Length; ++i)
                hsList[i].CancelCollision();

        prvMultiHold = curMultiHold;
        curMultiHold = hsList.Length > 1;
        curHoldCount = hsList.Length;
    }

    void GetDronePose()
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

        spidar.SetForce(Converter.Convert(f + g), forceK, forceB, Converter.Convert(t), torqueK, torqueB, false, CascadeControl);
    }

    void SetObjectForce()
    {
        if (holdingObject == null) return;

        float timeStep = Time.fixedDeltaTime * Application.targetFrameRate;

        Pose temp = Pose.Lerp(prevPose, pose, timeStep);

        Vector3 force = holdingObject.mass * model.CalcForce(temp, holdingObject); //TODO ここにmassを掛けると物体の重さの違いを感じないのでは？
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

    void RemoveHoldState()
    {
        GameObject obj = holdingObject.gameObject;

        HoldState[] hsList = obj.GetComponents<HoldState>();

        for (int i = 0; i < hsList.Length; ++i)
            if (hsList[i].Owner == this)
                Destroy(hsList[i]);
    }

    void TransmitObject(Rigidbody obj, bool flag)
    {
        const string MatName = "__HapticPointer_Material_transparent";

        if (transmitObject != null)
        {
            Renderer[] renderers = transmitObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.material = stockMaterial[renderer] as Material;
                stockMaterial.Remove(renderer);
            }

            transmitObject = null;
        }

        if (flag)
        {
            Renderer [] renderers = obj.GetComponentsInChildren<Renderer>();
            for(int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (renderer.materials.Length > 0 &&
                    (renderer.material.name.Length < MatName.Length ||
                     renderer.material.name.Substring(0, MatName.Length) != MatName))
                {
                    stockMaterial[renderer] = renderer.material;

                    Material newMaterial = new Material(Resources.Load<Material>("Transmission"));

                    Color orgColor = renderer.material.GetColor("_Color");
                    Color color = new Color(orgColor.r, orgColor.g, orgColor.b, 0.5f);

                    newMaterial.name = MatName;
                    newMaterial.SetColor("_Color", color);

                    renderer.material = newMaterial;

                    transmitObject = obj;
                }
            }
        }
    }

} // end of class HapticPointer.

// end of file.
