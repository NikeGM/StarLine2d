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

        private Image _image;
        private bool _needUpdate = true;

        private void Awake()
        {
            _image = GetComponentInChildren<Image>();
        }

        private void Update()
        {
            if (!_needUpdate) return;
            _needUpdate = false;
            
            if (_image == null) return;

            _image.sprite = icon;
            _image.color = color;
        }

        private void OnValidate()
        {
            _image = GetComponentInChildren<Image>();
            _needUpdate = true;
        }
    }
}