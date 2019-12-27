using System.Threading.Tasks;

namespace DirectX12GameEngine.Editor
{
    public interface IClosable
    {
        public bool CanClose { get; }

        public Task<bool> TryCloseAsync();
    }
}
