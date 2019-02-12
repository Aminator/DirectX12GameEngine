using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    [StaticShaderClass]
    public class LightStream
    {
        [StaticResource] public static Vector3 LightPosition;
        [StaticResource] public static Vector3 LightDirection;
        [StaticResource] public static Vector3 LightColor;
        [StaticResource] public static Vector3 LightColorNDotL;
        [StaticResource] public static Vector3 LightSpecularColorNDotL;

        [StaticResource] public static Vector3 EnvironmentDiffuseColor;
        [StaticResource] public static Vector3 EnvironmentSpecularColor;

        [StaticResource] public static float NDotL;
        [StaticResource] public static float LightDirectAmbientOcclusion;
    }
}
