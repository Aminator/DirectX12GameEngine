using DirectX12GameEngine.Editor.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class PropertyManagerViewModel : ObservableObject
    {
        private ClassPropertyViewModel? rootObject;

        public PropertyManagerViewModel(IPropertyManager propertyManager)
        {
            propertyManager.PropertyViewRequested += (s, e) => ShowProperties(e.Value);
        }

        public ClassPropertyViewModel? RootObject
        {
            get => rootObject;
            set => Set(ref rootObject, value);
        }

        private void ShowProperties(object value)
        {
            RootObject = new ClassPropertyViewModel(value, null)
            {
                IsExpanded = true
            };
        }
    }
}
