using System;
using System.Collections.ObjectModel;
using DirectX12GameEngine.Editor.ViewModels.Properties;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Messaging;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class PropertiesViewModel : ViewModelBase
    {
        private Type? rootObjectType;
        private ObservableCollection<PropertyViewModel>? properties;

        public PropertiesViewModel()
        {
            EventBus.Default.GetEvent<PropertiesViewRequestedEventArgs>().Invoked += (s, e) => ShowProperties(e.Object);
        }

        public Type? RootObjectType
        {
            get => rootObjectType;
            set => Set(ref rootObjectType, value);
        }

        public ObservableCollection<PropertyViewModel>? Properties
        {
            get => properties;
            set => Set(ref properties, value);
        }

        public void ShowProperties(object obj)
        {
            RootObjectType = obj.GetType();

            ClassPropertyViewModel classPropertyViewModel = new ClassPropertyViewModel(obj, null);
            classPropertyViewModel.IsExpanded = true;

            Properties = classPropertyViewModel.Properties;
        }
    }
}
