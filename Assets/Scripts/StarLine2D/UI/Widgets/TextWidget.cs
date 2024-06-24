using StarLine2D.Utils.Disposable;
using StarLine2D.Utils.Observables;
using TMPro;
using UnityEngine;

namespace StarLine2D.UI.Widgets
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextWidget : MonoBehaviour
    {
        [SerializeField] private string label;
        [SerializeField] private int decimals = 0;

        private TextMeshProUGUI _text;

        private readonly CompositeDisposable _trash = new();

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void SetValue(float value)
        {
            var valueString = value.ToString($"f{decimals}");
            var labelString = label.Length > 0 ? $"{label}:" : "";

            _text.text = $"{labelString}{valueString}";
        }
        
        public void Watch(IntObservableProperty property)
        {
            _trash.Retain(property.SubscribeAndInvoke(OnPropertyChanged));
        }

        private void OnPropertyChanged(int _, int value)
        {
            OnPropertyChanged(1f * _, 1f * value);
        }

        public void Watch(FloatObservableProperty property)
        {
            _trash.Retain(property.SubscribeAndInvoke(OnPropertyChanged));
        }
        
        private void OnPropertyChanged(float _, float value)
        {
            SetValue(value);
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }
    }
}