using UnityEngine;
using UnityEngine.UI;

public class DroneHUD : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform leftStickHandle;  // 左スティックの動く部分
    public RectTransform rightStickHandle; // 右スティックの動く部分
    
    [Header("Settings")]
    public float moveRange = 75f; // ノブが動ける最大半径(ピクセル)
                                  // Baseサイズが200なら、半径100 - Handle半径25 = 75くらいが適正

    void Update()
    {
        // シングルトンインスタンスから入力を取得
        if (HakoDroneSpidarInputManager.Instance == null) return;

        // 1. 右スティック (Roll / Pitch)
        // inputは (-1.0 ～ 1.0) の範囲で返ってくる
        Vector2 rightInput = HakoDroneSpidarInputManager.Instance.GetRightStickInput();
        UpdateStickPosition(rightStickHandle, rightInput);

        // 2. 左スティック (Yaw / Throttle)
        Vector2 leftInput = HakoDroneSpidarInputManager.Instance.GetLeftStickInput();
        UpdateStickPosition(leftStickHandle, leftInput);
    }

    // UIの位置を更新するヘルパー関数
    void UpdateStickPosition(RectTransform handle, Vector2 input)
    {
        if (handle == null) return;

        // 入力値 (-1～1) に 移動範囲 (75px) を掛けて座標にする
        // アンカーが中心にあれば、anchoredPosition (0,0) が中心
        handle.anchoredPosition = input * moveRange;
    }
}