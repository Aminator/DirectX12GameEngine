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

        internal CompiledCommandList? CommandList { get; set; }

        internal Texture[]? ConstantBuffers { get; set; }
    }
}
