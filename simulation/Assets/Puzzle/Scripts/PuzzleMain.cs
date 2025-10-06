//
// PuzzleMain.cs
//

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

//
public class PuzzleMain : MonoBehaviour
{
    private float mass;
    private float drag;
    private float angularDrag;

    private GUIStyle style = new GUIStyle();
    private GUIStyleState state = new GUIStyleState();

    //
    void Awake()
    {
        SetObjectPrefs();

        Application.targetFrameRate = 60;

        state.textColor = Color.black;
        style.normal = state;
        style.fontSize = 16;
    }

    void Start()
    {

    }

    void SetObjectPrefs()
    {
        mass = PlayerPrefs.GetFloat("Mass", 0.075f);
        drag = PlayerPrefs.GetFloat("Drag", 2);
        angularDrag = PlayerPrefs.GetFloat("AngularDrag", 2);

        SetObjectPrefs("BoxPiece", mass, drag, angularDrag);
        SetObjectPrefs("LPieceR", mass * 0.75f, drag, angularDrag);
        SetObjectPrefs("LPieceG", mass * 0.75f, drag, angularDrag);
        SetObjectPrefs("LPieceB", mass * 0.75f, drag, angularDrag);
        SetObjectPrefs("LPieceY", mass * 0.75f, drag, angularDrag);
    }

    void SetObjectPrefs(string name, float mass, float drag, float angularDrag)
    {
        GameObject obj = GameObject.Find(name);

        if (!obj) return;

        Rigidbody body = obj.GetComponent<Rigidbody>();

        if (!body) return;

        body.mass = mass;
        body.linearDamping = drag;
        body.angularDamping = angularDrag;
    }

    void Update()
    {
        // 新しい Input System を使用してキーボード入力を取得
        Keyboard keyboard = Keyboard.current;
        
        if (keyboard != null)
        {
            // Q キーでアプリケーション終了
            if (keyboard.qKey.wasPressedThisFrame)
            {
                Application.Quit();
            }

            // R キーでシーンリロード
            if (keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene("Puzzle");
            }

            // N キーで新しいボックスピース作成
            if (keyboard.nKey.wasPressedThisFrame)
            {
                CreateBoxPiece();
            }
        }
    }

    void CreateBoxPiece()
    {
        Vector3 position = Vector3.zero;
        position.y = 0.4f * 10;
        GameObject obj = (GameObject)Instantiate(Resources.Load("MetalPiece"), position, Quaternion.identity);

        if (!obj) return;

        Rigidbody body = obj.GetComponent<Rigidbody>();

        if (!body) return;

        body.mass = mass * 1.5f;
        body.linearDamping = drag;
        body.angularDamping = angularDrag;
    }

} // end of class PuzzleMain.

// end of file.
