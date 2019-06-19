using System;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Core.Assets
{
    public abstract class Asset
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public virtual string? MainSource => null;

        public abstract Task CreateAssetAsync(object obj);
    }

    public abstract class Asset<T> : Asset
    {
        public override Task CreateAssetAsync(object obj) => CreateAssetAsync((T)obj);

        public abstract Task CreateAssetAsync(T obj);
    }
}
