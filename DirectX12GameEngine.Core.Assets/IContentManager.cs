using System.Threading.Tasks;

namespace DirectX12GameEngine.Core.Assets
{
    public interface IContentManager
    {
        Task<bool> ExistsAsync(string path);

        Task<T> LoadAsync<T>(string path);

        void Unload(object asset);
    }
}
