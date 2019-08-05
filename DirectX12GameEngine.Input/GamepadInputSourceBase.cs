using System.Collections.ObjectModel;

namespace DirectX12GameEngine.Input
{
    public class GamepadInputSourceBase : IGamepadInputSource
    {
        public ObservableCollection<IGamepadDevice> Gamepads { get; } = new ObservableCollection<IGamepadDevice>();

        public virtual void Dispose()
        {
            Gamepads.Clear();
        }

        public virtual void Scan()
        {
        }

        public virtual void Update()
        {
            foreach (IGamepadDevice gamepad in Gamepads)
            {
                gamepad.Update();
            }
        }
    }
}
