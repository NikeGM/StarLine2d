using StarLine.Utils.Disposable;
using StarLine.Utils.Observables;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine.UI.Widgets
{
    public class ProgressBarWidget : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private float defaultValue = 0f;

        private readonly CompositeDisposable _trash = new();

        private void Awake()
        {
            SetProgress(defaultValue);
        }
        
        private void SetProgress(float progress)
        {
            fill.fillAmount = progress;
        }

        public void Watch(FloatObservableProperty property)
        {
            _trash.Retain(property.SubscribeAndInvoke(OnPropertyChanged));
        }

        private void OnPropertyChanged(float oldValue, float newValue)
        {
            SetProgress(newValue);
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }
    }
}