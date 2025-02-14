using System;
using StarLine2D.Utils.Disposable;
using StarLine2D.Utils.Observables;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets.ProgressBar
{
    public class ProgressBarWidget : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private bool mirrored = false;
        [SerializeField] private bool text = true;
        [SerializeField] private IntObservableProperty property;
        [SerializeField] private int defaultValue = 0;

        private readonly CompositeDisposable _trash = new();
        private TextMeshProUGUI _textMesh;

        private bool _updated = true;

        private void Awake()
        {
            _textMesh = GetComponentInChildren<TextMeshProUGUI>();

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
            if (_textMesh != null) _textMesh.text = value + "";
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
            
            var scale = transform.localScale;
            scale.x = mirrored ? -1 : 1;
            transform.localScale = scale;

            if (_textMesh == null) return;
            _textMesh.gameObject.SetActive(text);

            var pivot = _textMesh.rectTransform.pivot;
            pivot.x = mirrored ? 1 : 0;
            _textMesh.rectTransform.pivot = pivot;

            var s = _textMesh.transform.localScale;
            s.x = mirrored ? -1 : 1;
            _textMesh.transform.localScale = s;
        }
    }
}