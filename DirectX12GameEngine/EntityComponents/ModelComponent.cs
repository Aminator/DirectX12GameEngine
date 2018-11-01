namespace DirectX12GameEngine
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
