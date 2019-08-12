using System;
using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    public static class BrdfMicrofacet
    {
        [ShaderMember]
        public static Vector3 FresnelSchlick(Vector3 f0, float LOrVDotH)
        {
            return FresnelSchlick(f0, Vector3.One, LOrVDotH);
        }

        [ShaderMember]
        public static Vector3 FresnelSchlick(Vector3 f0, Vector3 f90, float lOrVDotH)
        {
            return f0 + (f90 - f0) * (float)Math.Pow(1 - lOrVDotH, 5);
        }

        [ShaderMember]
        public static float VisibilitySmithSchlickGgx(float alphaRoughness, float nDotX)
        {
            float k = alphaRoughness * 0.5f;
            return nDotX / (nDotX * (1.0f - k) + k);
        }

        [ShaderMember]
        public static float VisibilitySmithSchlickGgx(float alphaRoughness, float nDotL, float nDotV)
        {
            return VisibilitySmithSchlickGgx(alphaRoughness, nDotL) * VisibilitySmithSchlickGgx(alphaRoughness, nDotV) / (nDotL * nDotV);
        }

        [ShaderMember]
        public static float NormalDistributionGgx(float alphaRoughness, float nDotH)
        {
            float alphaRoughness2 = alphaRoughness * alphaRoughness;
            float d = Math.Max(nDotH * nDotH * (alphaRoughness2 - 1) + 1, 0.0001f);
            return alphaRoughness2 / ((float)Math.PI * d * d);
        }
    }
}
