using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System;
using hakoniwa.objects.core;
using hakoniwa.drone;

public class HoverExperimentManager : MonoBehaviour
{
    public enum ExperimentCondition
    {
        PS4_Controller,      // PS4コントローラー (SPIDAR無効)
        SPIDAR_NoForce,      // SPIDAR (力覚なし)
        SPIDAR_WithForce     // SPIDAR (力覚あり)
    }
    // ログの間引き用カウンタ
    private int fixedUpdateCount = 0;

    // [設定] 何回に1回ログを取るか
    // FixedTimestep = 0.001 (1000Hz) の場合:
    // 20回に1回 = 50Hz (0.02秒間隔) -> 一般的な分析にはこれで十分
    // 10回に1回 = 100Hz (0.01秒間隔) -> かなり高精度
    private int logInterval = 20;
    [Header("Experiment Settings")]
    public string subjectName = "Subject_A"; // 被験者名 (ファイル名に使用)
    public ExperimentCondition condition = ExperimentCondition.SPIDAR_WithForce;
    public float taskDuration = 30.0f;       // 計測時間 (秒)
    public float startZoneRadius = 0.5f;     // 開始判定のエリア半径

    [Header("References")]
    public Transform droneTransform;         // ドローン本体
    public Rigidbody droneRigidbody;       // ドローンのRigidbody
    public Transform targetZoneCenter;       // ターゲット(青い箱)の中心
    public Text statusText;                  // 画面表示用テキスト (UI)
    public Text timerText;                   // タイマー表示用テキスト (UI)
    public DroneControl droneControl;        // ドローン制御マネージャー
    private Vector2 currentWind = Vector2.zero; // 現在の風ベクトル
    private Vector3 currentForce = Vector3.zero; // 現在の力ベクトル
    public GameObject HapticWall;
    // 内部状態
    private bool isRunning = false;
    private bool isFinished = false;
    private float currentTime = 0f;
    private StringBuilder logData;
    private string csvFilePath;
    private IDroneInput controller_input;

    void Start()
    {
        // ログ保存先のパス設定 (プロジェクトフォルダ直下)
        string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(Application.dataPath, "../ExperimentLogs");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        
        csvFilePath = Path.Combine(folderPath, $"{subjectName}_{condition}_{date}.csv");

        // 条件に応じた初期設定
        ApplyConditionSettings();

        // ログヘッダー作成
        logData = new StringBuilder();
        logData.AppendLine("Time,PosX,PosY,PosZ,VelX,VelY,VelZ,DistToTarget,InputRightX,InputRightY,InputLeftX,InputLeftY,WindX,WindY,ForceX,ForceY,ForceZ,CollisionCount");

        UpdateUI("Move to Target Zone");

        // ハプティック壁の無効化
        if (HapticWall != null)
        {
            HapticWall.SetActive(false);
        }

        // コントローラー入力の取得
        
    }

    void Update()
    {
        if (isFinished) return;

        // ドローンとターゲットの距離
        float dist = Vector3.Distance(droneTransform.position, targetZoneCenter.position);

        // 1. タスク開始前
        if (!isRunning)
        {
            // ゾーン内に入っているかチェック
            if (dist <= startZoneRadius)
            {
                UpdateUI("Ready! Press 'S' to Start");
                
                // Sキーでスタート (新Input Systemを使用)
                if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
                {
                    StartTask();
                }
            }
            else
            {
                UpdateUI($"Move to Target Zone ({dist:F2}m)");
            }
        }
        // 2. タスク実行中
        else
        {
            //currentTime += Time.deltaTime;
            float remaining = taskDuration - currentTime;
            timerText.text = $"Time: {remaining:F1}";

            if (currentTime >= taskDuration)
            {
                FinishTask();
            }
        }
    }

