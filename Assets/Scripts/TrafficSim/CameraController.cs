using UnityEngine;
using UnityEngine.InputSystem;

namespace TrafficSim
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Keyboard Panning")]
        [Tooltip("Base speed of keyboard panning.")]
        [SerializeField] private float panSpeed = 15f;

        [Header("Mouse Dragging")]
        [Tooltip("Button used for drag panning (1 = Right Click, 2 = Middle Click)")]
        [SerializeField] private int dragButton = 1;

        [Header("Zoom Settings")]
        [Tooltip("Speed of the mouse wheel zoom.")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 25f;

        [Header("Map Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

        private Camera _cam;
        private Vector3 _dragOrigin;
        private bool _isDragging;

        private InputAction _moveAction;
        private InputAction _dragAction;
        private InputAction _scrollAction;
        private InputAction _mousePositionAction;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (!_cam.orthographic)
            {
                Debug.LogWarning("CameraController expects an Orthographic camera for 2D map navigation.");
            }

            SetupInputs();
        }

        private void SetupInputs()
        {
            _moveAction = new InputAction("Move");
            _moveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            string dragBinding = dragButton == 1 ? "<Mouse>/rightButton" : "<Mouse>/middleButton";
            _dragAction = new InputAction("Drag", binding: dragBinding);

            _scrollAction = new InputAction("Scroll", binding: "<Mouse>/scroll/y");
            _mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");

            _dragAction.started += _ => StartDrag();
            _dragAction.canceled += _ => EndDrag();
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _dragAction.Enable();
            _scrollAction.Enable();
            _mousePositionAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _dragAction.Disable();
            _scrollAction.Disable();
            _mousePositionAction.Disable();
        }

        private void Update()
        {
            HandleKeyboardPanning();
            HandleMouseDragging();
            HandleZooming();
        }

        private void HandleKeyboardPanning()
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();

            if (input != Vector2.zero)
            {
                // Normalize direction to prevent faster diagonal movement
                Vector3 direction = new Vector3(input.x, input.y, 0).normalized;

                // Scale the movement speed dynamically based on how zoomed out the camera is
                float zoomScaleMultiplier = _cam.orthographicSize / 5f;
                Vector3 moveDelta = direction * (panSpeed * zoomScaleMultiplier * Time.unscaledDeltaTime);

                ApplyMovement(moveDelta);
            }
        }

        private void StartDrag()
        {
            Vector2 mousePos = _mousePositionAction.ReadValue<Vector2>();
            _dragOrigin = _cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            _isDragging = true;
        }

        private void EndDrag()
        {
            _isDragging = false;
        }

        private void HandleMouseDragging()
        {
            if (_isDragging)
            {
                Vector2 mousePos = _mousePositionAction.ReadValue<Vector2>();
                Vector3 currentMouseWorld = _cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

                Vector3 difference = _dragOrigin - currentMouseWorld;
                ApplyMovement(difference);
            }
        }

        private void HandleZooming()
        {
            float scroll = _scrollAction.ReadValue<float>();
            if (scroll != 0)
            {
                // The new Input System typically returns scroll values of 120 or -120 per notch.
                // We normalize it to 1 or -1 to maintain consistent zoom speeds with the legacy system.
                float scrollDirection = Mathf.Sign(scroll);

                // If using a trackpad that sends smaller values, we scale it down relative to a standard notch.
                float normalizedScroll = scrollDirection * Mathf.Clamp01(Mathf.Abs(scroll) / 120f);

                float newSize = _cam.orthographicSize - (normalizedScroll * zoomSpeed);
                _cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }

        private void ApplyMovement(Vector3 moveDelta)
        {
            Vector3 newPosition = transform.position + moveDelta;

            if (useBounds)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
                newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
            }

            transform.position = newPosition;
        }
    }
}