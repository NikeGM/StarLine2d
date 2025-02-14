using System;
using StarLine2D.Components;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets
{
    [RequireComponent(typeof(Image))]
    public class StateImageWidget : DisplayStateComponent
    {
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        protected override void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }
    }
}