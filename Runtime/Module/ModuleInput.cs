using System;
using System.Collections;
using System.Collections.Generic;
using GG.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace GG.Input
{
    public class ModuleInput : CoreModule, ITickable
    {
        #region VARIABLES
        
        public TickGroup TickGroup => TickGroup.InputTransmission;
        public Action<InputControl> OnAnyButtonPressed;

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
        
        #endregion LOAD


        #region TICK

        void ITickable.Tick(float delta)
        {
            // For each operator...
            foreach (System.Collections.Generic.KeyValuePair<int, UISimulatedPointer> op in _operatorPointers)
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

        public void SetSimulatedPointerVisible(bool visible, int index = 0)
        {
            if (_operatorPointers.ContainsKey(index))
            {
                _operatorPointers[index].SetPointerVisible(visible);
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
            TickRouter.Unregister(this);
            if (_router != null)
            {
                TickRouter.Unregister(_router);
            }
        }

        #endregion CLEANUP
    }
}