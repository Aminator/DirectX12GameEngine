using System.Collections.Generic;
using Windows.Gaming.Input;

namespace DirectX12GameEngine.Input
{
    public class UwpGamepadInputSource : GamepadInputSourceBase
    {
        private readonly Dictionary<Gamepad, UwpGamepad> gamepads = new Dictionary<Gamepad, UwpGamepad>();

        public UwpGamepadInputSource()
        {
            Gamepad.GamepadAdded += OnGamepadAdded;
            Gamepad.GamepadRemoved += OnGamepadRemoved;

            Scan();
        }

        public override void Dispose()
        {
            base.Dispose();

            Gamepad.GamepadAdded -= OnGamepadAdded;
            Gamepad.GamepadRemoved -= OnGamepadRemoved;
        }

        public override void Scan()
        {
            base.Scan();

            foreach (Gamepad gamepad in Gamepad.Gamepads)
            {
                OnGamepadAdded(this, gamepad);
            }
        }

        private void OnGamepadAdded(object sender, Gamepad gamepad)
        {
            if (!gamepads.ContainsKey(gamepad))
            {
                UwpGamepad newGamepad = new UwpGamepad(gamepad);
                gamepads.Add(gamepad, newGamepad);
                Gamepads.Add(newGamepad);
            }
        }

        private void OnGamepadRemoved(object sender, Gamepad gamepad)
        {
            if (gamepads.TryGetValue(gamepad, out UwpGamepad currentGamepad))
            {
                gamepads.Remove(gamepad);
                Gamepads.Remove(currentGamepad);
            }
        }
    }
}
