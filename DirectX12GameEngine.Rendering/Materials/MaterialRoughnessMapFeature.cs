using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialRoughnessMapFeature : IMaterialMicroSurfaceFeature
    {
        private bool invert;
        private GraphicsBuffer<bool>? invertBuffer;

        public MaterialRoughnessMapFeature()
        {
        }

        public MaterialRoughnessMapFeature(IComputeScalar roughnessMap)
        {
            RoughnessMap = roughnessMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            RoughnessMap.Accept(context);

            invertBuffer ??= GraphicsBuffer.Create(context.GraphicsDevice, Invert, ResourceFlags.None, GraphicsHeapType.Upload);

            context.ConstantBufferViews.Add(invertBuffer);
        }

        public IComputeScalar RoughnessMap { get; set; } = new ComputeScalar();

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
        public void Compute()
        {
            float roughness = RoughnessMap.Compute();
            roughness = Invert ? 1.0f - roughness : roughness;

            MaterialPixelStream.MaterialRoughness = roughness;
        }
    }
}
