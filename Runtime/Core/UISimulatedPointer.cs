using System;
using System.Collections.Generic;
using GG.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GG.Input
{
    /// <summary>
    /// Specialized input listener that translates mouse or gamepad inputs to an on-screen pointer.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UISimulatedPointer : MonoBehaviour, ITickable
    {
        #region VARIABLES

        [Header("References")]
        [SerializeField] private RectTransform _pointer;
        [SerializeField] private RectTransform _pointerOverlay;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private Camera _uiCamera;

        [Header("Values")] 
        [SerializeField] private float _pointerSpeedMouseKB;
        [SerializeField] private float _pointerSpeedGamepad;
        
        public TickGroup TickGroup => TickGroup.InputTransmission;
        public Vector2 Position { get; private set; }
        public Camera Camera => _uiCamera;

        private float _frameDelta;
        private string _controlScheme;
        private PointerEventData _eventData;
        private List<GameObject> _hoveredObjs = new List<GameObject>();
        private readonly List<GameObject> _selectedObjs = new List<GameObject>();
        private PlayerInput _input;
        private Guid _deltaActionID;
        private Guid _scrollActionID;
        private DataInputValuesPointer _dataInput;
        private readonly List<IInputReceiver<DataInputValuesPointer>> _receivers = new();

        #endregion VARIABLES


        #region INITIALIZATION

        private void Awake()
        {
            _eventData = new PointerEventData(_eventSystem);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void Init(PlayerInput input)
        {
            input.onActionTriggered += OnActionTriggered;
            _deltaActionID = input.actions["Delta"].id;
            _scrollActionID = input.actions["Scroll"].id;
            _input = input;
        }

        private void OnEnable()
        {
            TickRouter.Register(this);
        }

        private void OnDisable()
        {
            TickRouter.Unregister(this);
            if (_input)
            {
                _input.onActionTriggered -= OnActionTriggered;
            }
        }

        #endregion INITIALIZATION


        #region API

        internal void SetPointerVisible(bool visible)
        {
            
        }

        #endregion


        #region INPUT

        internal void UpdatePointer(float deltaTime)
        {
            // Move pointer
            Vector2 delta = _input.actions.FindAction(_deltaActionID).ReadValue<Vector2>();
            _dataInput.Scroll = _input.actions.FindAction(_scrollActionID).ReadValue<Vector2>();
            float speed = _controlScheme == "Gamepad" ? _pointerSpeedGamepad * deltaTime : _pointerSpeedMouseKB * deltaTime;
            Vector2 increase = speed * delta;
            _dataInput.Delta = increase;
            _pointer.anchoredPosition = GatePointerPosition(_pointer.anchoredPosition + increase);
            Position = _pointer.position;
            _dataInput.Position = Position;

            // Move virtual mouse
            _eventData.position = _uiCamera.WorldToScreenPoint(_pointer.position);
            List<RaycastResult> results = new List<RaycastResult>();
            _eventSystem.RaycastAll(_eventData, results);
            
            // Update hovered objects
            List<GameObject> newHovered = new List<GameObject>();
            foreach (RaycastResult obj in results)
            {
                newHovered.Add(obj.gameObject);
            }

            ResolveHoveredObjects(newHovered);

            foreach (GameObject obj in _hoveredObjs)
            {
                Move(obj);
            }
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            switch (context.action.name)
            {
                case "Select":
                    OnActionSelect(context.action);
                    break;
                case "SelectAlternate":
                    OnActionSelectAlternate(context.action);
                    break;
                case "SelectTertiary":
                    OnActionSelectTertiary(context.action);
                    break;
            }
        }

        private void OnActionSelect(InputAction action)
        {
            _eventData.button = PointerEventData.InputButton.Left;
            if (action.phase == InputActionPhase.Started)
            {
                _dataInput.SelectStarted = true;
                _dataInput.SelectIsPressed = true;
                ResolveActionPointerDown();
            }
            else if (action.phase == InputActionPhase.Canceled)
            {
                _dataInput.SelectReleased = true;
                _dataInput.SelectIsPressed = false;
                ResolveActionPointerUp();
            }
        }

        private void OnActionSelectAlternate(InputAction action)
        {
            _eventData.button = PointerEventData.InputButton.Right;
            if (action.phase == InputActionPhase.Started)
            {
                _dataInput.SelectAlternateStarted = true;
                _dataInput.SelectAlternateIsPressed = true;
                ResolveActionPointerDown();
            }
            else if (action.phase == InputActionPhase.Canceled)
            {
                _dataInput.SelectAlternateReleased = true;
                _dataInput.SelectAlternateIsPressed = false;
                ResolveActionPointerUp();
            }
        }

        private void OnActionSelectTertiary(InputAction action)
        {
            _eventData.button = PointerEventData.InputButton.Middle;
            if (action.phase == InputActionPhase.Started)
            {
                ResolveActionPointerDown();
            }
            else if (action.phase == InputActionPhase.Canceled)
            {
                ResolveActionPointerUp();
            }
        }

        public void OnControlsChanged(string newControls)
        {
            _controlScheme = newControls;
        }

        #endregion INPUT


        #region EVENTS

        private void ResolveHoveredObjects(List<GameObject> objs)
        {
            // Objects that were not in the previous hovered list have been entere
            foreach (GameObject obj in objs)
            {
                if (!_hoveredObjs.Contains(obj))
                {
                    Enter(obj);
                }
            }

            foreach (GameObject obj in _hoveredObjs)
            {
                if (!objs.Contains(obj))
                {
                    Exit(obj);
                }
            }

            _hoveredObjs = objs;
        }

        private void ResolveActionPointerDown()
        {
            foreach (GameObject obj in _hoveredObjs)
            {
                PointerDown(obj);
                _selectedObjs.Add(obj);
            }
        }

        private void ResolveActionPointerUp()
        {
            foreach (GameObject obj in _selectedObjs)
            {
                PointerUp(obj);
            }

            _selectedObjs.Clear();
        }

        private void Enter(GameObject obj)
        {
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.pointerEnterHandler);
        }

        private void Exit(GameObject obj)
        {
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.pointerExitHandler);
        }

        private void PointerDown(GameObject obj)
        {
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.pointerClickHandler);
        }

        private void PointerUp(GameObject obj)
        {
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.deselectHandler);
        }

        private void Move(GameObject obj)
        {
            ExecuteEvents.Execute(obj, _eventData, ExecuteEvents.pointerMoveHandler);
        }

        #endregion EVENTS


        #region TRANSMIT

        void ITickable.Tick(float delta)
        {
            foreach (IInputReceiver<DataInputValuesPointer> receiver in _receivers)
            {
                receiver.ReceiveInput(_dataInput, delta);
            }
            
            // Reset input
            _dataInput.SelectStarted = false;
            _dataInput.SelectReleased = false;
            _dataInput.SelectAlternateStarted = false;
            _dataInput.SelectAlternateReleased = false;
        }

        public void RegisterReceiver(IInputReceiver<DataInputValuesPointer> receiver)
        {
            _receivers.Add(receiver);
        }
        
        public void UnregisterReceiver(IInputReceiver<DataInputValuesPointer> receiver)
        {
            _receivers.Remove(receiver);
        }

        #endregion TRANSMIT


        #region UTILITY

        private Vector2 GatePointerPosition(Vector2 inPosition)
        {
            Vector2 adjustment = Vector2.zero;
            if (inPosition.x < _pointerOverlay.rect.xMin)
                adjustment.x = _pointerOverlay.rect.xMin - inPosition.x;
            else if (inPosition.x > _pointerOverlay.rect.xMax)
                adjustment.x = _pointerOverlay.rect.xMax - inPosition.x;
            
            if (inPosition.y < _pointerOverlay.rect.yMin)
                adjustment.y = _pointerOverlay.rect.yMin - inPosition.y;
            else if (inPosition.y > _pointerOverlay.rect.yMax)
                adjustment.y = _pointerOverlay.rect.yMax - inPosition.y;
            
            return inPosition + adjustment;
        }

        public Vector2 GetPointerPosForViewportRaycast()
        {
            return _uiCamera.WorldToViewportPoint(_pointer.position);
        }

        #endregion UTILITY
    }
}