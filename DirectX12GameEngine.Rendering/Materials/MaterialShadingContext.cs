using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public struct MaterialShadingContext
    {
        public Vector3 H;

        public float NDotL;

        public float NDotV;

        public float NDotH;

        public float LDotH;

        public Vector3 LightColor;

        public Vector3 DiffuseColor;

        public Vector3 SpecularColor;

        public float AlphaRoughness;
    }
}
