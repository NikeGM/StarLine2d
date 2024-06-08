using System;
using StarLine2D.Utils.Disposable;
using StarLine2D.Utils.Observables;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets
{
    public class ProgressBarWidget : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private float defaultValue = 0f;

        private readonly CompositeDisposable _trash = new();

        private float _min = 0;
        private float _max = 1;

        private void Awake()
        {
            SetProgress(defaultValue);
        }
        
        private void SetProgress(float progress)
        {
            fill.fillAmount = progress;
        }

        public void Watch(IntObservableProperty property, int min, int max)
        {
            _min = 1f * min;
            _max = 1f * max;
            
            _trash.Retain(property.SubscribeAndInvoke(OnPropertyChanged));
        }

        private void OnPropertyChanged(int _, int value)
        {
            OnPropertyChanged(1f * _, 1f * value);
        }

        public void Watch(FloatObservableProperty property, float min, float max)
        {
            _min = min;
            _max = max;
            
            _trash.Retain(property.SubscribeAndInvoke(OnPropertyChanged));
        }
        
        private void OnPropertyChanged(float _, float value)
        {
            var percentage = (value - _min) / (_max - _min);
            percentage = Mathf.Clamp01(percentage);
            SetProgress(percentage);
        }

        private void OnDestroy()
        {
            _trash.Dispose();
        }
    }
}