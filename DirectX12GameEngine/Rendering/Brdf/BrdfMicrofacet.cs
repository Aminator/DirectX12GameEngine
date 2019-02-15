using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Brdf
{
    [StaticShaderClass]
    public static class BrdfMicrofacet
    {
        [ShaderMethod]
        public static Vector3 FresnelSchlick(Vector3 f0, float LOrVDotH)
        {
            return FresnelSchlick(f0, Vector3.One, LOrVDotH);
        }

        [ShaderMethod]
        public static Vector3 FresnelSchlick(Vector3 f0, Vector3 f90, float lOrVDotH)
        {
            return f0 + (f90 - f0) * (float)Math.Pow(1 - lOrVDotH, 5);
        }

        [ShaderMethod]
        public static float VisibilitySmithSchlickGgx(float alphaRoughness, float nDotX)
        {
            float k = alphaRoughness * 0.5f;
            return nDotX / (nDotX * (1.0f - k) + k);
        }

        [ShaderMethod]
        public static float VisibilitySmithSchlickGgx(float alphaRoughness, float nDotL, float nDotV)
        {
            return VisibilitySmithSchlickGgx(alphaRoughness, nDotL) * VisibilitySmithSchlickGgx(alphaRoughness, nDotV) / (nDotL * nDotV);
        }

        [ShaderMethod]
        public static float NormalDistributionGgx(float alphaRoughness, float nDotH)
        {
            float alphaRoughness2 = alphaRoughness * alphaRoughness;
            float d = Math.Max(nDotH * nDotH * (alphaRoughness2 - 1) + 1, 0.0001f);
            return alphaRoughness2 / ((float)Math.PI * d * d);
        }
    }
}
