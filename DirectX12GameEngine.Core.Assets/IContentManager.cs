using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectX12GameEngine.Core.Assets
{
    public interface IContentManager
    {
        IStorageFolder RootFolder { get; set; }

        Task<bool> ExistsAsync(string path);

        Task<T> GetAsync<T>(string path) where T : class?;

        Task<object?> GetAsync(Type type, string path);

        Task<T> LoadAsync<T>(string path) where T : class;

        Task<object> LoadAsync(Type type, string path);

        Task<bool> ReloadAsync(object asset, string? newPath = null);

        Task SaveAsync(string path, object asset);

        bool TryGetAssetPath(object asset, out string? path);

        void Unload(object asset);

        void Unload(string path);
    }
}
