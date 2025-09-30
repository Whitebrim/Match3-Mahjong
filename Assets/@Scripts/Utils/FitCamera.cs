using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(Camera))]
    public class FitCamera : MonoBehaviour
    {
        private const float MinAspectRatio = 9f / 19.5f;
        
        private float _unitsWidth;

        private Camera _camera;
        
        private void Start()
        {
            _camera = GetComponent<Camera>();

            Fit();
        }

        public void Fit(float units)
        {
            _unitsWidth = units;
            Fit();
        }
        
        public void Fit()
        {
            var aspect = Mathf.Min(MinAspectRatio, (float)Screen.width / Screen.height);
            
            _camera.orthographicSize = _unitsWidth * 0.5f / aspect;
        }
    }
}
