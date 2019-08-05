using System;

namespace DirectX12GameEngine.Input
{
    [Flags]
    public enum VirtualKeyModifiers : uint
    {
        None = 0,
        Control = 1,
        Menu = 2,
        Shift = 4,
        Windows = 8
    }
}
