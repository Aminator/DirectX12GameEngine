namespace DirectX12GameEngine.Rendering.Materials
{
    public static class MaterialGenerator
    {
        public static Material Generate(MaterialDescriptor descriptor, MaterialGeneratorContext context)
        {
            context.PushMaterialDescriptor(descriptor);

            for (int passIndex = 0; passIndex < context.PassCount; passIndex++)
            {
                MaterialPass materialPass = context.PushPass();

                descriptor.Visit(context);

                materialPass.PipelineState = context.CreateGraphicsPipelineState();
                (materialPass.NativeCpuDescriptorHandle, materialPass.NativeGpuDescriptorHandle) = context.CopyDescriptors();

                context.PopPass();
            }

            context.PopMaterialDescriptor();

            return context.Material;
        }
    }
}
