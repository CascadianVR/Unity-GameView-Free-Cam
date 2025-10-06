#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEditorInternal;

namespace Cascadian.GameCameraFlyCam
{
    [InitializeOnLoad]
    public static class GameViewFlyCamEditor
    {
        private static bool _isFocused;
        private static float _yaw;
        private static float _pitch; 
        private static Vector2 _lastMousePosition;
        private static Vector3 _lastCamPosition;
        private static Vector3 _velocity;
        private static Quaternion _targetRotation;
        private static GameViewFlyCamWindow _overlayWindow;

        private static readonly Type GameViewType;

        private const float MOVE_MULT = 2.0f;

        static GameViewFlyCamEditor()
        {
            GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            _lastMousePosition = GameViewInputs.CurrentMousePosition;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!GameViewFlyCamSettings.instance.enabled) return;
            
            bool unityActive = InternalEditorUtility.isApplicationActive;
            bool gameFocused = EditorWindow.focusedWindow?.GetType() == GameViewType;
            bool activeAndFocused = unityActive && gameFocused;
            Vector2 currentMousePos = GameViewInputs.CurrentMousePosition;

            // Handle window focus changes
            if (activeAndFocused != _isFocused)
            { 
                _isFocused = activeAndFocused;
                Cursor.visible = !_isFocused;

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
            var cam = Camera.main;
            if (cam == null) return;

            var settings = GameViewFlyCamSettings.instance;

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

            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, _targetRotation,
                settings.lookSmooth * Time.deltaTime);

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

            float t = Mathf.Clamp(settings.moveSmooth * Time.deltaTime, 0f, 0.03f);
            _velocity = Vector3.Lerp(_velocity, targetVelocity, t);
            cam.transform.position += _velocity * Time.deltaTime;

            if (_velocity == Vector3.zero && _targetRotation == cam.transform.rotation) return;

            // Mark scene dirty so changes are saved
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(cam);
            }
        }
    }

    [InitializeOnLoad]
    public static class GameViewInputs
    {
        private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

#if UNITY_EDITOR_WIN
        private static MousePoint _mousePoint;

        private struct MousePoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern int GetCursorPos(ref MousePoint lpPoint);

        public static Vector2 CurrentMousePosition => new Vector2(_mousePoint.X, _mousePoint.Y);
#elif UNITY_EDITOR_OSX
        private static CGPoint _mousePoint;
        private struct CGPoint{
            public double X { get; set; }
            public double Y { get; set; }
        }

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventCreate(IntPtr source);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern CGPoint CGEventGetLocation(IntPtr theEvent);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CFRelease(IntPtr obj);
     
        public static Vector2 CurrentMousePosition => new Vector2((float)_mousePoint.X, (float)_mousePoint.Y);
#endif

        static GameViewInputs()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
#if UNITY_EDITOR_WIN
            GetCursorPos(ref _mousePoint);
#elif UNITY_EDITOR_OSX
            var theEvent = CGEventCreate(IntPtr.Zero);
            var point = CGEventGetLocation(theEvent);
            CFRelease(theEvent);
#endif
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_W = 0x57;
        private const int VK_A = 0x41;
        private const int VK_S = 0x53;
        private const int VK_D = 0x44;
        private const int VK_Q = 0x51;
        private const int VK_E = 0x45;
        private const int VK_RBUTTON = 0x02;
        private const int VK_LSHIFT = 0xA0;

        // Check if RMB is down
        public static bool RightMousePressed()
        {
            return (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
        }

        public static bool LeftShiftPressed()
        {
            return (GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0;
        }

        public static Vector3 GetMovementVector()
        {
            Vector3 dir = Vector3.zero;
            if ((GetAsyncKeyState(VK_W) & 0x8000) != 0) dir += Vector3.forward;
            if ((GetAsyncKeyState(VK_S) & 0x8000) != 0) dir += Vector3.back;
            if ((GetAsyncKeyState(VK_A) & 0x8000) != 0) dir += Vector3.left;
            if ((GetAsyncKeyState(VK_D) & 0x8000) != 0) dir += Vector3.right;
            if ((GetAsyncKeyState(VK_Q) & 0x8000) != 0) dir += Vector3.down;
            if ((GetAsyncKeyState(VK_E) & 0x8000) != 0) dir += Vector3.up;
            return dir.normalized;
        }

        public static Vector2 WrapMouseInGameView()
        {
            // Find the GameView
            EditorWindow gameView = null;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType() == GameViewType)
                {
                    gameView = window;
                    break;
                }
            }

            if (gameView == null) return Vector2.zero;

            // Get GameView rect in screen coordinates
            Vector2 pos = gameView.position.position; // top-left
            Vector2 size = gameView.position.size;
            float left = pos.x;
            float top = pos.y;
            float right = pos.x + size.x;
            float bottom = pos.y + size.y;

            // Get current mouse position in screen space (top-left origin)
            Vector2 mousePos = CurrentMousePosition;
            Vector2 newMousePos = mousePos;

            bool didWrap = false;

            // Horizontal wrap
            if (newMousePos.x < left)
            {
                newMousePos.x = right - 1;
                didWrap = true;
            }
            else if (newMousePos.x > right)
            {
                newMousePos.x = left + 1;
                didWrap = true;
            }

            // Vertical wrap
            if (newMousePos.y < top)
            {
                newMousePos.y = bottom - 1;
                didWrap = true;
            }
            else if (newMousePos.y > bottom)
            {
                newMousePos.y = top + 1;
                didWrap = true;
            }

            SetCursorPos((int)newMousePos.x, (int)newMousePos.y);

            // Get delta mouse position after wrapping
            Vector2 mousePosDelta = newMousePos - mousePos;

            if (!didWrap) return Vector2.zero;
            return mousePosDelta;
        }
    }
}

#endif