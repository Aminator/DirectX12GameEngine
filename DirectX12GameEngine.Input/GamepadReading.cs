using System.Numerics;

namespace DirectX12GameEngine.Input
{
    public struct GamepadReading
    {
        public GamepadButtons Buttons { get; set; }

        public float LeftTrigger { get; set; }

        public float RightTrigger { get; set; }

        public Vector2 LeftThumbstick { get; set; }

        public Vector2 RightThumbstick { get; set; }
    }
}
