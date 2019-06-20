using System.Threading.Tasks;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialGenerator
    {
        public static async Task<Material> GenerateAsync(MaterialDescriptor descriptor, MaterialGeneratorContext context)
        {
            context.PushMaterialDescriptor(descriptor);

            for (int passIndex = 0; passIndex < context.PassCount; passIndex++)
            {
                MaterialPass materialPass = context.PushPass();

                descriptor.Visit(context);

                materialPass.PipelineState = await context.CreateGraphicsPipelineStateAsync();
                (materialPass.NativeConstantBufferCpuDescriptorHandle, materialPass.NativeConstantBufferGpuDescriptorHandle) = context.GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(context.ConstantBuffers);
                (materialPass.NativeSamplerCpuDescriptorHandle, materialPass.NativeSamplerGpuDescriptorHandle) = context.GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(context.Samplers);
                (materialPass.NativeTextureCpuDescriptorHandle, materialPass.NativeTextureGpuDescriptorHandle) = context.GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(context.Textures);

                context.PopPass();
            }

            context.PopMaterialDescriptor();

            return context.Material;
        }
    }
}
