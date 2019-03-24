namespace DirectX12GameEngine.Rendering.Lights
{
    public class DirectionalLight : DirectLightBase
    {
        public DirectionalLight()
        {
            Shadow = new DirectionalLightShadowMap();
        }
    }
}
