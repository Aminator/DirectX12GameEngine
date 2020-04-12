using DirectX12GameEngine.Editor.ViewModels.Properties;
using DirectX12GameEngine.Mvvm;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class PropertyManagerViewModel : ViewModelBase
    {
        private ClassPropertyViewModel? rootObject;

        public ClassPropertyViewModel? RootObject
        {
            get => rootObject;
            set => Set(ref rootObject, value);
        }

        public void ShowProperties(object obj)
        {
            RootObject = new ClassPropertyViewModel(obj, null)
            {
                IsExpanded = true
            };
        }
    }
}
