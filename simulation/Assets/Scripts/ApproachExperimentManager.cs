using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;
using System.Collections;
using System;
using hakoniwa.objects.core;
using hakoniwa.drone;

public class ApproachExperimentManager : MonoBehaviour
{
    public enum ExperimentCondition
    {
        PS4_Controller,      // PS4コントローラー
        SPIDAR_NoForce,      // SPIDAR (力覚なし)
        SPIDAR_WithForce     // SPIDAR (力覚あり = 予測的提示)
    }

    private int fixedUpdateCount = 0;
    [Tooltip("ログ記録間隔: 1kHz環境なら20で50Hz記録")]
    public int logInterval = 20;

    [Header("Experiment Settings")]
    public string subjectName = "Subject_A";
    public ExperimentCondition condition = ExperimentCondition.SPIDAR_WithForce;
    
    [Header("Task 2 Settings")]
    [Tooltip("壁までのランダムなスタート距離の最小値 (m)")]
    public float minStartDist = 4.0f;
    [Tooltip("壁までのランダムなスタート距離の最大値 (m)")]
    public float maxStartDist = 6.0f;
    // public float collisionThreshold = 0.05f; // DroneCollisionを使うため削除
    [Tooltip("タスク開始とみなす最低高度 (m)")]
    public float taskStartAltitude = 0.5f;

    [Header("References")]
    public Transform droneTransform;
    public Rigidbody droneRigidbody;
    public Transform wallObject;             // リセット時に位置を動かす対象
    public Text statusText;
    public Text resultText;                  // 結果表示用
    public DroneControl droneControl;
    
    // ★追加: 衝突検知スクリプトへの参照
    public DroneCollision droneCollision;

    [Header("Hakoniwa UI Controls")]
    public Button hakoStartButton;
    public Button hakoStopButton;
    public Button hakoResetButton;
    
    public GameObject predictivePointerObj; 

    // 内部変数
    private Vector2 currentWind = Vector2.zero;
    private Vector3 currentForce = Vector3.zero;
    
    private bool isRunning = false;      // タスク（ログ記録）中か
    private bool isFinished = false;     // 今回の試行が終わったか
    private bool isTakingOff = false;    // 離陸中

    private float currentTime = 0f;
    private StringBuilder currentTrialLog;
    private string csvFilePath;
    private int trialCount = 1;

    // タスク評価用
    private float initialDistance = 0f;
    private float currentDistanceToWall = 0f;
    private float closestDistance = 999f;
    private bool hasCollided = false;

    private Vector3 droneOriginPos;

    void Start()
    {
        string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = Path.Combine(Application.dataPath, "../ExperimentLogs_Task2");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        
        csvFilePath = Path.Combine(folderPath, $"{subjectName}_{condition}_{date}.csv");

        string header = "TrialNum,Time,PosX,PosY,PosZ,VelX,VelY,VelZ,DistToWall,ClosestDist,InputRightX,InputRightY,InputLeftX,InputLeftY,ForceX,ForceY,ForceZ,IsCollided";
        File.WriteAllText(csvFilePath, header + "\n");

        ApplyConditionSettings();
        
        if (droneTransform != null)
        {
            droneOriginPos = droneTransform.position;
        }

        // イベント購読 (Startで一度行えばOK)
        if (droneCollision != null)
        {
            droneCollision.OnCollisionEnterAction += OnDroneCollision;
        }
        else
        {
            Debug.LogWarning("⚠️ DroneCollision reference is missing in ExperimentManager!");
        }

        StartCoroutine(ResetAndPrepareCoroutine());
    }

    void OnDestroy()
    {
        // イベント購読解除 (メモリリーク防止)
        if (droneCollision != null)
        {
            droneCollision.OnCollisionEnterAction -= OnDroneCollision;
        }
    }

    // ★ 衝突時のコールバック
    void OnDroneCollision(Collider other)
    {
        if (isRunning && !isFinished)
        {
            // 必要に応じて「壁」かどうかタグなどでチェックしても良いが
            // DroneCollision側でLayerMaskフィルタリングされている前提で進める
            Debug.Log($"💥 Collision Detected with {other.name} via DroneCollision script!");
            hasCollided = true;
            closestDistance = 0f; // 接触したので距離ゼロ扱い
            FinishTask();
        }
    }