    // 物理演算のタイミングに合わせてログを取る
    void FixedUpdate()
    {
        if (isRunning && !isFinished)
        {
            currentTime += Time.fixedDeltaTime;
            // ログ記録だけ間引く
            if (fixedUpdateCount % logInterval == 0)
            {
                RecordLog();
            }
            fixedUpdateCount++;
        }
    }

    void StartTask()
    {
        isRunning = true;
        currentTime = 0f;
        UpdateUI("KEEP POSITION!");
        
        // 風を有効化 (DroneControl経由)
        if (droneControl != null)
        {
            droneControl.enableWind = true;
        }

        if (HapticWall != null)
        {
            HapticWall.SetActive(true);
        }

        Debug.Log("Task Started");
    }

    void FinishTask()
    {
        isRunning = false;
        isFinished = true;
        UpdateUI("FINISHED! Data Saved.");
        timerText.text = "Time: 0.0";

        // 風を停止
        if (droneControl != null)
        {
            droneControl.enableWind = false;
        }

        // CSV書き出し
        File.WriteAllText(csvFilePath, logData.ToString());
        Debug.Log($"Log saved to: {csvFilePath}");
    }

    void RecordLog()
    {
        // 現在のドローン位置
        Vector3 pos = droneTransform.position;
        // 現在のドローンの速度
        Vector3 vel = droneRigidbody.linearVelocity;
        // ターゲットからの距離 (RMSE計算用)
        float dist = Vector3.Distance(pos, targetZoneCenter.position);
       
        // 入力値と風ベクトルの取得 (DroneControl経由)
        Vector2 inputRight = Vector2.zero;
        Vector2 inputLeft = Vector2.zero;
        if (droneControl != null)
        {
            var droneInput = droneControl.GetDroneInput();
            if (droneInput != null)
            {
                inputRight = droneInput.GetRightStickInput();
                inputLeft = droneInput.GetLeftStickInput();
                
                // currentForce の取得 (SPIDAR使用時のみ)
                var spidarInput = droneInput as HakoDroneSpidarInputManagerV2;
                if (spidarInput != null)
                {
                    currentForce = spidarInput.currentForce;
                }
            }
            currentWind = droneControl.currentWind;
        }

        // 衝突回数 (必要ならInputManagerにカウンタ変数を追加してここから読む)
        int collision = 0; 

        // CSV行追加
        string line = $"{currentTime},{pos.x},{pos.y},{pos.z},{vel.x},{vel.y},{vel.z},{dist},{inputRight.x},{inputRight.y},{inputLeft.x},{inputLeft.y},{currentWind.x},{currentWind.y},{currentForce.x},{currentForce.y},{currentForce.z},{collision}";
        logData.AppendLine(line);
    }

    void UpdateUI(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // 条件に応じた設定の適用
    void ApplyConditionSettings()
    {
        if (droneControl == null) return;

        switch (condition)
        {
            case ExperimentCondition.PS4_Controller:
                // PS4コントローラーに設定
                droneControl.SetInputType(DroneControlInputType.PS4);
                droneControl.enableWind = false; // スタートまでは風なし
                break;

            case ExperimentCondition.SPIDAR_NoForce:
                // SPIDARに設定して力覚OFF
                droneControl.SetInputType(DroneControlInputType.SPIDARV2);
                droneControl.enableWind = false;
                var droneInputNoForce = droneControl.GetDroneInput() as HakoDroneSpidarInputManagerV2;
                if (droneInputNoForce != null)
                {
                    droneInputNoForce.Haptics = false;    // 力覚OFF
                }
                break;

            case ExperimentCondition.SPIDAR_WithForce:
                // SPIDARに設定して力覚ON
                droneControl.SetInputType(DroneControlInputType.SPIDARV2);
                droneControl.enableWind = false;
                var droneInputWithForce = droneControl.GetDroneInput() as HakoDroneSpidarInputManagerV2;
                if (droneInputWithForce != null)
                {
                    droneInputWithForce.Haptics = true;     // 力覚ON
                }
                break;
        }
    }
}