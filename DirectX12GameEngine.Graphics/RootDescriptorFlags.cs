using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum RootDescriptorFlags
    {
        None = 0,
        DataVolatile = 2,
        DataStaticWhileSetAtExecute = 4,
        DataStatic = 8
    }
}
