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
        [SerializeField] private long itemIndex;
        
        public void UpdateData(long index)
        {
            itemIndex = index;
            
            InitializeContent(index);
        }
        
        private void InitializeContent(long index)
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].UpdateData(index * buttons.Count + i);
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