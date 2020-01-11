using System.Threading.Tasks;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface IClosable
    {
        public bool CanClose { get; }

        public Task<bool> TryCloseAsync();
    }
}