    void Update()
    {
        if (wallObject == null || droneTransform == null) return;

        // 壁の表面までの距離を計算 (表示・ログ用)
        float wallThickness = wallObject.lossyScale.z;
        float wallSurfaceZ = wallObject.position.z - (wallThickness * 0.5f);
        currentDistanceToWall = wallSurfaceZ - droneTransform.position.z;

        if (isRunning)
        {
            currentTime += Time.deltaTime;
            UpdateUI($"Trial {trialCount}\nGo to Wall!\n(Distance Hidden)\nPress 'SPACE' to Stop");

            if (currentDistanceToWall < closestDistance)
            {
                closestDistance = currentDistanceToWall;
            }

            // 終了判定 A: 衝突 (イベントコールバック OnDroneCollision で処理するためここでは削除)
            
            // 終了判定 B: ユーザーによる停止宣言 (Spaceキー)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                FinishTask();
            }
        }
        else if (isTakingOff)
        {
            float altitude = droneTransform.position.y;
            UpdateUI($"Trial {trialCount}: Takeoff Phase\nArm & Fly Up to Start\nAlt: {altitude:F2}m / {taskStartAltitude}m\n(Press 'S' when ready)");

            if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
            {
                StartTask();
            }
        }
        else if (isFinished)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                trialCount++;
                StartCoroutine(ResetAndPrepareCoroutine());
            }
        }
    }

    // ... (以降のメソッドは変更なし。ResetAndPrepareCoroutine, FinishTask等) ...
    // ... 省略せずに実装する場合は以前のコードをそのまま利用してください ...

    void FixedUpdate()
    {
        if (isRunning)
        {
            if (fixedUpdateCount % logInterval == 0)
            {
                RecordLog();
            }
            fixedUpdateCount++;
        }
    }

    bool CheckButtonText(Button btn, string expectedText)
    {
        if (btn == null) return false;
        var t = btn.GetComponentInChildren<Text>();
        return t != null && t.text == expectedText;
    }

    IEnumerator ResetAndPrepareCoroutine()
    {
        isRunning = false;
        isFinished = false;
        isTakingOff = false;
        
        UpdateUI("Resetting Simulation...");

        if (CheckButtonText(hakoStopButton, "STOP"))
        {
            hakoStopButton.onClick.Invoke();
            yield return new WaitForSeconds(0.5f);
        }

        if (CheckButtonText(hakoResetButton, "RESET"))
        {
            hakoResetButton.onClick.Invoke();
            yield return new WaitForSeconds(1.2f);
        }

        float randomDist = UnityEngine.Random.Range(minStartDist, maxStartDist);
        initialDistance = randomDist;
        closestDistance = initialDistance;
        hasCollided = false; // フラグリセット
        currentTime = 0f;
        currentTrialLog = new StringBuilder();
        
        if (resultText != null) resultText.text = "";

        if (wallObject != null)
        {
            float wallThickness = wallObject.lossyScale.z;
            Vector3 newWallPos = wallObject.position;
            newWallPos.z = droneOriginPos.z + randomDist + (wallThickness * 0.5f); 
            wallObject.position = newWallPos;
        }

        UpdateUI($"Trial {trialCount} Ready.\nTarget: Close to Wall\nPress 'S' to Start Simulation");

        yield return new WaitUntil(() => Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame);

        if (CheckButtonText(hakoStartButton, "START"))
        {
            hakoStartButton.onClick.Invoke();
        }
        
        // 離陸フェーズへ
        if (droneControl != null)
        {
            droneControl.restrictHorizontalControl = true;
        }
        isTakingOff = true;
        
        yield return new WaitForSeconds(0.5f);
    }

    void StartTask()
    {
        isTakingOff = false; 
        isRunning = true;    
        if (droneControl != null)
        {
            droneControl.enableWind = false;
            droneControl.restrictHorizontalControl = false;
        }
        Debug.Log($"Trial {trialCount} Task Started (Recording)");
    }

    void FinishTask()
    {
        isRunning = false;
        isFinished = true;

        if (CheckButtonText(hakoStopButton, "STOP"))
        {
            hakoStopButton.onClick.Invoke();
        }

        string resultMsg = hasCollided ? "COLLISION! (Failed)" : $"STOPPED. Closest: {closestDistance:F3}m";
        resultMsg += "\nPress 'R' for Next Trial";
        
        UpdateUI(resultMsg);
        if(resultText != null) resultText.text = resultMsg;

        File.AppendAllText(csvFilePath, currentTrialLog.ToString());
        Debug.Log($"Trial {trialCount} data appended.");
    }

    void RecordLog()
    {
        Vector3 pos = droneTransform.position;
        Vector3 vel = (droneRigidbody != null) ? droneRigidbody.linearVelocity : Vector3.zero;
        
        Vector2 inputRight = Vector2.zero;
        Vector2 inputLeft = Vector2.zero;
        
        if (droneControl != null)
        {
            var droneInput = droneControl.GetDroneInput();
            if (droneInput != null)
            {
                inputRight = droneInput.GetRightStickInput();
                inputLeft = droneInput.GetLeftStickInput();
                
                var spidarInput = droneInput as HakoDroneSpidarInputManagerV2;
                if (spidarInput != null)
                {
                    currentForce = spidarInput.currentForce;
                }
            }
        }

        int collidedFlag = hasCollided ? 1 : 0;

        string line = $"{trialCount},{currentTime},{pos.x},{pos.y},{pos.z},{vel.x},{vel.y},{vel.z},{currentDistanceToWall},{closestDistance},{inputRight.x},{inputRight.y},{inputLeft.x},{inputLeft.y},{currentForce.x},{currentForce.y},{currentForce.z},{collidedFlag}";
        currentTrialLog.AppendLine(line);
    }

    void UpdateUI(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    void ApplyConditionSettings()
    {
        if (droneControl == null) return;

        droneControl.enableWind = false;

        switch (condition)
        {
            case ExperimentCondition.PS4_Controller:
                droneControl.SetInputType(DroneControlInputType.PS4);
                HakoDroneSpidarInputManagerV2.Instance.SetControlActive(false); // PS4時はSPIDAR無効
                if(predictivePointerObj != null) predictivePointerObj.SetActive(false);
                break;

            case ExperimentCondition.SPIDAR_NoForce:
                droneControl.SetInputType(DroneControlInputType.SPIDARV2);
                HakoDroneSpidarInputManagerV2.Instance.SetControlActive(true); // 位置入力は必要なのでActive
                SetSpidarHaptics(false); // 力覚だけOFF
                if(predictivePointerObj != null) predictivePointerObj.SetActive(true); 
                break;

            case ExperimentCondition.SPIDAR_WithForce:
                droneControl.SetInputType(DroneControlInputType.SPIDARV2);
                HakoDroneSpidarInputManagerV2.Instance.SetControlActive(true);
                SetSpidarHaptics(true);
                if(predictivePointerObj != null) predictivePointerObj.SetActive(true);
                break;
        }
    }

    void SetSpidarHaptics(bool enable)
    {
        var spidarInput = droneControl.GetDroneInput() as HakoDroneSpidarInputManagerV2;
        if (spidarInput != null)
        {
            spidarInput.Haptics = enable;
        }
    }
    
    void OnDrawGizmos()
    {
        if (wallObject != null && droneTransform != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 dronePos = Application.isPlaying ? droneOriginPos : droneTransform.position;
            
            float wallHalfThick = wallObject.lossyScale.z * 0.5f;
            Gizmos.DrawWireCube(new Vector3(dronePos.x, dronePos.y, dronePos.z + minStartDist), Vector3.one * 0.2f);
            Gizmos.DrawWireCube(new Vector3(dronePos.x, dronePos.y, dronePos.z + maxStartDist), Vector3.one * 0.2f);
            Gizmos.DrawLine(new Vector3(dronePos.x, dronePos.y, dronePos.z + minStartDist), 
                            new Vector3(dronePos.x, dronePos.y, dronePos.z + maxStartDist));
        }
    }
}