using UnityEngine;
using Utils.Extensions;

namespace UI
{
    public class CanvasRoot : MonoBehaviour
    {
        public static RectTransform Root;
        private void Awake()
        {
            Root = transform as RectTransform;
        }

        public static void Clear()
        {
            Root.DestroyAllChildren();
        }
    }
}
