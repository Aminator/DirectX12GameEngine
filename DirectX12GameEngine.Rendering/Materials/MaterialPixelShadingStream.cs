using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialPixelShadingStream
    {
        [ShaderMember] public static Vector3 ShadingColor;
        [ShaderMember] public static float ShadingColorAlpha;

        [ShaderMember] public static Vector3 H;

        [ShaderMember] public static float NDotH;
        [ShaderMember] public static float LDotH;
        [ShaderMember] public static float VDotH;

        [ShaderMethod]
        public static void PrepareMaterialPerDirectLight()
        {
            H = Vector3.Normalize(MaterialPixelStream.ViewWS + LightStream.LightDirectionWS);
            NDotH = Vector3.Dot(NormalStream.NormalWS, H);
            LDotH = Vector3.Dot(LightStream.LightDirectionWS, H);
            VDotH = LDotH;
        }

        [ShaderMethod]
        public static void Reset()
        {
            ShadingColorAlpha = 1.0f;
        }
    }
}
