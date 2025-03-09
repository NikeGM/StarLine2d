using System;
using StarLine2D.Libraries.Palette;
using UnityEngine;

namespace StarLine2D.Components
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteColorComponent : MonoBehaviour
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] [Palette] private Color color;

        private bool _updated = true;

        private void LateUpdate()
        {
            if (!_updated) return;
            var r = GetComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
        }

        private void OnValidate()
        {
            _updated = true;
        }
    }
}