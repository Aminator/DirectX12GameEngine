namespace DirectX12GameEngine.Engine
{
    public abstract class EntityComponent
    {
        public Entity? Entity { get; internal set; }

        public virtual bool IsEnabled { get; set; } = true;
    }
}
