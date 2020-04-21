using System.Linq;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public struct RootParameter
    {
        public RootParameter(RootDescriptorTable descriptorTable, ShaderVisibility visibility)
        {
            NativeRootParameter = new RootParameter1(new RootDescriptorTable1(descriptorTable.Ranges.Select(r => Unsafe.As<DescriptorRange, DescriptorRange1>(ref r)).ToArray()), (Vortice.Direct3D12.ShaderVisibility)visibility);
        }

        public RootParameter(RootConstants rootConstants, ShaderVisibility visibility)
        {
            NativeRootParameter = new RootParameter1(Unsafe.As<RootConstants, Vortice.Direct3D12.RootConstants>(ref rootConstants), (Vortice.Direct3D12.ShaderVisibility)visibility);
        }

        public RootParameter(RootParameterType parameterType, RootDescriptor rootDescriptor, ShaderVisibility visibility)
        {
            NativeRootParameter = new RootParameter1((Vortice.Direct3D12.RootParameterType)parameterType, Unsafe.As<RootDescriptor, RootDescriptor1>(ref rootDescriptor), (Vortice.Direct3D12.ShaderVisibility)visibility);
        }

        public RootParameterType ParameterType => (RootParameterType)NativeRootParameter.ParameterType;

        public RootDescriptorTable? RootDescriptorTable => NativeRootParameter.DescriptorTable?.Ranges != null ? new RootDescriptorTable(NativeRootParameter.DescriptorTable.Ranges.Select(r => Unsafe.As<DescriptorRange1, DescriptorRange>(ref r)).ToArray()) : null;

        public RootConstants Constants => Unsafe.As<Vortice.Direct3D12.RootConstants, RootConstants>(ref Unsafe.AsRef(NativeRootParameter.Constants));

        public RootDescriptor Descriptor => Unsafe.As<RootDescriptor1, RootDescriptor>(ref Unsafe.AsRef(NativeRootParameter.Descriptor));

        public ShaderVisibility ShaderVisibility => (ShaderVisibility)NativeRootParameter.ShaderVisibility;

        internal RootParameter1 NativeRootParameter { get; }
    }
}
