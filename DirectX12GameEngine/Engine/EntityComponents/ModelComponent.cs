using DirectX12GameEngine.Rendering;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(RenderSystem))]
    public sealed class ModelComponent : EntityComponent
    {
        public ModelComponent()
        {
        }

        public ModelComponent(Model model)
        {
            Model = model;
        }

        public Model? Model { get; set; }
    }
}
