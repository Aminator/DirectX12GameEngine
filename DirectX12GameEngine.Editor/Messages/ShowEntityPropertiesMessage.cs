using DirectX12GameEngine.Editor.ViewModels;

namespace DirectX12GameEngine.Editor.Messages
{
    public class ShowEntityPropertiesMessage
    {
        public ShowEntityPropertiesMessage(EntityViewModel entity)
        {
            Entity = entity;
        }

        public EntityViewModel Entity { get; }
    }
}
