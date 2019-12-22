using System.Collections.Generic;
using Windows.UI.Xaml;

namespace DirectX12GameEngine.Input
{
    public class XamlInputSourceConfiguration : IInputSourceConfiguration
    {
        public XamlInputSourceConfiguration(UIElement element)
        {
            UwpGamepadInputSource gamepadSource = new UwpGamepadInputSource();
            Sources.Add(gamepadSource);

            XamlKeyboardInputSource keyboardSource = new XamlKeyboardInputSource(element);
            Sources.Add(keyboardSource);

            XamlPointerInputSource pointerSource = new XamlPointerInputSource(element);
            Sources.Add(pointerSource);
        }

        public IList<IInputSource> Sources { get; } = new List<IInputSource>();
    }
}
