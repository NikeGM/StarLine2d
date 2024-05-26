using StarLine2D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarLine2D.Controllers
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;

        private IHoverable _hovered;
        
        public void OnClick(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            
            Vector3 mouse = Mouse.current.position.ReadValue();
            if (!Physics.Raycast(mainCamera.ScreenPointToRay(mouse), out var hit)) return;
            var clicked = hit.collider.GetComponent<IClickable>();
            clicked?.OnClick();
        }

        private void Update()
        {
            var wasHovered = _hovered;

            Vector3 mouse = Mouse.current.position.ReadValue();
            _hovered = Physics.Raycast(mainCamera.ScreenPointToRay(mouse), out var hit) ?
                hit.collider.GetComponent<IHoverable>() :
                null;

            if (_hovered == wasHovered) return;

            wasHovered?.OnHoverFinished();
            _hovered?.OnHoverStarted();
        }
    }
}