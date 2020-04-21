using DirectX12GameEngine.Engine;
using Microsoft.Toolkit.Mvvm.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EntityComponentViewModel : ObservableObject
    {
        public EntityComponentViewModel(EntityComponent model)
        {
            Model = model;
        }

        public EntityComponent Model { get; }
    }
}
