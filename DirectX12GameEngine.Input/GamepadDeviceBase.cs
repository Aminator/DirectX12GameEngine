using System.Collections.Generic;

namespace DirectX12GameEngine.Input
{
    public abstract class GamepadDeviceBase : IGamepadDevice
    {
        public abstract bool IsWireless { get; }

        public abstract GamepadVibration Vibration { get; set; }

        public ISet<GamepadButtons> DownButtons { get; } = new HashSet<GamepadButtons>();

        public ISet<GamepadButtons> PressedButtons { get; } = new HashSet<GamepadButtons>();

        public ISet<GamepadButtons> ReleasedButtons { get; } = new HashSet<GamepadButtons>();

        public abstract GamepadReading CurrentReading { get; }

        public abstract BatteryReport TryGetBatteryReport();

        public abstract void Update();

        protected void ClearButtonStates()
        {
            PressedButtons.Clear();
            ReleasedButtons.Clear();
        }

        protected void UpdateButtonState(GamepadButtons button, bool isDown)
        {
            if (isDown && !DownButtons.Contains(button))
            {
                PressedButtons.Add(button);
                DownButtons.Add(button);
            }
            else if (!isDown && DownButtons.Contains(button))
            {
                ReleasedButtons.Add(button);
                DownButtons.Remove(button);
            }
        }
    }
}
