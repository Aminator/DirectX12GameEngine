using System;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Serialization
{
    public abstract class Asset
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public virtual string? MainSource => null;

        public abstract Task CreateAssetAsync(object obj, IServiceProvider services);
    }

    public abstract class Asset<T> : Asset
    {
        public abstract Task CreateAssetAsync(T obj, IServiceProvider services);

        public override Task CreateAssetAsync(object obj, IServiceProvider services) => CreateAssetAsync((T)obj, services);
    }
}
