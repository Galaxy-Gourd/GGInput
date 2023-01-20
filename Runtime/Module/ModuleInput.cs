using System;
using System.Collections;
using System.Collections.Generic;
using GG.Core;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace GG.Input
{
    public class ModuleInput : CoreModule, ITickable
    {
        #region VARIABLES
        
        public TickGroup TickGroup => TickGroup.InputTransmission;
        public Action<InputControl> OnAnyButtonPressed;
        public Action<InputDevice, InputDeviceChange> OnInputDeviceChanged;

        private Mouse _systemMouse;
        private InputRouter _router;
        private readonly Dictionary<int, UISimulatedPointer> _operatorPointers = new ();
        private IDisposable _buttonPressListener;

        #endregion VARIABLES


        #region LOAD

        protected override IEnumerator LoadModule()
        {
            // Register with tick callbacks
            _router = new InputRouter();
            TickRouter.Register(this);
            TickRouter.Register(_router);

            _buttonPressListener = InputSystem.onAnyButtonPress.Call(DoAnyButtonPressed);
            InputSystem.onDeviceChange += DoInputDeviceChanged;

            yield return null;
        }

        /// <summary>
        /// Responds to button presses; can be listened to to dynamically change control schemes
        /// </summary>
        /// <param name="control"></param>
        private void DoAnyButtonPressed(InputControl control)
        {
            OnAnyButtonPressed?.Invoke(control);
        }
        
        private void DoInputDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            OnInputDeviceChanged?.Invoke(device, change);
        }

        #endregion LOAD


        #region TICK

        void ITickable.Tick(float delta)
        {
            // For each operator...
            foreach (KeyValuePair<int, UISimulatedPointer> op in _operatorPointers)
            {
                op.Value.UpdatePointer(delta);
            }
        }

        #endregion TICK


        #region API

        public void RegisterGamePointerForOperator(UISimulatedPointer pointer, int index)
        {
            // Set or replace pointer reference
            if (!_operatorPointers.ContainsKey(index))
            {
                _operatorPointers.Add(index, pointer);
            }
            else
            {
                _operatorPointers[index] = pointer;
            }
        }

        public void UnregisterGamePointerForOperator(int index)
        {
            if (_operatorPointers.ContainsKey(index))
            {
                _operatorPointers.Remove(index);
            }
        }

        public UISimulatedPointer GetPointerForOperator(int index = 0)
        {
            return !_operatorPointers.ContainsKey(index) ? null : _operatorPointers[index];
        }

        #endregion API


        #region CLEANUP

        protected override void OnModuleDestroy()
        {
            base.OnModuleDestroy();

            _buttonPressListener?.Dispose();
            InputSystem.onDeviceChange -= OnInputDeviceChanged;
            TickRouter.Unregister(this);
            if (_router != null)
            {
                TickRouter.Unregister(_router);
            }
        }

        #endregion CLEANUP
    }
}