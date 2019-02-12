namespace DirectX12GameEngine.Rendering.Lights
{
    public class DirectLightBase : ColorLightBase, IDirectLight
    {
        public LightShadowMap? Shadow { get; protected set; }
    }
}
