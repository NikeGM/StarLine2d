using System.Collections.Generic;
using StarLine2D.Libraries.Palette;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets.Icon
{
    [ExecuteInEditMode]
    public class IconWidget : MonoBehaviour
    {
        [SerializeField] [Libraries.Icon.Icon] private Sprite icon;
        [SerializeField] [Palette] private Color color;

        private readonly List<Image> _images = new();
        private bool _needUpdate = true;

        private void Awake()
        {
            _images.AddRange(GetComponentsInChildren<Image>(true));
        }

        private void Update()
        {
            if (!_needUpdate) return;
            _needUpdate = false;
            
            if (_images.Count <= 0) return;

            foreach (var item in _images)
            {
                item.sprite = icon;
                item.color = color;
            }
        }

        private void OnValidate()
        {
            _images.Clear();
            _images.AddRange(GetComponentsInChildren<Image>(true));
            _needUpdate = true;
        }
    }
}