using System.Collections.Generic;

namespace DirectX12GameEngine.Input
{
    public interface IGamepadDevice
    {
        public bool IsWireless { get; }

        public GamepadVibration Vibration { get; set; }

        public ISet<GamepadButtons> DownButtons { get; }

        public ISet<GamepadButtons> PressedButtons { get; }

        public ISet<GamepadButtons> ReleasedButtons { get; }

        public GamepadReading CurrentReading { get; }

        public BatteryReport TryGetBatteryReport();

        public void Update();
    }
}
