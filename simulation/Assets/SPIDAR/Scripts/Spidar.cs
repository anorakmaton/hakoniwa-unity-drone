//
// Spidar.cs
//
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace TokyoTech.Spidar
{
    /// <summary>
    /// 3次元ベクトルクラス．
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public static Vector3 zero
        {
            get { return Vector3.zero_; }
        }

        private static Vector3 zero_;

        static Vector3()
        {
            zero_.x = zero_.y = zero_.z = 0;
        }
    };

    /// <summary>
    /// 四元数クラス．
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static Quaternion identity
        {
            get { return Quaternion.identity_; }
        }

        private static Quaternion identity_;

        static Quaternion()
        {
            identity_.x = 1;
            identity_.y = identity_.z = identity_.w = 0;
        }
    };

    /// <summary>
    /// SPIDARデバイスの管理クラス．
    /// </summary> 
    public class Spidar : IDisposable
    {
        private uint serialNumber = 0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private int[] count;


        /// <summary>
        /// ライブラリのバージョン．
        /// </summary> 
        public static uint LibraryVersion
        {
            get
            {
                uint version = 0;
                Spidar.GetLibraryVersion(out version);
                return version;
            }
        }

        /// <summary>
        /// 接続済みデバイスリスト．
        /// </summary> 
        public static uint [] DeviceList
        {
            get
            {
                uint count;
                Spidar.GetControllerCount(out count);
                uint[] list = new uint[count];
                for (uint i = 0; i < count; ++i)
                    Spidar.GetControllerSerialNumber(i, out list[i]);
                return list;
            }
        }

        /// <summary>
        /// デバイス（SPIDAR基板）固有のID．
        /// </summary> 
        public uint SerialNumber { get { return serialNumber; } }

        /// <summary>
        /// GPIOのチャンネル数．
        /// </summary> 
        public uint GpioCount { get { return 8; } }

        /// <summary>
        /// デバイスを初期化し，Spidarクラスのインスタンスを作成する．
        /// デバイスの初期化に失敗した場合はnullを返す．
        /// </summary> 
        /// <param name="fileName">
        /// SPIDAR設定ファイルの名前
        /// </param>
        /// <returns>
        /// Spidarクラスのインスタンス
        /// </returns> 
        public static Spidar Create(uint serialNumber, int model)
        {
            if (Spidar.Initialize(serialNumber, model))
            {
                return new Spidar(serialNumber);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// デストラクタ．
        /// </summary> 
        ~Spidar()
        {
            this.Dispose();
        }

        /// <summary>
        /// デバイスの終了処理．
        /// </summary> 
        public void Dispose()
        {
            Spidar.Terminate(this.SerialNumber);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// デバイスのグリップ姿勢のキャリブレーションを行う．
        /// </summary> 
        public void Calibrate()
        {
            Spidar.Calibrate(this.SerialNumber);
        }

        /// <summary>
        /// デバイスの更新周期[s]を取得する（実測値）．
        /// </summary> 
        /// <returns>
        /// デバイスの更新周期[s]
        /// </returns>
        public float GetDeltaTime()
        {
            float deltaTime;
            Spidar.GetDeltaTime(this.SerialNumber, out deltaTime);
            return deltaTime;
        }

        /// <summary>
        /// グリップの半径[m]を取得する．
        /// </summary> 
        /// <returns>
        /// グリップの半径[m]
        /// </returns>
        public float GetGripRadius()
        {
            float gripRadius;
            Spidar.GetGripRadius(this.SerialNumber, out gripRadius);
            return gripRadius;
        }

        /// <summary>
        /// GPIOの値を取得する．
        /// </summary> 
        /// <returns>
        /// GPIOの値
        /// </returns>
        public uint GetGpioValue()
        {
            uint value;
            if (!Spidar.GetGpioValue(this.SerialNumber, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// 力覚提示のON/OFFを設定する．
        /// </summary> 
        /// <param name="enable">
        /// true: ON, false: OFF
        /// </param>
        public void SetHaptics(bool enable)
        {
            Spidar.SetHaptics(this.SerialNumber, enable);
        }

        /// <summary>
        /// カスケード制御用バネ定数のゲインを設定する．
        /// </summary> 
        /// <param name="gain">
        /// ゲイン
        /// </param>
        public void SetCascadeGain(float gain)
        {
            Spidar.SetCascadeGain(this.SerialNumber, gain);
        }

        /// <summary>
        /// エンコーダ値を取得する．
        /// </summary>
        /// <param name="count">
        /// エンコーダ値
        /// </param>
        public void GetEncoderCount(ref int[] count)
        {
            Spidar.GetEncoderCount(this.SerialNumber, this.count);

            int max = count.Length > 8 ? 8 : count.Length;

            for (int i = 0; i < max; ++i)
            {
                count[i] = this.count[i];
            }
        }

        /// <summary>
        /// グリップの姿勢を取得する．
        /// </summary> 
        /// <param name="position">
        /// グリップの位置[m]
        /// </param>
        /// <param name="rotation">
        /// グリップの回転（四元数）
        /// </param>
        /// <param name="velocity">
        /// グリップの速度[m/s]
        /// </param>
        /// <param name="angularVelocity">
        /// グリップの角速度[rad/s]
        /// </param>
        public void GetPose(out Vector3 position, out Quaternion rotation,
                            out Vector3 velocity, out Vector3 angularVelocity)
        {
            Spidar.GetPose(this.SerialNumber, out position, out rotation,
                           out velocity, out angularVelocity);
        }

        /// <summary>
        /// グリップの位置を取得する．
        /// </summary> 
        /// <param name="position">
        /// グリップの位置[m]
        /// </param>
        /// <param name="velocity">
        /// グリップの速度[m/s]
        /// </param>
        public void GetPosition(bool ofLeft, out Vector3 position, out Vector3 velocity)
        {
            Spidar.GetPosition(this.SerialNumber, ofLeft, out position, out velocity);
        }

        /// <summary>
        /// 提示力を設定する．
        /// </summary> 
        /// <param name="force">
        /// 出力する併進力[N]（目標値）
        /// </param>
        /// <param name="torque">
        /// 出力する回転力[Nm]（目標値）
        /// </param>
        /// <param name="forceK">
        /// バネ定数 K
        /// </param>
        /// <param name="forceB">
        /// ダンパ係数 B
        /// </param>
        /// <param name="torqueK">
        /// バネ定数 K
        /// </param>
        /// <param name="torqueB">
        /// ダンパ係数 B
        /// </param> 
        /// <param name="lerp">
        /// 提示力の線形補間のON/OFF
        /// </param>
        /// <param name="cascade">
        /// カスケード制御のON/OFF
        /// </param>
        public void SetForce(Vector3 force, float forceK, float forceB,
                             Vector3 torque, float torqueK, float torqueB, bool lerp, bool cascade)
        {
            Spidar.SetForce(this.SerialNumber, ref force, forceK, forceB,
                                           ref torque, torqueK, torqueB,
                                            lerp, cascade);
        }

        /// <summary>
        /// 提示力を設定する．
        /// </summary> 
        /// <param name="ofLeft">
        /// 左側のグリップに対してかどうか.
        /// </param>
        /// <param name="force">
        /// 出力する併進力[N]（目標値）
        /// </param>
        /// <param name="forceK">
        /// バネ定数 K
        /// </param>
        /// <param name="forceB">
        /// ダンパ係数 B
        /// </param>
        /// <param name="lerp">
        /// 提示力の線形補間のON/OFF
        /// </param>
        /// <param name="cascade">
        /// カスケード制御のON/OFF
        /// </param>
        public void SetForcePoint(bool ofLeft, Vector3 force, float forceK, float forceB,
                                  bool lerp, bool cascade)
        {
            Spidar.SetForcePoint(this.SerialNumber, ofLeft, ref force, forceK, forceB,
                                 lerp, cascade);
        }

        /// <summary>
        /// 力提示をやめ最小張力とする．
        /// </summary>
        /// <param name="lerp">
        /// 提示力補間のON/OFF
        /// </param>
        public void ClearForce(bool lerp = false)
        {
            Vector3 zero = Vector3.zero;

            Spidar.SetForce(this.SerialNumber, ref zero, 0, 0, ref zero, 0, 0, lerp, false);
        }

        /// <summary>
        /// 力提示をやめ最小張力とする．
        /// </summary>
        /// <param name="ofLeft">
        /// 左側のグリップに対してかどうか.
        /// </param>
        /// <param name="lerp">
        /// 提示力補間のON/OFF
        /// </param>
        public void ClearForcePoint(bool ofLeft, bool lerp = false)
        {
            Vector3 zero = Vector3.zero;

            Spidar.SetForcePoint(this.SerialNumber, ofLeft, ref zero, 0.0f, 0.0f, lerp, false);
        }

        /// <summary>
        /// デバイスの動作を開始する．
        /// </summary> 
        public void Start()
        {
            Spidar.Start(this.SerialNumber);
        }

        /// <summary>
        /// デバイスの動作を停止する．
        /// </summary> 
        public void Stop()
        {
            Spidar.Stop(this.SerialNumber);
        }

        /// <summary>
        /// ログの取得を開始する．
        /// </summary>
        public void StartLog()
        {
            Spidar.StartLog(this.SerialNumber);
        }

        //
        // 以下private関数
        //
        private Spidar() { }

        private Spidar(uint serialNumber)
        {
            this.serialNumber = serialNumber;

            this.count = new int[8];

            for (int i = 0; i < 8; ++i)
            {
                this.count[i] = i;
            }
        }

        //
        // DllImport
        //
        [DllImport("Spidar", EntryPoint = "SpidarGetLibraryVersion")]
        extern private static bool GetLibraryVersion(out uint version);

        [DllImport("Spidar", EntryPoint = "SpidarGetControllerCount")]
        extern private static bool GetControllerCount(out uint count);

        [DllImport("Spidar", EntryPoint = "SpidarGetControllerSerialNumber")]
        extern private static bool GetControllerSerialNumber(uint controllerIndex, out uint serialNumber);

        [DllImport("Spidar", EntryPoint = "SpidarInitialize")]
        extern private static bool Initialize(uint serialNumber, int model);

        [DllImport("Spidar", EntryPoint = "SpidarTerminate")]
        extern private static bool Terminate(uint serialNumber);

        [DllImport("Spidar", EntryPoint = "SpidarSetHaptics")]
        extern private static bool SetHaptics(uint serialNumber, bool enable);

        [DllImport("Spidar", EntryPoint = "SpidarSetCascadeGain")]
        extern private static bool SetCascadeGain(uint serialNumber, float gain);

        [DllImport("Spidar", EntryPoint = "SpidarStart")]
        extern private static bool Start(uint serialNumber);

        [DllImport("Spidar", EntryPoint = "SpidarStop")]
        extern private static bool Stop(uint serialNumber);

        [DllImport("Spidar", EntryPoint = "SpidarCalibrate")]
        extern private static bool Calibrate(uint serialNumber);

        [DllImport("Spidar", EntryPoint = "SpidarGetPose")]
        extern private static bool GetPose(uint serialNumber, out Vector3 position, out Quaternion rotation,
                                                    out Vector3 velocity, out Vector3 angularVelocity);

        [DllImport("Spidar", EntryPoint = "SpidarGetPosition")]
        extern private static bool GetPosition(uint serialNumber, bool ofLeft, out Vector3 position, out Vector3 velocity);

        [DllImport("Spidar", EntryPoint = "SpidarGetEncoderCount")]
        extern private static bool GetEncoderCount(uint serialNumber, [In, Out] int[] count);

        [DllImport("Spidar", EntryPoint = "SpidarSetForce")]
        extern private static bool SetForce(uint serialNumber, ref Vector3 force, float forceK, float forceB,
                                                     ref Vector3 torque, float torqueK, float torqueB, bool lerp, bool cascade);

        [DllImport("Spidar", EntryPoint = "SpidarSetForcePoint")]
        extern private static bool SetForcePoint(uint serialNumber, bool ofLeft, ref Vector3 force, float forceK, float forceB,
                                                     bool lerp, bool cascade);

        [DllImport("Spidar", EntryPoint = "SpidarGetDeltaTime")]
        extern private static bool GetDeltaTime(uint serialNumber, out float deltaTime);

        [DllImport("Spidar", EntryPoint = "SpidarGetGripRadius")]
        extern private static bool GetGripRadius(uint serialNumber, out float gripRadius);

        [DllImport("Spidar", EntryPoint = "SpidarGetGpioValue")]
        extern private static bool GetGpioValue(uint serialNumber, out uint value);

        [DllImport("Spidar", EntryPoint = "SpidarStartLog")]
        extern private static bool StartLog(uint serialNumber);

    };  // end of class Spidar.

} // end of namespace TokyoTech.Spidar.

// end of file.
