#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    /// <summary>
    /// Automatically switches to scene 0 when entering play mode, and back to selected scene when exiting play mode
    /// </summary>
    [InitializeOnLoad]
    public class AutoChangeScene
    {
        static AutoChangeScene() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void ChangeScene()
        {
            EditorSceneManager.playModeStartScene =
                SceneUtility.GetBuildIndexByScenePath(SceneManager.GetActiveScene().path) >= 0
                    ? AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneUtility.GetScenePathByBuildIndex(0))
                    : null;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) ChangeScene();
        }
    }
}
#endif