using System;
using System.Collections.Generic;

namespace DirectX12GameEngine.Input
{
    public abstract class PointerEventArgs : EventArgs
    {
        public abstract bool Handled { get; set; }

        public abstract PointerPoint CurrentPoint { get; }

        public abstract VirtualKeyModifiers KeyModifiers { get; }

        public abstract IList<PointerPoint> GetIntermediatePoints();
    }
}
