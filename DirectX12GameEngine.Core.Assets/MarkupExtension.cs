using System;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Core.Assets
{
    public abstract class MarkupExtension
    {
        public abstract Task<object> ProvideValueAsync(IServiceProvider services);
    }
}
