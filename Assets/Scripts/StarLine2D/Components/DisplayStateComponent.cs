using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Components
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DisplayStateComponent : MonoBehaviour
    {
        [SerializeField] private List<DisplayStateItem> states = new();

        private SpriteRenderer _spriteRenderer;
        [SerializeField] private string _currentState;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (states.Count == 0) return;

            _currentState = states[0].Name;
            SetState(states[0].Name);
        }

        public bool HasState(string stateName)
        {
            return states.Any(item => item.Name == stateName);
        }
        
        public virtual void SetState(string stateName, float alpha = 1)
        {
            if (!HasState(stateName)) return;
            if (stateName == _currentState) return;
 
            var sprite = states.Find(item => item.Name == stateName).Sprite;
            if (_spriteRenderer is null) _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = sprite;
            _currentState = stateName;
            // SetTransparency(alpha);
        }
        
        public void SetTransparency(float alpha)
        {
            // Убедитесь, что значение альфа находится в диапазоне [0, 1]
            alpha = Mathf.Clamp01(alpha);
    
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
    
            // Получаем текущий цвет спрайта и изменяем альфа-канал
            var color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }

        public virtual string GetCurrentState()
        {
            return _currentState;
        }

        public virtual bool Is(string stateName)
        {
            return GetCurrentState() == stateName;
        }

        [Serializable]
        private class DisplayStateItem
        {
            [SerializeField] private string name;
            [SerializeField] private Sprite sprite;

            public string Name => name;
            public Sprite Sprite => sprite;
        }
    }
}