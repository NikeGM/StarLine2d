using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StarLine2D.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float zoomSpeed = 1f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private Vector2 panLimitMin;
        [SerializeField] private Vector2 panLimitMax;

        private Vector3 touchStart;
        private InputAction moveAction;
        private InputAction zoomAction;

        private void Awake()
        {
            var playerInput = GetComponent<PlayerInput>();
            var inputActions = playerInput.actions;

            moveAction = inputActions["Move"];
            zoomAction = inputActions["Zoom"];

            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();
        }

        private void OnEnable()
        {
            moveAction.Enable();
            zoomAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            zoomAction.Disable();
        }

        private void Update()
        {
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            {
                HandleTouchInput();
            }
            else
            {
                HandleMouseInput();
                HandleKeyboardInput();
            }

            ClampCameraPosition();
        }

        private void HandleMouseInput()
        {
            float scroll = zoomAction.ReadValue<float>();
            ZoomCamera(scroll);
        }

        private void HandleKeyboardInput()
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            Vector3 direction = new Vector3(move.x, move.y, 0);
            Vector3 newPosition = mainCamera.transform.position + direction * (moveSpeed * Time.deltaTime);
            mainCamera.transform.position = newPosition;
        }

        private void HandleTouchInput()
        {
            var touches = Touch.activeTouches;

            if (touches.Count == 1)
            {
                var touch = touches[0];

                if (touch.phase == TouchPhase.Began)
                {
                    touchStart = mainCamera.ScreenToWorldPoint(touch.screenPosition);
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    Vector3 direction = touchStart - mainCamera.ScreenToWorldPoint(touch.screenPosition);
                    mainCamera.transform.position += direction * (moveSpeed * Time.deltaTime);
                    touchStart =
                        mainCamera.ScreenToWorldPoint(touch
                            .screenPosition); // Обновление touchStart для плавного перемещения
                }
            }

            if (touches.Count == 2)
            {
                var touchZero = touches[0];
                var touchOne = touches[1];

                Vector2 touchZeroPrevPos = touchZero.screenPosition - touchZero.delta;
                Vector2 touchOnePrevPos = touchOne.screenPosition - touchOne.delta;

                float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float currentMagnitude = (touchZero.screenPosition - touchOne.screenPosition).magnitude;

                float difference = currentMagnitude - prevMagnitude;

                ZoomCamera(difference * zoomSpeed * 0.1f);
            }
        }

        private void ZoomCamera(float increment)
        {
            float newSize = mainCamera.orthographicSize - increment;
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }

        private void ClampCameraPosition()
        {
            Vector3 pos = mainCamera.transform.position;
            pos.x = Mathf.Clamp(pos.x, panLimitMin.x, panLimitMax.x);
            pos.y = Mathf.Clamp(pos.y, panLimitMin.y, panLimitMax.y);
            mainCamera.transform.position = pos;
        }
    }
}