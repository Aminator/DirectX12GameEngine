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

        public void Visit(MaterialGeneratorContext context)
        {
            RoughnessMap.Visit(context);

            invertBuffer ??= GraphicsBuffer.Constant.New(context.GraphicsDevice, Invert).DisposeBy(context.GraphicsDevice);

            context.ConstantBufferViews.Add(invertBuffer);
        }

        #region Shader

        [ShaderMember] public IComputeScalar RoughnessMap { get; set; } = new ComputeScalar();

        [ConstantBufferView] public bool Invert
        {
            get => invert;
            set
            {
                invert = value;
                invertBuffer?.SetData(invert);
            }
        }

        [ShaderMember]
        public void Compute()
        {
            float roughness = RoughnessMap.Compute();
            roughness = Invert ? 1.0f - roughness : roughness;

            MaterialPixelStream.MaterialRoughness = roughness;
        }

        #endregion
    }
}
