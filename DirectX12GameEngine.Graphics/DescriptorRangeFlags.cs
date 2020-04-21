using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum DescriptorRangeFlags
    {
        None = 0,
        DescriptorsVolatile = 1,
        DataVolatile = 2,
        DataStaticWhileSetAtExecute = 4,
        DataStatic = 8,
        DescriptorsStaticKeepingBufferBoundsChecks = 65536
    }
}
