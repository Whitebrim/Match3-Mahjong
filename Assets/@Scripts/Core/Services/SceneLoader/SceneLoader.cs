using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Services.SceneLoader
{
    public static class SceneLoader
    {
        public static async UniTask LoadSceneAsync(string sceneName)
        {
            if (SceneManager.GetActiveScene().name != sceneName)
            {
                await SceneManager.LoadSceneAsync(sceneName);
            }
        }
    }
}