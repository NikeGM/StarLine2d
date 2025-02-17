using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets
{
    [ExecuteInEditMode]
    public class ShipCharOptionWidget : MonoBehaviour
    {
        [Header("Attribute Settings")] 
        [SerializeField] private Sprite icon;
        [SerializeField] private string charName;
        [SerializeField] private string value = "";

        [Header("Widget Settings")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI valueText;

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
        }

        public void SetValue(string v)
        {
            value = v;
        }

        private void OnValidate()
        {
            Init();
            SetValue(value);
        }
    }
}