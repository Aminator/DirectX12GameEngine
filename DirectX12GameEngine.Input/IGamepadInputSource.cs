using System.Collections.ObjectModel;

namespace DirectX12GameEngine.Input
{
    public interface IGamepadInputSource : IInputSource
    {
        ObservableCollection<IGamepadDevice> Gamepads { get; }
    }
}
