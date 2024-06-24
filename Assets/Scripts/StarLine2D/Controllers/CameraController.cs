using UnityEngine;
using UnityEngine.InputSystem;

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
            Debug.Log($"Mouse scroll value: {scroll}");
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
            if (Touchscreen.current.touches.Count == 1)
            {
                var touch = Touchscreen.current.touches[0];

                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    touchStart = mainCamera.ScreenToWorldPoint(touch.position.ReadValue());
                    Debug.Log($"Touch started at: {touch.position.ReadValue()}");
                }

                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector3 direction = touchStart - mainCamera.ScreenToWorldPoint(touch.position.ReadValue());
                    mainCamera.transform.position += direction * (moveSpeed * Time.deltaTime);
                    touchStart = mainCamera.ScreenToWorldPoint(touch.position.ReadValue()); // Update touchStart to current position to ensure smooth movement
                    Debug.Log($"Touch moved. New camera position: {mainCamera.transform.position}");
                }
            }

            if (Touchscreen.current.touches.Count == 2)
            {
                var touchZero = Touchscreen.current.touches[0];
                var touchOne = Touchscreen.current.touches[1];

                Vector2 touchZeroPrevPos = touchZero.position.ReadValue() - touchZero.delta.ReadValue();
                Vector2 touchOnePrevPos = touchOne.position.ReadValue() - touchOne.delta.ReadValue();

                float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float currentMagnitude = (touchZero.position.ReadValue() - touchOne.position.ReadValue()).magnitude;

                float difference = currentMagnitude - prevMagnitude;

                ZoomCamera(difference * zoomSpeed * 0.1f);
                Debug.Log($"Two-finger pinch. Zoom increment: {difference * zoomSpeed * 0.1f}");
            }
        }

        private void ZoomCamera(float increment)
        {
            Debug.Log($"Zoom increment: {increment}");
            float newSize = mainCamera.orthographicSize - increment;
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            Debug.Log($"New orthographic size: {mainCamera.orthographicSize}");
        }

        private void ClampCameraPosition()
        {
            Vector3 pos = mainCamera.transform.position;
            Debug.Log($"Position before clamping: {pos}");
            pos.x = Mathf.Clamp(pos.x, panLimitMin.x, panLimitMax.x);
            pos.y = Mathf.Clamp(pos.y, panLimitMin.y, panLimitMax.y);
            mainCamera.transform.position = pos;
            Debug.Log($"Position after clamping: {mainCamera.transform.position}");
        }
    }
}
