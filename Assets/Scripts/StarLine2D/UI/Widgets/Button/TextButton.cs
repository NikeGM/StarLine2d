using System;
using TMPro;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Button
{
    [RequireComponent(typeof(CustomButton))]
    public class TextButton : MonoBehaviour
    {
        [SerializeField] private string title;

        private bool _updated = false;

        private void Awake()
        {
            UpdateTitle();
        }

        private void Update()
        {
            if (!_updated) return;
            
            UpdateTitle();
        }

        [ContextMenu("Update title")]
        private void UpdateTitle()
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts) t.text = title;
        }

        private void OnValidate()
        {
            _updated = true;
        }
    }
}