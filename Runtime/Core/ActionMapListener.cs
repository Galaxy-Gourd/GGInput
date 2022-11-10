using GG.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GG.Input
{
    /// <summary>
    /// Dedicated class for listening to actions from a given input map. Typically managed through an operator.
    /// </summary>
    public abstract class ActionMapListener : MonoBehaviour, ITickable
    {
        #region VARIABLES

        public TickGroup TickGroup => TickGroup.InputTransmission;

        protected PlayerInput _input;
        protected string _currentControlScheme;
        private InputActionMap _actionMap;

        #endregion VARIABLES


        #region INITIALIZATION

        public virtual void Init(PlayerInput input, string actionMap, bool enable = false)
        {
            _input = input;
            _actionMap = _input.actions.FindActionMap(actionMap);
            OnControlsChanged(_input);
            
            // Enable as needed to start
            if (enable) _actionMap.Enable();
            else _actionMap.Disable();

            // Subscribe for input callbacks
            _input.onActionTriggered += OnActionTriggered;
            _input.onControlsChanged += OnControlsChanged;
        }

        private void OnEnable()
        {
            _actionMap?.Enable();
            TickRouter.Register(this);
        }

        private void OnDisable()
        {
            TickRouter.Unregister(this);
            if (!_input)
                return;
            
            // Unsub from input callbacks
            _input.onActionTriggered -= OnActionTriggered;
            _input.onControlsChanged -= OnControlsChanged;
            _actionMap?.Disable();
        }

        #endregion INITIALIZATION
        
        
        #region TICK
        
        void ITickable.Tick(float delta)
        {
            OnTick(delta);
        }

        protected virtual void OnTick(float delta)
        {
            
        }
        
        #endregion TICK


        #region INPUT
        
        protected virtual void OnActionTriggered(InputAction.CallbackContext callback)
        {
            
        }
        
        protected virtual void OnControlsChanged(PlayerInput input)
        {
            _currentControlScheme = input.currentControlScheme;
        }

        public void SetMapEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                _actionMap.Enable();
            }
            else
            {
                _actionMap.Disable();
            }
        }

        #endregion INPUT
    }
}