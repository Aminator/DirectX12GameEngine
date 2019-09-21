using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    public class LightStream
    {
        public static Vector3 LightPositionWS;
        public static Vector3 LightDirectionWS;
        public static Vector3 LightColor;

        public static Vector3 LightColorNDotL;
        public static Vector3 LightSpecularColorNDotL;

        public static Vector3 EnvironmentLightDiffuseColor;
        public static Vector3 EnvironmentLightSpecularColor;

        public static float NDotL;

        public static float LightDirectAmbientOcclusion;

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
