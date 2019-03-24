using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialPixelStream
    {
        [ShaderResource] public static Vector3 MaterialNormal;

        [ShaderResource] public static float MaterialRoughness;

        [ShaderResource] public static Vector4 MaterialColorBase;

        [ShaderResource] public static Vector4 MaterialDiffuse;
        [ShaderResource] public static Vector3 MaterialSpecular;

        [ShaderResource] public static Vector3 ViewWS;

        [ShaderResource] public static Vector3 MaterialDiffuseVisible;
        [ShaderResource] public static Vector3 MaterialSpecularVisible;

        [ShaderResource] public static float NDotV;

        [ShaderResource] public static float AlphaRoughness;

        [ShaderMethod]
        public static void PrepareMaterialForLightingAndShading()
        {
            Vector4 materialDiffuse = MaterialDiffuse;
            Vector3 materialSpecular = MaterialSpecular;

            MaterialDiffuseVisible = new Vector3(materialDiffuse.X, materialDiffuse.Y, materialDiffuse.Z);
            MaterialSpecularVisible = materialSpecular;

            NDotV = Math.Max(Vector3.Dot(NormalStream.NormalWS, ViewWS), 0.0001f);

            AlphaRoughness = Math.Max(MaterialRoughness * MaterialRoughness, 0.001f);
        }

        [ShaderMethod]
        public static void Reset()
        {
            MaterialNormal = Vector3.UnitZ;
            MaterialRoughness = default;
            MaterialDiffuse = default;
            MaterialSpecular = default;
            MaterialDiffuseVisible = default;
            MaterialSpecularVisible = default;
            AlphaRoughness = default;
        }
    }
}
