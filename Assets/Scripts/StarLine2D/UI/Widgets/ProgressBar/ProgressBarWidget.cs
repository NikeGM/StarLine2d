using System;
using StarLine2D.UI.Widgets.Palette;
using StarLine2D.Utils.Disposable;
using StarLine2D.Utils.Observables;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets.ProgressBar
{
    [ExecuteInEditMode]
    public class ProgressBarWidget : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image background;
        
        [SerializeField] [Palette] private Color color;
        [SerializeField] [Palette] private Color fillColor;
        
        [SerializeField] private bool mirrored = false;
        [SerializeField] private bool text = true;
        [SerializeField] private IntObservableProperty property;
        [SerializeField] private int defaultValue = 0;

        private readonly CompositeDisposable _trash = new();
        private Text.TextWidget _textWidget;

        private bool _updated = true;

        private void Awake()
        {
            _textWidget = GetComponentInChildren<Text.TextWidget>();

            property.Value = defaultValue;
            Watch(property);
        }

        private void SetProgress(int p)
        {
            fill.fillAmount = p / 100f;
        }

        public void Watch(IntObservableProperty prop)
        {
            _trash.Dispose();
            _trash.Retain(property.Watch(prop));
            _trash.Retain(prop.SubscribeAndInvoke(OnPropertyChanged));
        }

        private void OnPropertyChanged(int _, int value)
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (clamped != value)
            {
                property.Value = clamped;
                return;
            }

            SetProgress(value);
            if (_textWidget != null) _textWidget.SetText(value + "");
        }
        
        private void OnDestroy()
        {
            _trash.Dispose();
        }

        private void OnValidate()
        {
            property.Validate();
            _updated = true;
        }

        private void Update()
        {
            if (!_updated) return;
            _updated = false;

            if (fill == null) return;
            if (background == null) return;
            
            fill.color = fillColor;
            background.color = color;
            
            var scale = transform.localScale;
            scale.x = mirrored ? -1 : 1;
            transform.localScale = scale;

            if (_textWidget == null) return;
            _textWidget.gameObject.SetActive(text);

            var rotation = Quaternion.Euler(0, mirrored ? -180 : 0, 0);
            _textWidget.transform.rotation = rotation;
        }
    }
}