using GG.Core;
using UnityEngine.InputSystem;

namespace GG.Input
{
    public class InputRouter : ITickable
    {
        #region VARIABLES

        public TickGroup TickGroup => TickGroup.Input;

        #endregion VARIABLES


        #region TICK

        void ITickable.Tick(float delta)
        {
            InputSystem.Update();
        }

        #endregion TICK
    }
}