using System.Collections.Generic;
using Windows.UI.Xaml;

namespace DirectX12GameEngine.Input
{
    public class XamlInputSourceConfiguration : IInputSourceConfiguration
    {
        public XamlInputSourceConfiguration(UIElement uiElement)
        {
            UwpGamepadInputSource gamepadSource = new UwpGamepadInputSource();
            Sources.Add(gamepadSource);

            XamlKeyboardInputSource keyboardSource = new XamlKeyboardInputSource(uiElement);
            Sources.Add(keyboardSource);

            XamlPointerInputSource pointerSource = new XamlPointerInputSource(uiElement);
            Sources.Add(pointerSource);
        }

        public IList<IInputSource> Sources { get; } = new List<IInputSource>();
    }
}
