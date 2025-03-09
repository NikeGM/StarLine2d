using System;
using StarLine2D.Libraries.Palette;
using StarLine2D.Libraries.Text;
using TMPro;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Text
{
    [ExecuteInEditMode]
    public class TextWidget : MonoBehaviour
    {
        [SerializeField] [Font] private TMP_FontAsset font;
        [SerializeField] [FontSize] private int size;
        [SerializeField] [Palette] private Color color;
        [SerializeField] private string text;

        private TextMeshProUGUI _tmp;
        private bool _needUpdate = true;

        private void Awake()
        {
            _tmp = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (!_needUpdate) return;
            _needUpdate = false;

            if (_tmp is null) return;

            _tmp.text = text;
            _tmp.font = font;
            _tmp.fontSize = size;
            _tmp.color = color;
        }

        private void OnValidate()
        {
            _tmp = GetComponentInChildren<TextMeshProUGUI>();
            _needUpdate = true;
        }

        public void SetText(string s)
        {
            text = s;
            _needUpdate = true;
        }
    }
}