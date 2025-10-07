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
abstract public class HapticPointerBase : MonoBehaviour
{
    public enum DevModel
    {
        GZRs_240225,
    }

    public uint SerialNumber = 0;
    public DevModel DeviceModel = DevModel.GZRs_240225;
    public bool Haptics = false;
    public bool DebugMode = false;
    public bool Gravity = false;
    public float UnitySpringK = 0;
    public float UnityDamperB = 0;
    public float PositionScale = 0;
    public float RotationScale = 0;
    public float DeviceSpringK = 0;
    public float DeviceDamperB = 0;
    public bool CascadeControl = false;
    public float CascadeGain = 0;
    public bool ToggleHold = false;
    public bool ToggleClutch = false;
    public int HoldChannel = 0;
    public int ClutchChannel = 0;
    public int CalibrationChannel = 3;
    public static float FixedDeltaTime = 0.001f;

    public bool Activated { get { return spidar != null; } }
    public bool RequestInit { get { bool flag = requestInit; requestInit = false; return flag; } }
    public float DeviceDeltaTime { get { return spidar != null ? spidar.GetDeltaTime() : 0.0f; } }
    public uint GpioValue { get { return spidar != null ? spidar.GetGpioValue() : 0; } }

    protected Spidar spidar = null;
    protected bool requestInit = false;

    protected bool[] gpioDownState = new bool[8];
    protected bool[] gpioUpState = new bool[8];


    /// <summary>
    /// SPIDARの初期化を明示的に実行する．
    /// 初期化に成功した場合はtrueを返す．
    /// </summary> 
    /// <returns>
    /// true: 成功
    /// false: 失敗
    /// </returns> 
    public virtual bool Initialize()
    {
        if (spidar != null)
        {
            spidar.Stop();
            spidar.Dispose();
            spidar = null;
        }
        spidar = Spidar.Create(SerialNumber, (int)DeviceModel);
        if (spidar != null)
        {
            spidar.Start();

            for (int i = 0; i < 8; ++i)
            {
                gpioDownState[i] = true;
                gpioUpState[i] = true;
            }
            return true;
        }
        else
        {
            return SerialNumber == 0;
        }
    }

    /// <summary>
    /// エンコーダ値を取得する．
    /// </summary>
    /// <param name="count">
    /// エンコーダ値を保存する配列の参照
    /// </param>
    public void GetEncoderCount(ref int[] count)
    {
        if (spidar != null)
            spidar.GetEncoderCount(ref count);
    }

    /// <summary>
    /// GPIO値がhigh(1)からlow(0)に変化した際にtrueを返し，それ以外の場合はfalseを返す．
    /// 他にチャンネル番号が基板のGPIO数の範囲にない場合もfalseを返す．
    /// </summary>
    /// <param name="channel">
    /// 取得するGPIOのチャンネル番号
    /// </param>
    /// <returns>
    /// true: GPIO値が1から0に変化した場合
    /// false: それ以外
    /// </returns>  
    public bool GetGpioDown(int channel)
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

    /// <summary>
    /// GPIO値がlow(0)からhigh(1)に変化した際にtrueを返し，それ以外の場合はfalseを返す．
    /// 他にチャンネル番号が基板のGPIO数の範囲にない場合もfalseを返す．
    /// </summary>
    /// <param name="channel">
    /// 取得するGPIOのチャンネル番号
    /// </param>
    /// <returns>
    /// true: GPIO値が0から1に変化した場合
    /// false: それ以外
    /// </returns>   
    public bool GetGpioUp(int channel)
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

}