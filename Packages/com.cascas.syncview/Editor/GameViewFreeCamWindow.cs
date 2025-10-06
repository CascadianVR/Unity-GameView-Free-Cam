#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Cascadian.GameCameraFlyCam
{
    public class GameViewFreeCamWindow : EditorWindow
    {

        [MenuItem("Tools/Cascadian/Game View Free-Cam Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameViewFreeCamWindow>(false);
            window.titleContent = new GUIContent("GameView FlyCam Settings"); // Empty title
            window.Show();
            
            Vector2 size = new Vector2(350, 125);
            window.minSize = size;
            window.maxSize = size;
            Rect windowPos = new Rect(100, 100, size.x, size.y);
            
            // Get game view position
            var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            if (gameView != null)
            {
                var gameViewPos = gameView.position;
                windowPos = new Rect(gameViewPos.x, gameViewPos.y + 40, size.x, size.y);
            }
            
            window.position = windowPos;
        }

        private void OnGUI()
        {
            Handles.BeginGUI();

            var settings = GameViewFreeCamSettings.instance;
            
            settings.moveSpeed = EditorGUILayout.Slider("Move Speed", settings.moveSpeed, 0.01f, 10f);
            settings.moveSmooth = EditorGUILayout.Slider("Move Smooth", settings.moveSmooth, 1f, 50f);
            settings.lookSensitivity = EditorGUILayout.Slider("Look Sensitivity", settings.lookSensitivity, 0.01f, 1f);
            settings.lookSmooth = EditorGUILayout.Slider("Look Smooth", settings.lookSmooth, 1f, 50f);
            settings.enabled = EditorGUILayout.Toggle("Enabled", settings.enabled);

            if (GUILayout.Button("Reset to Defaults"))
            {
                settings.ResetSettings();
            }
            
            if (GUI.changed)
            {
                settings.SaveSettings();
            }

            Handles.EndGUI();
        }
    }
}

#endif