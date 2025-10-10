using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class ScrollItem : MonoBehaviour
    {
        [SerializeField] private List<LevelButton> buttons;
        [SerializeField] private List<Image> props;
        [SerializeField] private int itemIndex;
        
        public void UpdateData(int index)
        {
            itemIndex = index;
            
            InitializeContent(index);
        }
        
        private void InitializeContent(int index)
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].UpdateData(index * buttons.Count + i + 1);
            }
        }
        
        private void ResetItem()
        {
            itemIndex = 0;
        }
    
        private void OnDisable()
        {
            ResetItem();
        }
    }
}