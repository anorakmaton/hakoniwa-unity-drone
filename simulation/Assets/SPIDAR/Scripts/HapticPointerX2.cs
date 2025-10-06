using UnityEngine;
using System.Collections;

using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;

public class HapticPointerX2 : HapticPointerBase
{
    public Vector3 PositionOffset { get; set; }
    public Quaternion RotationOffset { get; set; }

    private Rigidbody point1 = null;
    private Rigidbody point2 = null;
    private SpringDamperModel model1 = new SpringDamperModel();
    private SpringDamperModel model2 = new SpringDamperModel();

    private Pose pose1;
    private Pose rawPose1;

    private Pose pose2;
    private Pose rawPose2;

    private bool clutchEngaged = true;
    private Vector3 clutchedPositionOffset = Vector3.zero;
    private Vector3 clutchedPosition = Vector3.zero;

    private int preroll = 200;
    private bool moveRigidbody = false;

    public void Calibrate()
    {
        clutchEngaged = false;

        if (spidar != null)
            spidar.Calibrate();

        clutchedPositionOffset = Vector3.zero;
        clutchedPosition = Vector3.zero;
        clutchEngaged = true;
    }

    void Start ()
    {
        PositionOffset = transform.position;
        RotationOffset = transform.rotation;

        point1 = transform.Find("Point1").gameObject.GetComponent<Rigidbody>();
        point2 = transform.Find("Point2").gameObject.GetComponent<Rigidbody>();

        model1.Clear();
        model1.SpringK = UnitySpringK;
        model1.DamperB = UnityDamperB;

        model2.Clear();
        model2.SpringK = UnitySpringK;
        model2.DamperB = UnityDamperB;

        requestInit = !Initialize();

        preroll = 1000;
        moveRigidbody = true;
    }

    void OnDestroy()
    {
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
        if (FixedDeltaTime != Time.fixedDeltaTime)
            Time.fixedDeltaTime = FixedDeltaTime;

        if (GetGpioDown(CalibrationChannel))
            Calibrate();
    }

    void FixedUpdate ()
    {
        GetSpidarPose();

        if (preroll > 0)
        {
            --preroll;
            return;
        }

        SetForce();
    }

    private void GetSpidarPose()
    {
        SpidarVector pos1, vel1;
        SpidarVector pos2, vel2;

        pos1 = vel1 = SpidarVector.zero;
        pos2 = vel2 = SpidarVector.zero;

        if (spidar != null)
        {
            spidar.GetPosition(true, out pos1, out vel1);
            spidar.GetPosition(false, out pos2, out vel2);
        }

        rawPose1.position = Converter.ScaleUp(Converter.Convert(pos1), PositionScale);
        rawPose1.velocity = Converter.ScaleUp(Converter.Convert(vel1), PositionScale);
        rawPose2.position = Converter.ScaleUp(Converter.Convert(pos2), PositionScale);
        rawPose2.velocity = Converter.ScaleUp(Converter.Convert(vel2), PositionScale);

        if (clutchEngaged)
        {
            pose1.position = RotationOffset * (rawPose1.position + clutchedPositionOffset) + PositionOffset;
            pose1.velocity = RotationOffset * rawPose1.velocity;
            pose2.position = RotationOffset * (rawPose2.position + clutchedPositionOffset) + PositionOffset;
            pose2.velocity = RotationOffset * rawPose2.velocity;
        }
        else
        {
            pose1.position = RotationOffset * clutchedPosition + PositionOffset;
            pose1.velocity = Vector3.zero;
            pose2.position = RotationOffset * clutchedPosition + PositionOffset;
            pose2.velocity = Vector3.zero;
        }
    }

    private void SetForce()
    {
        model1.SpringK = UnitySpringK;
        model1.DamperB = UnityDamperB;
        model2.SpringK = UnitySpringK;
        model2.DamperB = UnityDamperB;

        if (moveRigidbody)
        {
            moveRigidbody = false;
            point1.position = pose1.position;
            point2.position = pose2.position;
            point1.linearVelocity = Vector3.zero;
            point2.linearVelocity = Vector3.zero;
        }

        Vector3 force1 = model1.CalcForce(pose1, point1);
        Vector3 force2 = model2.CalcForce(pose2, point2);

        float forceScale = DeviceSpringK / (UnitySpringK * PositionScale);
        forceScale *= 0.4f;

        Vector3 f1 = -force1 * forceScale;
        Vector3 f2 = -force2 * forceScale;

        if (spidar != null)
        {
            spidar.SetHaptics(Haptics);
            spidar.SetForcePoint(true, Converter.Convert(f1), 0.0f, 0.0f, false, false);
            spidar.SetForcePoint(false, Converter.Convert(f2), 0.0f, 0.0f, false, false);
        }

        point1.AddForce(point1.mass * force1);
        point2.AddForce(point2.mass * force2);
    }
}
