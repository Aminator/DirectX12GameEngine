using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialRoughnessMapFeature : IMaterialRoughnessFeature
    {
        private bool invert;
        private GraphicsResource? invertBuffer;

        public MaterialRoughnessMapFeature()
        {
        }

        public MaterialRoughnessMapFeature(IScalarShader roughnessMap)
        {
            RoughnessMap = roughnessMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            RoughnessMap.Accept(context);

            invertBuffer ??= GraphicsResource.CreateBuffer(context.GraphicsDevice, Invert, ResourceFlags.None, HeapType.Upload);

            context.ConstantBufferViews.Add(invertBuffer.DefaultConstantBufferView);
        }

        public IScalarShader RoughnessMap { get; set; } = new ScalarShader();

        [ConstantBufferView]
        public bool Invert
        {
            get => invert;
            set
            {
                invert = value;
                invertBuffer?.SetData(invert);
            }
        }

        [ShaderMethod]
        public float ComputeRoughness(in SamplingContext context)
        {
            float roughness = RoughnessMap.ComputeScalar(context);
            return Invert ? 1.0f - roughness : roughness;
        }
    }
}
