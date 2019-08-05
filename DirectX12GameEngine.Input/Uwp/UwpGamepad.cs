using System;
using System.Numerics;
using Windows.Gaming.Input;

namespace DirectX12GameEngine.Input
{
    public class UwpGamepad : GamepadDeviceBase
    {
        private readonly Gamepad gamepad;
        private GamepadReading currentReading;

        public UwpGamepad(Gamepad gamepad)
        {
            this.gamepad = gamepad;
        }

        public override bool IsWireless => gamepad.IsWireless;

        public override GamepadVibration Vibration
        {
            get => ToVibration(gamepad.Vibration);
            set => gamepad.Vibration = ToVibration(value);
        }

        public override GamepadReading CurrentReading => currentReading;

        public override BatteryReport TryGetBatteryReport()
        {
            return new UwpBatteryReport(gamepad.TryGetBatteryReport());
        }

        public override void Update()
        {
            GamepadReading oldReading = currentReading;
            GamepadReading newReading = GetCurrentReading();

            currentReading = newReading;

            ClearButtonStates();

            foreach (GamepadButtons button in Enum.GetValues(typeof(GamepadButtons)))
            {
                bool oldState = oldReading.Buttons.HasFlag(button);
                bool newState = newReading.Buttons.HasFlag(button);

                if (oldState != newState)
                {
                    UpdateButtonState(button, newState);
                }
            }
        }

        private GamepadReading GetCurrentReading()
        {
            Windows.Gaming.Input.GamepadReading reading = gamepad.GetCurrentReading();

            return new GamepadReading
            {
                Buttons = (GamepadButtons)reading.Buttons,
                LeftThumbstick = new Vector2((float)reading.LeftThumbstickX, (float)reading.LeftThumbstickY),
                RightThumbstick = new Vector2((float)reading.RightThumbstickX, (float)reading.RightThumbstickY),
                LeftTrigger = (float)reading.LeftTrigger,
                RightTrigger = (float)reading.RightTrigger,
            };
        }

        public GamepadVibration ToVibration(in Windows.Gaming.Input.GamepadVibration vibration) => new GamepadVibration
        {
            LeftMotor = vibration.LeftMotor,
            LeftTrigger = vibration.LeftTrigger,
            RightMotor = vibration.RightMotor,
            RightTrigger = vibration.RightTrigger
        };

        private Windows.Gaming.Input.GamepadVibration ToVibration(in GamepadVibration vibration) => new Windows.Gaming.Input.GamepadVibration
        {
            LeftMotor = vibration.LeftMotor,
            LeftTrigger = vibration.LeftTrigger,
            RightMotor = vibration.RightMotor,
            RightTrigger = vibration.RightTrigger
        };

        private class UwpBatteryReport : BatteryReport
        {
            private readonly Windows.Devices.Power.BatteryReport batteryReport;

            public UwpBatteryReport(Windows.Devices.Power.BatteryReport batteryReport)
            {
                this.batteryReport = batteryReport;
            }

            public override int? ChargeRateInMilliwatts => batteryReport.ChargeRateInMilliwatts;

            public override int? DesignCapacityInMilliwattHours => batteryReport.DesignCapacityInMilliwattHours;

            public override int? FullChargeCapacityInMilliwattHours => batteryReport.FullChargeCapacityInMilliwattHours;

            public override int? RemainingCapacityInMilliwattHours => batteryReport.RemainingCapacityInMilliwattHours;

            public override BatteryStatus Status => (BatteryStatus)batteryReport.Status;
        }
    }
}
