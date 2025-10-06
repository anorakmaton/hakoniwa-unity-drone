using UnityEngine;
using System.Collections;

using Spidar = TokyoTech.Spidar.Spidar;
using SpidarVector = TokyoTech.Spidar.Vector3;
using SpidarQuaternion = TokyoTech.Spidar.Quaternion;

public class HapticPointerX2v2 : HapticPointerBase
{
    public override bool Initialize()
    {
        bool flag = base.Initialize();

        HapticPointerV2 hpv2L = transform.Find("HapticPointerV2L").gameObject.GetComponent<HapticPointerV2>();
        HapticPointerV2 hpv2R = transform.Find("HapticPointerV2R").gameObject.GetComponent<HapticPointerV2>();
        hpv2L.spidar = spidar;
        hpv2R.spidar = spidar;

        return flag;
    }

    void Start ()
    {
        requestInit = !Initialize();
    }

    void Update()
    {
        if (FixedDeltaTime != Time.fixedDeltaTime)
            Time.fixedDeltaTime = FixedDeltaTime;
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
}
