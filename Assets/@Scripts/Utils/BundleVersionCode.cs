using TMPro;
using UnityEditor;
using UnityEngine;

namespace Utils
{
    public class BundleVersionCode : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI view;
        
        private string _appVersion;
        private string _bundleVersionCode;
        private string _iOSBuildNumber;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            _appVersion = Application.version;

#if UNITY_ANDROID
            _bundleVersionCode = PlayerSettings.Android.bundleVersionCode.ToString();
#elif UNITY_IOS
            _iOSBuildNumber = PlayerSettings.iOS.buildNumber;
#endif

            view.text = _appVersion + " (" + _bundleVersionCode + _iOSBuildNumber + ")";
        }
    }
}
