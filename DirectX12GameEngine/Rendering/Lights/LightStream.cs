using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    public class LightStream
    {
        [ShaderResource] public static Vector3 LightPositionWS;
        [ShaderResource] public static Vector3 LightDirectionWS;
        [ShaderResource] public static Vector3 LightColor;

        [ShaderResource] public static Vector3 LightColorNDotL;
        [ShaderResource] public static Vector3 LightSpecularColorNDotL;

        [ShaderResource] public static Vector3 EnvironmentLightDiffuseColor;
        [ShaderResource] public static Vector3 EnvironmentLightSpecularColor;

        [ShaderResource] public static float NDotL;

        [ShaderResource] public static float LightDirectAmbientOcclusion;

        [ShaderMethod]
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
