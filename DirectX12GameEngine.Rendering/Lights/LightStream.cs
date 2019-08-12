using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    public class LightStream
    {
        [ShaderMember] public static Vector3 LightPositionWS;
        [ShaderMember] public static Vector3 LightDirectionWS;
        [ShaderMember] public static Vector3 LightColor;

        [ShaderMember] public static Vector3 LightColorNDotL;
        [ShaderMember] public static Vector3 LightSpecularColorNDotL;

        [ShaderMember] public static Vector3 EnvironmentLightDiffuseColor;
        [ShaderMember] public static Vector3 EnvironmentLightSpecularColor;

        [ShaderMember] public static float NDotL;

        [ShaderMember] public static float LightDirectAmbientOcclusion;

        [ShaderMember]
        public static void Reset()
        {
            LightPositionWS = default;
            LightDirectionWS = default;
            LightColor = default;
            LightColorNDotL = default;
            LightSpecularColorNDotL = default;
            EnvironmentLightDiffuseColor = default;
            EnvironmentLightSpecularColor = default;
            NDotL = default;
            LightDirectAmbientOcclusion = 1.0f;
        }
    }
}
