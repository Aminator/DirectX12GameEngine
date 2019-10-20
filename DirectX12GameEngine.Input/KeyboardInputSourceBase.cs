using System;
using System.Collections.Generic;

namespace DirectX12GameEngine.Input
{
    public class KeyboardInputSourceBase : IKeyboardInputSource
    {
        private readonly List<KeyEventArgs> keyEvents = new List<KeyEventArgs>();

        public event EventHandler<KeyEventArgs>? KeyDown;

        public event EventHandler<KeyEventArgs>? KeyUp;

        public ISet<VirtualKey> DownKeys { get; } = new HashSet<VirtualKey>();

        public ISet<VirtualKey> PressedKeys { get; } = new HashSet<VirtualKey>();

        public ISet<VirtualKey> ReleasedKeys { get; } = new HashSet<VirtualKey>();

        public virtual void Dispose()
        {
        }

        public virtual void Scan()
        {
        }

        public virtual void Update()
        {
            PressedKeys.Clear();
            ReleasedKeys.Clear();

            foreach (KeyEventArgs keyEvent in keyEvents)
            {
                if (DownKeys.Contains(keyEvent.Key))
                {
                    PressedKeys.Add(keyEvent.Key);
                }
                else
                {
                    ReleasedKeys.Add(keyEvent.Key);
                }
            }

            keyEvents.Clear();
        }

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
            if (!DownKeys.Contains(e.Key))
            {
                DownKeys.Add(e.Key);
                keyEvents.Add(e);
            }

            KeyDown?.Invoke(this, e);
        }

        protected virtual void OnKeyUp(KeyEventArgs e)
        {
            if (DownKeys.Contains(e.Key))
            {
                DownKeys.Remove(e.Key);
                keyEvents.Add(e);
            }

            KeyUp?.Invoke(this, e);
        }
    }
}
