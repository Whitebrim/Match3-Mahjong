using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TestChangeImage : MonoBehaviour
    {
        [SerializeField] private List<Sprite> sprites;
        [SerializeField] private Image image;

        public void ChangeSprite(int index)
        {
            image.sprite = sprites[index];
        }
    }
}
