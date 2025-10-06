//
// HapticPointer.cs 
//
using UnityEngine;
using System.Collections;

using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;

//
// class HapticPointerV2
//
public class HapticPointerV2 : MonoBehaviour
{
    public bool     IamLeftHand         = false;
    public bool     TransmitOnCollide   = false;
    public bool     TransmitOnHold      = false;
    public Material HoldingMaterial     = null;
    public Material CollidingMaterial   = null;
    public Material FreeMaterial        = null;
    public Mesh     FistMesh            = null;
    public Mesh     PalmMesh            = null;
    public Spidar   spidar              = null;

    //
    private bool    Haptics             = false;
    private bool    Gravity             = false;
    private float   UnitySpringK        = 0;
    private float   UnityDamperB        = 0;
    private float   PositionScale       = 0;
    private float   DeviceSpringK       = 0;
    private float   DeviceDamperB       = 0;
    private bool    CascadeControl      = false;
    private float   CascadeGain         = 0;
    private bool    ToggleHold          = false;
    private int     HoldChannel         = 0;
    private int     CalibrationChannel  = 0;
    private bool[]  gpioDownState       = new bool[8];
    private bool[]  gpioUpState         = new bool[8];

    //
    private Vector3      PositionOffset { get; set; }
    private Quaternion   RotationOffset { get; set; }
    private uint GpioValue { get { return spidar != null ? spidar.GetGpioValue() : 0; } }

    //
    private SpringDamperModel model = new SpringDamperModel();

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

    private SmoothForce sf1 = new SmoothForce();
    private SmoothForce sf2 = new SmoothForce();


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

        sf1.clear();
        sf2.clear();

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
            spidar.ClearForcePoint(IamLeftHand);

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
    /// デバイスのグリップ姿勢のキャリブレーションを行う．
    /// 安全のため，オブジェクトを掴んでいる場合は離し，クラッチによるオフセット値もクリアする．
    /// </summary> 
    public void Calibrate()
    {
        if (holdingObject != null)
            ReleaseObject();

        if (spidar != null)
            spidar.Calibrate();
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

        if (spidar != null)
            updateSkipCount = 100;

        for (int i = 0; i < 8; ++i)
        {
            gpioDownState[i] = true;
            gpioUpState[i] = true;
        }
    }

    void OnDestroy()
    {
        ReleaseObject();

        spidar = null;
    }

    void Update()
    {
        if (spidar != null && !meshRenderer.enabled)
            meshRenderer.enabled = true;
        else if (spidar == null && meshRenderer.enabled)
            meshRenderer.enabled = false;

        if (updateSkipCount > 0)
        {
            updateSkipCount--;
            return;
        }

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

    }

    void FixedUpdate()
    {
        CheckParentVariables();
        CheckMultiHold();
        GetSpidarPose();
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

    void CheckParentVariables()
    {
        HapticPointerX2v2 hp = transform.parent.GetComponent<HapticPointerX2v2>();
        Haptics = hp.Haptics;
        Gravity = hp.Gravity;
        UnitySpringK = hp.UnitySpringK;
        UnityDamperB = hp.UnityDamperB;
        PositionScale = hp.PositionScale;
        DeviceSpringK = hp.DeviceSpringK;
        DeviceDamperB = hp.DeviceDamperB;
        CascadeControl = hp.CascadeControl;
        CascadeGain = hp.CascadeGain;
        ToggleHold = hp.ToggleHold;
        HoldChannel = IamLeftHand ? 5 : 3;
        CalibrationChannel = IamLeftHand ? 4 : 2;
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

    void GetSpidarPose()
    {
        prevPose = pose;

        SpidarVector pos, vel, avel;
        SpidarQuaternion rot;

        pos = vel = avel = SpidarVector.zero;
        rot = SpidarQuaternion.identity;

        if (spidar != null)
            spidar.GetPosition(IamLeftHand, out pos, out vel);

        rawPose.position = Converter.ScaleUp(Converter.Convert(pos), PositionScale);
        rawPose.velocity = Converter.ScaleUp(Converter.Convert(vel), PositionScale);

        pose.position = RotationOffset * rawPose.position + PositionOffset;
        pose.velocity = RotationOffset * rawPose.velocity;

        transform.position = pose.position;
    }

    void SetSpidarForce()
    {
        if (spidar == null)
            return;

        spidar.SetHaptics(Haptics);
        spidar.SetCascadeGain(CascadeGain);

        if (holdingObject == null) return;

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
                spidar.SetForcePoint(IamLeftHand, Converter.Convert(g), 0, 0, true, false);
            }
            else
            {
                spidar.ClearForcePoint(IamLeftHand, true);
            }
            return;
        }

        float forceScale = DeviceSpringK / (UnitySpringK * PositionScale);
        Vector3 f = -model.CalcForce(pose, holdingObject) * forceScale;
        f = Quaternion.Inverse(RotationOffset) * f;
        float forceK = DeviceSpringK;
        float forceB = DeviceDamperB;

        Vector3 f2 = sf1.get(f);

        spidar.SetForcePoint(IamLeftHand, Converter.Convert(f2 + g), forceK, forceB, false, CascadeControl);
    }

    void SetObjectForce()
    {
        if (holdingObject == null) return;

        float timeStep = Time.fixedDeltaTime * Application.targetFrameRate;
        Pose temp = Pose.Lerp(prevPose, pose, timeStep);

        Vector3 f = holdingObject.mass * model.CalcForce(temp, holdingObject); //TODO ここにmassを掛けると物体の重さの違いを感じないのでは？

        if (curMultiHold)
            f /= (float)curHoldCount;

        Vector3 f2 = sf2.get(f);

        holdingObject.AddForce(f2);
    }

    HoldState GetHoldState()
    {
        GameObject obj = holdingObject.gameObject;

        HoldState[] hsList = obj.GetComponents<HoldState>();

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
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
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

    bool GetGpioDown(int channel)
    {
        if (spidar == null)
            return false;

        if (channel == 0 || channel > spidar.GpioCount)
            return false;

        int checkValue = 1 << (channel - 1);

        if ((GpioValue & checkValue) == 0)
        {
            if (!gpioDownState[channel - 1]) return false;
            gpioDownState[channel - 1] = false;
            return true;
        }
        else
        {
            gpioDownState[channel - 1] = true;
            return false;
        }
    }

    bool GetGpioUp(int channel)
    {
        if (spidar == null)
            return false;

        if (channel == 0 || channel > spidar.GpioCount)
            return false;

        int checkValue = 1 << (channel - 1);

        if ((GpioValue & checkValue) == 0)
        {
            gpioUpState[channel - 1] = true;
            return false;
        }
        else
        {
            if (!gpioUpState[channel - 1]) return false;

            gpioUpState[channel - 1] = false;
            return true;
        }
    }
} // end of class HapticPointer.

// end of file.
