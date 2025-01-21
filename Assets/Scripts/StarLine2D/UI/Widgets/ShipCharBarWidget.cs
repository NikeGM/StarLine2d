using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets
{
    [ExecuteInEditMode]
    public class ShipCharBarWidget : MonoBehaviour
    {
        [Header("Attribute Settings")] 
        [SerializeField] private Sprite icon;
        [SerializeField] private string charName;
        [SerializeField] private int value = 50;
        [SerializeField] private int maxValue = 100;

        [Header("Widget Settings")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private ProgressBarCellsWidget bar;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            iconImage.sprite = icon;
            nameText.text = charName;
        }

        private void Update()
        {
            UpdateValue();
        }

        private void UpdateValue()
        {
            valueText.text = $"{value}";
            bar.SetProgress(1f * value / maxValue);
        }

        public void SetValue(int v)
        {
            value = Mathf.Clamp(v, 0, maxValue);
        }

        private void OnValidate()
        {
            Init();
            SetValue(value);
        }
    }
}
