using System;

namespace DirectX12GameEngine.Input
{
    public abstract class KeyEventArgs : EventArgs
    {
        public abstract bool Handled { get; set; }

        public abstract VirtualKey Key { get; }
    }
}
