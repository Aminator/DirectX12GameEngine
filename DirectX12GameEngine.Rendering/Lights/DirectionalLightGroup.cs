using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    [ShaderContract]
    [ConstantBufferResource]
    public class DirectionalLightGroup : DirectLightGroup
    {
#nullable disable
        [ShaderMember] public DirectionalLightData[] Lights;
#nullable enable

        /// <summary>
        /// Compute the light color/direction for the specified index within this group.
        /// </summary>
        [ShaderMember]
        protected override void PrepareDirectLightCore(int lightIndex)
        {
            LightStream.LightColor = Lights[lightIndex].Color;
            LightStream.LightDirectionWS = -Lights[lightIndex].Direction;
        }
    }
}
