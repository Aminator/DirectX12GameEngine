using System;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Serialization
{
    public abstract class Asset
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public virtual string? MainSource => null;

        public abstract Task<object> CreateAssetAsync(IServiceProvider services);
    }
}
