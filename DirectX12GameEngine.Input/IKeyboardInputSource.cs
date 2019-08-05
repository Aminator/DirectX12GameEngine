using System;
using System.Collections.Generic;

namespace DirectX12GameEngine.Input
{
    public interface IKeyboardInputSource : IInputSource
    {
        public event EventHandler<KeyEventArgs> KeyDown;

        public event EventHandler<KeyEventArgs> KeyUp;

        public ISet<VirtualKey> DownKeys { get; }

        public ISet<VirtualKey> PressedKeys { get; }

        public ISet<VirtualKey> ReleasedKeys { get; }
    }
}
