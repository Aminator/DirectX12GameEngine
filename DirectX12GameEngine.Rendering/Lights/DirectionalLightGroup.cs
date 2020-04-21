using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    [ShaderContract]
    [ConstantBufferView]
    public class DirectionalLightGroup : DirectLightGroup
    {
#nullable disable
        [ShaderMember]
        public DirectionalLightData[] Lights;
#nullable restore

        /// <summary>
        /// Compute the light color/direction for the specified index within this group.
        /// </summary>
        [ShaderMethod]
        protected override void PrepareDirectLightCore(int lightIndex)
        {
            LightStream.LightColor = Lights[lightIndex].Color;
            LightStream.LightDirectionWS = -Lights[lightIndex].Direction;
        }
    }
}
