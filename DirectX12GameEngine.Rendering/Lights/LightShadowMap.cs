namespace DirectX12GameEngine.Rendering.Lights
{
    public abstract class LightShadowMap : ILightShadow
    {
        public bool Enabled { get; set; }

        public ShadowMapBiasParameters BiasParameters { get; } = new ShadowMapBiasParameters();

        public sealed class ShadowMapBiasParameters
        {
            public float DepthBias { get; set; } = 0.01f;

            public float NormalOffsetScale { get; set; } = 10.0f;
        }
    }
}
