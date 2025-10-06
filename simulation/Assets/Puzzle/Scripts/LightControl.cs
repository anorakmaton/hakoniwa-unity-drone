//
// LightControl.cs
//
using UnityEngine;
using UnityEngine.InputSystem;

public class LightControl : MonoBehaviour
{
    private Light lightComponent;

    void Start()
    {
        lightComponent = gameObject.GetComponent<Light>();
    }

    void Update()
    {
        // 新しい Input System を使用してキーボード入力を取得
        Keyboard keyboard = Keyboard.current;
        
        if (keyboard != null)
        {
            // 上矢印キー、Iキー、または数字の8キーで光の強度を上げる
            if (keyboard.upArrowKey.isPressed || keyboard.iKey.isPressed || keyboard.digit8Key.isPressed)
            {
                if (lightComponent != null)
                {
                    lightComponent.intensity += 0.01f;
                }
            }

            // 下矢印キー、Kキー、または数字の2キーで光の強度を下げる
            if (keyboard.downArrowKey.isPressed || keyboard.kKey.isPressed || keyboard.digit2Key.isPressed)
            {
                if (lightComponent != null)
                {
                    lightComponent.intensity -= 0.01f;
                    // 光の強度が負の値にならないように制限
                    if (lightComponent.intensity < 0)
                    {
                        lightComponent.intensity = 0;
                    }
                }
            }
        }
    }

} // end of class LightControl.

// end of file.
