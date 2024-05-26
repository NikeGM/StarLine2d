using System;
using StarLine2D.Utils;
using UnityEngine;

namespace StarLine2D.Components
{
    public class HoverableDisplayStateComponent : DisplayStateComponent, IHoverable
    {
        private bool _hover = false;
        
        public void OnHoverStarted(GameObject target)
        {
            _hover = true;
            var state = base.GetCurrentState();
            SetState(state);
        }

        public void OnHoverFinished(GameObject target)
        {
            _hover = false;
            var state = base.GetCurrentState();
            SetState(ExtractStateName(state));
        }

        public override string GetCurrentState()
        {
            var state = base.GetCurrentState();
            return ExtractStateName(state);
        }

        public override void SetState(string stateName)
        {
            base.SetState(_hover ? $"{stateName}:hover" : stateName);
        }

        public bool IsHovered()
        {
            return _hover;
        }

        private string ExtractStateName(string s)
        {
            var index = s.IndexOf(":", StringComparison.Ordinal);
            return index >= 0 ? s[..index] : s;
        }
    }
}