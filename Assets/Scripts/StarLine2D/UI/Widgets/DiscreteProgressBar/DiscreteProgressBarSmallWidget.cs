using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets.DiscreteProgressBar
{
    [ExecuteInEditMode]
    public class DiscreteProgressBarSmallWidget : MonoBehaviour
    {
        [SerializeField] private int value = 50;
        [SerializeField] private int maxValue = 100;
        [SerializeField] private Text.TextWidget valueText;
        [SerializeField] private DiscreteProgressBarWidget bar;

        private void Update()
        {
            UpdateValue();
        }

        private void UpdateValue()
        {
            valueText.SetText($"{value}");
            bar.SetProgress(1f * value / maxValue);
        }

        public void SetValue(int v)
        {
            value = Mathf.Clamp(v, 0, maxValue);
        }

        private void OnValidate()
        {
            SetValue(value);
        }
    }
}