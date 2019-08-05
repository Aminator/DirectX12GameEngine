#if NETCOREAPP
using System.Collections.Generic;
using System.Windows.Forms;

namespace DirectX12GameEngine.Input
{
    public class WinFormsInputSourceConfiguration : IInputSourceConfiguration
    {
        public WinFormsInputSourceConfiguration(Control control)
        {
            UwpGamepadInputSource gamepadSource = new UwpGamepadInputSource();
            Sources.Add(gamepadSource);

            WinFormsKeyboard keyboard = new WinFormsKeyboard(control);
            Sources.Add(keyboard);
        }

        public IList<IInputSource> Sources { get; } = new List<IInputSource>();
    }
}
#endif
