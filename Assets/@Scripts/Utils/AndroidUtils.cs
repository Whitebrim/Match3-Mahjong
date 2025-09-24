using UnityEngine;

public static class AndroidUtils
{
    public static int GetBundleVersionCode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject packageManager = context.Call<AndroidJavaObject>("getPackageManager");
                string packageName = context.Call<string>("getPackageName");
                AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);

#if UNITY_28_OR_NEWER // API level 28+ (Android 9 Pie)
                return packageInfo.Get<AndroidJavaObject>("longVersionCode").Call<int>("intValue");
#else
                return packageInfo.Get<int>("versionCode");
#endif
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to get bundle version code: " + e);
            return -1;
        }
#else
        return -1; // Not available in editor
#endif
    }
}