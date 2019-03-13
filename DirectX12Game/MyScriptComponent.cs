using DirectX12GameEngine.Engine;

namespace DirectX12Game
{
    [DefaultEntitySystem(typeof(MyScriptSystem))]
    public class MyScriptComponent : EntityComponent
    {
        public CameraComponent? Camera { get; set; }
    }
}
