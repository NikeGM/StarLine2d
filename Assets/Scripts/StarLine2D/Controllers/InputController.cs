using System.Collections.Generic;
using System.Linq;
using StarLine2D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(MouseInputController))]
    [RequireComponent(typeof(TouchInputController))]
    public class InputController : MonoBehaviour
    {
        private MouseInputController mouseInputController;
        private TouchInputController touchInputController;
        private readonly Dictionary<IHoverable, GameObject> _wasHovered = new Dictionary<IHoverable, GameObject>();
        private bool _initialized = false;
        
        private void Awake()
        {
            mouseInputController = GetComponent<MouseInputController>();
            touchInputController = GetComponent<TouchInputController>();
            
            if (mouseInputController == null)
            {
                Debug.LogError("MouseInputController not found on InputController.");
            }
            
            if (touchInputController == null)
            {
                Debug.LogError("TouchInputController not found on InputController.");
            }
            
            _initialized = true;
        }

        
        public void OnClick(InputAction.CallbackContext context)
        {
            if (!_initialized)
            {
                Debug.LogWarning("InputController is not initialized yet.");
                return;
            }
            
            Debug.Log("OnClick called with context: " + context);

            if (mouseInputController == null)
            {
                Debug.LogError("MouseInputController is null in InputController.");
                return;
            }

            foreach (var pair in mouseInputController.FilterHits<IClickable>())
            {
                if (context.started)
                {
                    pair.Item1.OnStarted(pair.Item2);
                    Debug.Log("Click Started on: " + pair.Item2.name);
                }
                if (context.performed)
                {
                    pair.Item1.OnPerformed(pair.Item2);
                    Debug.Log("Click Performed on: " + pair.Item2.name);
                }
                if (context.canceled)
                {
                    pair.Item1.OnCancelled(pair.Item2);
                    Debug.Log("Click Cancelled on: " + pair.Item2.name);
                }
            }

            if (touchInputController == null)
            {
                Debug.LogError("TouchInputController is null in InputController.");
                return;
            }

            foreach (var pair in touchInputController.FilterHits<IClickable>())
            {
                if (context.started)
                {
                    pair.Item1.OnStarted(pair.Item2);
                    Debug.Log("Touch Started on: " + pair.Item2.name);
                }
                if (context.performed)
                {
                    pair.Item1.OnPerformed(pair.Item2);
                    Debug.Log("Touch Performed on: " + pair.Item2.name);
                }
                if (context.canceled)
                {
                    pair.Item1.OnCancelled(pair.Item2);
                    Debug.Log("Touch Cancelled on: " + pair.Item2.name);
                }
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                Debug.LogWarning("InputController is not initialized yet.");
                return;
            }

            if (mouseInputController == null)
            {
                Debug.LogError("MouseInputController is null in InputController.");
                return;
            }

            if (touchInputController == null)
            {
                Debug.LogError("TouchInputController is null in InputController.");
                return;
            }

            // Обновляем состояние наведения для мыши
            var isHoveredMouse = mouseInputController.FilterHits<IHoverable>().ToArray();
            foreach (var (go, target) in isHoveredMouse)
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

            var toNullMouse = new List<IHoverable>();
            foreach (var (go, target) in _wasHovered)
            {
                if (go == null) continue;
                
                var found = isHoveredMouse.Any(item => item.Item1 == go);
                if (found) continue;
                
                go?.OnHoverFinished(target);
                toNullMouse.Add(go);
            }

            foreach (var item in toNullMouse)
            {
                _wasHovered.Remove(item);
            }

            // Обновляем состояние наведения для тачскрина
            var isHoveredTouch = touchInputController.FilterHits<IHoverable>().ToArray();
            foreach (var (go, target) in isHoveredTouch)
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

            var toNullTouch = new List<IHoverable>();
            foreach (var (go, target) in _wasHovered)
            {
                if (go == null) continue;
                
                var found = isHoveredTouch.Any(item => item.Item1 == go);
                if (found) continue;
                
                go?.OnHoverFinished(target);
                toNullTouch.Add(go);
            }

            foreach (var item in toNullTouch)
            {
                _wasHovered.Remove(item);
            }
        }
    }
}
