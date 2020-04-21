using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public struct DescriptorRange
    {
        public DescriptorRangeType RangeType;

        public int NumDescriptors;

        public int BaseShaderRegister;

        public int RegisterSpace;

        public DescriptorRangeFlags Flags;

        public int OffsetInDescriptorsFromTableStart;

        public DescriptorRange(DescriptorRangeType rangeType, int numDescriptors, int baseShaderRegister, int registerSpace = 0, int offsetInDescriptorsFromTableStart = -1, DescriptorRangeFlags flags = DescriptorRangeFlags.None)
        {
            RangeType = rangeType;
            NumDescriptors = numDescriptors;
            BaseShaderRegister = baseShaderRegister;
            RegisterSpace = registerSpace;
            Flags = flags;
            OffsetInDescriptorsFromTableStart = offsetInDescriptorsFromTableStart;
        }
    }
}
