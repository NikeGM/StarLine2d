using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets.WeaponSelect
{
    public class WeaponOptionWidget : MonoBehaviour
    {
        [SerializeField] private Image iconNormal;
        [SerializeField] private Image iconPressed;
        [SerializeField] private Image iconDisabled;

        [SerializeField] private TextMeshProUGUI lowText;
        [SerializeField] private TextMeshProUGUI mediumText;
        [SerializeField] private TextMeshProUGUI maxText;
        
        private WeaponSelectWidget.WeaponWidgetItem _data;

        public void Init(WeaponSelectWidget.WeaponWidgetItem value)
        {
            _data = value;

            if (iconNormal != null) iconNormal.sprite = value.iconNormal;
            if (iconPressed != null) iconPressed.sprite = value.iconPressed;
            if (iconDisabled != null) iconDisabled.sprite = value.iconDisabled;

            if (lowText != null) lowText.text = value.lowValue + "";
            if (mediumText != null) mediumText.text = value.mediumValue + "";
            if (maxText != null) maxText.text = value.maxValue + "";
        }
    }
}