using System.Numerics;
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

        [ShaderMethod]
        public override Vector3 ComputeLightColor(int lightIndex)
        {
            return Lights[lightIndex].Color;
        }

        [ShaderMethod]
        public override Vector3 ComputeLightDirection(int lightIndex)
        {
            return -Lights[lightIndex].Direction;
        }
    }
}
