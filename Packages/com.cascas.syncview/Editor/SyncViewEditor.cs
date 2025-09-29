#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace CasTools.GameCameraFollower
{
    [InitializeOnLoad]
    internal class SyncViewEditor : EditorWindow
    {
        private static bool _followCamera = false;
        private static bool _rightMouseDown = false;
        private static float _zoomSpeed = 1.0f;
        private static float _minFov = 10.0f;
        private static float _maxFov = 180.0f;
        private static Camera _mainCamera;

        [MenuItem("Cascadian/▶FollowSceneCamera◀ %t", false, 0)]
        private static void ToggleSceneFollow(MenuCommand menuCommand)
        {
            _followCamera = !_followCamera;
        }

        static SyncViewEditor()
        {
            EditorApplication.update += UpdateCamera;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void UpdateCamera()
        {
            if (!_followCamera) return;

            var sceneCameras = SceneView.GetAllSceneCameras();

            if (sceneCameras.Length <= 0) return;

            Camera camera = null;

            float depth = -100;
            var cams = FindObjectsOfType<Camera>();
            foreach (var cam in cams)
            {
                if (!cam.gameObject.activeSelf) continue;
                if (!(cam.depth > depth)) continue;
                depth = cam.depth;
                camera = cam;
                _mainCamera = cam;
            }

            if (camera == null)
            {
                Debug.LogError("No camera found");
                return;
            }

            camera.transform.SetPositionAndRotation(sceneCameras[0].transform.position,
                sceneCameras[0].transform.rotation);
        }
        
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!_followCamera) return;

            // Track right mouse button state
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1)
                _rightMouseDown = true;
            else if (e.type == EventType.MouseUp && e.button == 1)
                _rightMouseDown = false;
            
            if (e.type == EventType.ScrollWheel && _rightMouseDown)
            {
                if (_mainCamera != null)
                {
                    if (_mainCamera.orthographic)
                    {
                        _mainCamera.orthographicSize += e.delta.y * 0.1f;
                        _mainCamera.orthographicSize = Mathf.Max(0.1f, _mainCamera.orthographicSize);
                    }
                    else
                    {
                        _mainCamera.fieldOfView += e.delta.y * _zoomSpeed;
                        _mainCamera.fieldOfView = Mathf.Clamp(_mainCamera.fieldOfView, _minFov, _maxFov);
                    }

                    e.Use(); // consume the event so it doesn’t also zoom the scene view
                }
            }
        }
    }
}
#endif