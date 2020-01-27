using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Mvvm;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EntityComponentViewModel : ViewModelBase<EntityComponent>
    {
        public EntityComponentViewModel(EntityComponent model) : base(model)
        {
        }
    }
}
