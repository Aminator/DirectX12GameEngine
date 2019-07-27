using System;
using DirectX12GameEngine.Core.Assets;
using Windows.Storage;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class ShaderContentManager : ContentManager
    {
        public ShaderContentManager(IServiceProvider services)
            : base(services)
        {
        }

        public ShaderContentManager(IServiceProvider services, IStorageFolder rootFolder)
            : base(services, rootFolder)
        {
        }
    }
}
