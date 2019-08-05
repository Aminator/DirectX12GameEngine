namespace DirectX12GameEngine.Input
{
    public abstract class BatteryReport
    {
        public abstract int? ChargeRateInMilliwatts { get; }

        public abstract int? DesignCapacityInMilliwattHours { get; }

        public abstract int? FullChargeCapacityInMilliwattHours { get; }

        public abstract int? RemainingCapacityInMilliwattHours { get; }

        public abstract BatteryStatus Status { get; }
    }
}
