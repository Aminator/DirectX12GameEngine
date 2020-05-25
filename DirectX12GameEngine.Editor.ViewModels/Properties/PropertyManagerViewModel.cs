using DirectX12GameEngine.Editor.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class PropertyManagerViewModel : ObservableObject
    {
        private readonly ITabViewManager tabViewManager;

        private ClassPropertyViewModel? rootObject;

        public PropertyManagerViewModel(IPropertyManager propertyManager, ITabViewManager tabViewManager)
        {
            propertyManager.PropertyViewRequested += (s, e) => ShowProperties(e.Value);
            this.tabViewManager = tabViewManager;
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

            tabViewManager.OpenTab(this, tabViewManager.SolutionExplorerTabView);
        }
    }
}
