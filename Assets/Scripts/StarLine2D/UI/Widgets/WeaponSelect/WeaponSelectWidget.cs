using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace StarLine2D.UI.Widgets.WeaponSelect
{
    public class WeaponSelectWidget : MonoBehaviour
    {
        [SerializeField] private List<WeaponWidgetItem> items = new();

        private readonly List<WeaponOptionWidget> _options = new();

        private void Awake()
        {
            _options.AddRange(GetComponentsInChildren<WeaponOptionWidget>());
            for (var i = 0; i < Math.Min(items.Count, _options.Count); i++)
            {
                _options[i].Init(items[i]);
            }
        }

        [Serializable]
        public struct WeaponWidgetItem
        {
            [SerializeField] public Sprite iconNormal;
            [SerializeField] public Sprite iconPressed;
            [SerializeField] public Sprite iconDisabled;

            [SerializeField] public int lowValue;
            [SerializeField] public int mediumValue;
            [SerializeField] public int maxValue;
        }
    }
}