using System;
using System.Collections.Generic;
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

        private readonly List<TextMeshProUGUI> _textMeshPros = new();
        private bool _needUpdate = true;

        private void Awake()
        {
            _textMeshPros.AddRange(GetComponentsInChildren<TextMeshProUGUI>(true));
        }

        private void Update()
        {
            if (!_needUpdate) return;
            _needUpdate = false;

            if (_textMeshPros.Count <= 0) return;

            foreach (var item in _textMeshPros)
            {
                item.text = text;
                item.font = font;
                item.fontSize = size;
                item.color = color;
            }
        }

        private void OnValidate()
        {
            _textMeshPros.Clear();
            _textMeshPros.AddRange(GetComponentsInChildren<TextMeshProUGUI>(true));
            _needUpdate = true;
        }

        public void SetText(string s)
        {
            text = s;
            _needUpdate = true;
        }
    }
}