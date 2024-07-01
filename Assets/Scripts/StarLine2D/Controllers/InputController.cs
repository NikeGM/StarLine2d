using System.Collections.Generic;
using System.Linq;
using StarLine2D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(MouseInputController))]
    public class InputController : MonoBehaviour
    {
        private MouseInputController mouseInputController;
        private readonly Dictionary<IHoverable, GameObject> _wasHovered = new();
        private bool _initialized = false;

        private void Awake()
        {
            mouseInputController = GetComponent<MouseInputController>();
            _initialized = true;
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            if (!_initialized) return;
            
            mouseInputController.UpdateHits();

            foreach (var pair in mouseInputController.FilterHits<IClickable>())
            {
                if (context.started) pair.Item1.OnStarted(pair.Item2);
                if (context.performed) pair.Item1.OnPerformed(pair.Item2);
                if (context.canceled) pair.Item1.OnCancelled(pair.Item2);
            }
        }

        private void Update()
        {
            var isHovered = mouseInputController.FilterHits<IHoverable>().ToArray();
            foreach (var (go, target) in isHovered)
            {
                var v = _wasHovered.GetValueOrDefault(go, null);
                if (v is not null)
                {
                    go?.OnHovering(target);
                    continue;
                }

                _wasHovered[go] = target;
                go?.OnHoverStarted(target);
            }

            var toNull = new List<IHoverable>();
            foreach (var (go, target) in _wasHovered)
            {
                if (go == null) continue;
                
                var found = isHovered.Any(item => item.Item1 == go);
                if (found) continue;
                
                go?.OnHoverFinished(target);
                toNull.Add(go);
            }

            foreach (var item in toNull)
            {
                _wasHovered.Remove(item);
            }
        }
    }
}