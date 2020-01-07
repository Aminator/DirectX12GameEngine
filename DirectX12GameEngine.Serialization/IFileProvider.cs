using System.IO;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Serialization
{
    public interface IFileProvider
    {
        string RootPath { get; }

        Task<bool> ExistsAsync(string path);

        Task<Stream> OpenStreamAsync(string path, FileMode mode, FileAccess access);
    }
}
