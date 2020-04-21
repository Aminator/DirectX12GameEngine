using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public struct RootDescriptor
    {
        public int ShaderRegister;

        public int RegisterSpace;

        public RootDescriptorFlags Flags;

        public RootDescriptor(int shaderRegister, int registerSpace, RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            ShaderRegister = shaderRegister;
            RegisterSpace = registerSpace;
            Flags = flags;
        }
    }
}
