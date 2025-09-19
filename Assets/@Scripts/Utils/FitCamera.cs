using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(Camera))]
    public class FitCamera : MonoBehaviour
    {
        public float unitsWidth;

        private Camera _camera;
        
        private void Start()
        {
            _camera = GetComponent<Camera>();

            Fit();
        }

        [Sirenix.OdinInspector.Button]
        public void Fit()
        {
            var aspect = (float)Screen.width / Screen.height;
            
            _camera.orthographicSize = unitsWidth * 0.5f / aspect;
        }
    }
}
