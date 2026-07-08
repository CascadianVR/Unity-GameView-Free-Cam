#if UNITY_EDITOR && UNITY_EDITOR_WIN

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Cascadian.GameViewFreeCam.Editor
{
    [InitializeOnLoad]
    public static class GameViewFreeCamEditor
    {
        private static bool _isFocused;
        private static float _yaw;
        private static float _pitch; 
        private static Vector2 _lastMousePosition;
        private static Vector3 _lastCamPosition;
        private static Vector3 _velocity;
        private static Quaternion _targetRotation;
        private static GameViewFreeCamWindow _overlayWindow;

        private static readonly Type GameViewType;

        private const float MOVE_MULT = 2.0f;

        static GameViewFreeCamEditor()
        {
            GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            _lastMousePosition = GameViewInputs.CurrentMousePosition;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!GameViewFreeCamSettings.instance.enabled) return;
            
            bool unityActive = InternalEditorUtility.isApplicationActive;
            bool gameFocused = EditorWindow.focusedWindow?.GetType() == GameViewType;
            bool activeAndFocused = unityActive && gameFocused;
            Vector2 currentMousePos = GameViewInputs.CurrentMousePosition;

            // Handle window focus changes
            if (activeAndFocused != _isFocused)
            { 
                _isFocused = activeAndFocused;

                var cam = Camera.main;
                if (cam == null) return;

                Vector3 euler = cam.transform.rotation.eulerAngles;
                _yaw = euler.y;
                _pitch = euler.x;
                if (_pitch > 180f) _pitch -= 360f; // normalize for clamping
 
                _targetRotation = cam.transform.rotation; 
                _lastMousePosition = currentMousePos;
            }


            // Handle mouse + camera
            bool inputUpdated = false;
            if (_isFocused)
            {
                if (GameViewInputs.RightMousePressed())
                {
                    Vector2 mouseWrapDelta = GameViewInputs.WrapMouseInGameView();

                    if (mouseWrapDelta != Vector2.zero)
                        currentMousePos += mouseWrapDelta;
                    
                    inputUpdated = true;
                }

                MoveCamera(inputUpdated);
            }
            
            _lastMousePosition = currentMousePos;
        }

        private static void MoveCamera(bool inputUpdated)
        {
            // Get the currently selected camera
            GameObject selectedObj = Selection.activeGameObject;
            Camera cam = null;
            if (selectedObj != null)
            {
                cam = selectedObj.GetComponent<Camera>();
            }
            
            // If not select camera, get the main camera
            if (cam == null)
            {
                GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
                cam = mainCameras[^1].GetComponent<Camera>();
                if (cam == null) return;
            }

            var settings = GameViewFreeCamSettings.instance;

            // Mouse look
            if (inputUpdated)
            {
                Vector2 mouseDelta = (GameViewInputs.CurrentMousePosition - _lastMousePosition) * settings.lookSensitivity;
                
                _yaw += mouseDelta.x * 0.5f;
                _pitch += mouseDelta.y * 0.5f;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
 
                // Rotation smoothing 
                _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f); 
            } 

            float smooth = 1f - Mathf.Exp(-settings.lookSmooth * Time.deltaTime);

            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, _targetRotation, smooth);

            Vector3 inputMove = Vector3.zero; 
            if (inputUpdated)
            {
                inputMove = GameViewInputs.GetMovementVector();
            }

            Vector3 targetVelocity = cam.transform.TransformDirection(inputMove) * settings.moveSpeed;

            if (GameViewInputs.LeftShiftPressed())
            {
                targetVelocity *= MOVE_MULT;
            }

            _velocity = Vector3.Lerp(
                _velocity,
                targetVelocity,
                1f - Mathf.Exp(-settings.moveSmooth * Time.deltaTime)
            );
            cam.transform.position += _velocity * Time.deltaTime;

            if (_velocity == Vector3.zero && _targetRotation == cam.transform.rotation) return;

            // Mark scene dirty so changes are saved
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(cam);
            }
        }
    }
}

#endif