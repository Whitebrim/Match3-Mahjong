using UnityEngine;

namespace Utils
{
    public class LifeTime : MonoBehaviour
    {
        public float life = 0.1f;

        private void Start()
        {
            Destroy(gameObject, life);
        }

    }
}