using UnityEngine;

namespace hakoniwa.objects.core.sensors
{
    public class HakoCamera : MonoBehaviour
    {
        private Camera _camera;
        private IMovableObject targetObject;
        private RenderTexture _renderTexture;
        private Vector3 localPositionOffset;
        private Vector3 localRotationOffset;
        private string encode_type;

        public void ConfigureCamera(string cameraId, string cameraType, string _encode_type, string coordinate_type, string target, Vector3 position, Vector3 rotation, float fov, int width, int height)
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("HakoCamera: Camera component not found!");
            }
            Debug.Log("Camera is found");
            // カメラID（オブジェクト名として設定）
            this.gameObject.name = cameraId;


            // FOV（視野角）の適用
            if (_camera != null)
            {
                _camera.fieldOfView = fov;
            }

            // RenderTextureの作成 & 解像度設定
            if (_camera != null)
            {
                _renderTexture = new RenderTexture(width, height, 24); // 24はDepth Buffer
                _camera.targetTexture = _renderTexture;
            }
            Debug.Log("coordinate_type: " + coordinate_type);
            if (coordinate_type == "local")
            {
                var obj = GameObject.Find(target);
                if (obj == null)
                {
                    throw new System.ArgumentException("Can not find GameObject: " + target);
                }
                targetObject = obj.GetComponentInChildren<IMovableObject>();
                if (targetObject == null)
                {
                    throw new System.ArgumentException("Can not find IMovableObject: " + target);
                }
                localPositionOffset = position;
                localRotationOffset = rotation;
            }
            else //global
            {
                // 座標と回転の適用
                transform.position = position;
                transform.rotation = Quaternion.Euler(rotation);
            }
            this.encode_type = _encode_type;
            Debug.Log($"Configured Camera: {cameraId} - Type: {cameraType}, FOV: {fov}, Position: {position}, Rotation: {rotation}, Resolution: {width}x{height}");
        }
        void LateUpdate()
        {
            if (targetObject != null)
            {
                // **ターゲットオブジェクトの位置を基準にカメラの相対位置を設定**
                var target_rotation = Quaternion.Euler(targetObject.GetEulerDeg());
                transform.position = targetObject.GetPosition() + target_rotation * localPositionOffset;
                transform.rotation = target_rotation * Quaternion.Euler(localRotationOffset);
            }
        }
        public RenderTexture GetRenderTexture()
        {
            return _camera.targetTexture;
        }
        public float GetFov()
        {
            return _camera.fieldOfView;
        }
        public string GetEncodeType()
        {
            return encode_type;
        }

    }
}
