using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(Camera))]
    public class FitCamera : MonoBehaviour
    {
        public float unitsWidth;

        [Sirenix.OdinInspector.Button]
        private void Start()
        {
            var cam = GetComponent<Camera>();
            
            var aspect = (float)Screen.width / Screen.height;
            
            cam.orthographicSize = unitsWidth * 0.5f / aspect;
        }
    }
}
