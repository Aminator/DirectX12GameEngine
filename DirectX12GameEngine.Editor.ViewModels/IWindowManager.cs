using System.Threading.Tasks;
using Windows.Foundation;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface IWindowManager
    {
        Task<bool> TryCreateNewWindowAsync(TabViewViewModel tabView, Size size);
    }
}
