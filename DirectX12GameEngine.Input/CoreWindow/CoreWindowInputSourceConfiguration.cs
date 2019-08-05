using System.Collections.Generic;
using Windows.UI.Core;

namespace DirectX12GameEngine.Input
{
    public sealed class CoreWindowInputSourceConfiguration : IInputSourceConfiguration
    {
        public CoreWindowInputSourceConfiguration(CoreWindow coreWindow)
        {
            UwpGamepadInputSource gamepadSource = new UwpGamepadInputSource();
            Sources.Add(gamepadSource);

            CoreWindowKeyboardInputSource keyboardSource = new CoreWindowKeyboardInputSource(coreWindow);
            Sources.Add(keyboardSource);

            CoreWindowPointerInputSource pointerSource = new CoreWindowPointerInputSource(coreWindow);
            Sources.Add(pointerSource);
        }

        public IList<IInputSource> Sources { get; } = new List<IInputSource>();
    }
}
