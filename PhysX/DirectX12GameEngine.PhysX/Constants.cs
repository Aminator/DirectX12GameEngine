using System;

namespace DirectX12GameEngine.PhysX
{
    public partial class PhysX
    {
        public static Version CurrentVersion { get; } = new Version(4, 1, 1);

        public static uint CurrentVersionAsInteger => (uint)((CurrentVersion.Major << 24) + (CurrentVersion.Minor << 16) + (CurrentVersion.Build << 8) + 0);
    }
}
