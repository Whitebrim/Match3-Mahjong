#if UNITY_EDITOR
using UnityEditor;

namespace Editor
{
    /// <summary>
    /// If `Preferences/Asset Pipeline/Auto Refresh` is Disabled, it recompiles all scripts when Play button is pressed.
    /// </summary>
    [InitializeOnLoad]
    public class AutoScriptReloadWhenTryingToEnterPlayMode
    {
        static AutoScriptReloadWhenTryingToEnterPlayMode()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                AssetDatabase.Refresh();
        }
    }
}
#endif