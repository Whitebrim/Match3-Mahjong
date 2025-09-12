#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Editor
{
    /// <summary>
    /// Automatically switches to scene 0 when entering play mode, and back to selected scene when exiting play mode
    /// </summary>
    [InitializeOnLoad]
    public class AutoChangeScene
    {
        static AutoChangeScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneUtility.GetScenePathByBuildIndex(0));
        }
    }
}
#endif