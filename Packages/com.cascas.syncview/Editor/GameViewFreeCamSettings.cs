#if UNITY_EDITOR

using UnityEditor;

namespace Cascadian.GameCameraFlyCam
{
    [FilePath("ProjectSettings/CasTools.FlyCamSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    // Settings saved per project in Library
    public class GameViewFreeCamSettings : ScriptableSingleton<GameViewFreeCamSettings>
    {
        public float moveSpeed = 1f;
        public float moveSmooth = 20f;
        public float lookSensitivity = 0.3f;
        public float lookSmooth = 25f;
        public bool enabled = true;

        // Call to save changes
        public void SaveSettings()
        {
            Save(true); // true = save immediately to disk
        }

        public void ResetSettings()
        {
            moveSpeed = 1f;
            moveSmooth = 20f;
            lookSensitivity = 0.3f;
            lookSmooth = 25f;
            SaveSettings();
        }
    }
}

#endif