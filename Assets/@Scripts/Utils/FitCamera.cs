using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(Camera))]
    public class FitCamera : MonoBehaviour
    {
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
            var aspect = Mathf.Min(9f / 16f, (float)Screen.width / Screen.height);
            
            _camera.orthographicSize = _unitsWidth * 0.5f / aspect;
        }
    }
}
