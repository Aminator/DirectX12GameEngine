using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialPixelStream
    {
        [ShaderMember] public static Vector3 MaterialNormal;

        [ShaderMember] public static float MaterialRoughness;

        [ShaderMember] public static Vector4 MaterialColorBase;

        [ShaderMember] public static Vector4 MaterialDiffuse;
        [ShaderMember] public static Vector3 MaterialSpecular;

        [ShaderMember] public static Vector3 ViewWS;

        [ShaderMember] public static Vector3 MaterialDiffuseVisible;
        [ShaderMember] public static Vector3 MaterialSpecularVisible;

        [ShaderMember] public static float NDotV;

        [ShaderMember] public static float AlphaRoughness;

        [ShaderMember]
        [ShaderMethod]
        public static void PrepareMaterialForLightingAndShading()
        {
            MaterialDiffuseVisible = new Vector3(MaterialDiffuse.X, MaterialDiffuse.Y, MaterialDiffuse.Z);
            MaterialSpecularVisible = MaterialSpecular;

            NDotV = Math.Max(Vector3.Dot(NormalStream.NormalWS, ViewWS), 0.0001f);

            AlphaRoughness = Math.Max(MaterialRoughness * MaterialRoughness, 0.001f);
        }

        [ShaderMember]
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
