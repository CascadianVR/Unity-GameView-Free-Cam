using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Cascadian.GameCameraFlyCam
{
    [InitializeOnLoad]
    public static class GameViewInputs
    {
        private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern int GetCursorPos(ref MousePoint lpPoint);
        
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        
        // Virtual key codes
        private const int VK_W = 0x57;
        private const int VK_A = 0x41;
        private const int VK_S = 0x53;
        private const int VK_D = 0x44;
        private const int VK_Q = 0x51;
        private const int VK_E = 0x45;
        private const int VK_RBUTTON = 0x02;
        private const int VK_LSHIFT = 0xA0;
#endif
        
#if UNITY_EDITOR_LINUX
    //[DllImport("libX11.so.6")]
    //private static extern int XQueryKeymap(IntPtr display, byte[] keys);
//
    //private static bool IsKeyDown(int keycode)
    //{
    //    var keys = new byte[32];
    //    XQueryKeymap(_display, keys);
    //    return (keys[keycode >> 3] & (1 << (keycode & 7))) != 0;
    //}
#endif
        
        private static MousePoint _mousePoint;
        private struct MousePoint
        {
            public int X;
            public int Y;
        }

        public static Vector2 CurrentMousePosition => new Vector2(_mousePoint.X, _mousePoint.Y);

        static GameViewInputs()
        {
            EditorApplication.update += Update;
        }

        private static void GetCursorPosGeneric(ref MousePoint lpPoint)
        {
            #if UNITY_EDITOR_WIN
            GetCursorPos(ref lpPoint);
            #endif
            
            #if UNITY_EDITOR_LINUX
            GetCursorPosLIN(ref lpPoint);
            #endif
        }
        
        private static short GetAsyncKeyStateGeneric(int vKey)
        {
            #if UNITY_EDITOR_WIN
            return GetAsyncKeyState(vKey);
            #endif
            
            #if UNITY_EDITOR_LINUX
            GetAsyncKeyStateLIN(vKey);
            #endif
        }

        private static void SetCursorPosGeneric(int x, int y)
        {
            #if UNITY_EDITOR_WIN
            SetCursorPos(x, y);
            #endif
            
            #if UNITY_EDITOR_LINUX
            SetCursorPosLIN(x, y);
            #endif
        }
        
        private static void Update()
        {
            GetCursorPosGeneric(ref _mousePoint);
        }

        public static bool RightMousePressed()
        {
            return (GetAsyncKeyStateGeneric(VK_RBUTTON) & 0x8000) != 0;
        }

        public static bool LeftShiftPressed()
        {
            return (GetAsyncKeyStateGeneric(VK_LSHIFT) & 0x8000) != 0;
        }

        public static Vector3 GetMovementVector()
        {
            Vector3 dir = Vector3.zero;
            if ((GetAsyncKeyStateGeneric(VK_W) & 0x8000) != 0) dir += Vector3.forward;
            if ((GetAsyncKeyStateGeneric(VK_S) & 0x8000) != 0) dir += Vector3.back;
            if ((GetAsyncKeyStateGeneric(VK_A) & 0x8000) != 0) dir += Vector3.left;
            if ((GetAsyncKeyStateGeneric(VK_D) & 0x8000) != 0) dir += Vector3.right;
            if ((GetAsyncKeyStateGeneric(VK_Q) & 0x8000) != 0) dir += Vector3.down;
            if ((GetAsyncKeyStateGeneric(VK_E) & 0x8000) != 0) dir += Vector3.up;
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

            SetCursorPosGeneric((int)newMousePos.x, (int)newMousePos.y);

            // Get delta mouse position after wrapping
            Vector2 mousePosDelta = newMousePos - mousePos;

            if (!didWrap) return Vector2.zero;
            return mousePosDelta;
        }
    }
    
#if UNITY_EDITOR_LINUX
    public static class X11
    {
        private const string X11_LIB = "libX11.so.6";

        [DllImport(X11_LIB)]
        public static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport(X11_LIB)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(X11_LIB)]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport(X11_LIB)]
        public static extern int XQueryPointer(
            IntPtr display,
            IntPtr window,
            out IntPtr rootReturn,
            out IntPtr childReturn,
            out int rootX,
            out int rootY,
            out int winX,
            out int winY,
            out uint maskReturn
        );

        [DllImport(X11_LIB)]
        public static extern int XWarpPointer(
            IntPtr display,
            IntPtr src,
            IntPtr dest,
            int srcX,
            int srcY,
            uint srcWidth,
            uint srcHeight,
            int destX,
            int destY
        );

        [DllImport(X11_LIB)]
        public static extern int XFlush(IntPtr display);
    }
#endif
}