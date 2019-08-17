using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class DescriptorSet
    {
        public DescriptorSet(GraphicsDevice device, DescriptorHeapType descriptorHeapType, int descriptorCount)
        {
            if (descriptorCount < 1) throw new ArgumentOutOfRangeException(nameof(descriptorCount));

            GraphicsDevice = device;
            DescriptorHeapType = descriptorHeapType;
            TotalDescriptorCount = descriptorCount;

            DescriptorAllocator = GetDescriptorAllocator();
            StartCpuDescriptorHandle = DescriptorAllocator.Allocate(TotalDescriptorCount);
        }

        public DescriptorSet(GraphicsDevice device, DescriptorHeapType descriptorHeapType, GraphicsResource resource) : this(device, descriptorHeapType, 1)
        {
            AddDescriptor(resource);
        }

        public DescriptorSet(GraphicsDevice device, DescriptorHeapType descriptorHeapType, IEnumerable<GraphicsResource> resources) : this(device, descriptorHeapType, resources.Count())
        {
            AddDescriptors(resources);
        }

        public GraphicsDevice GraphicsDevice { get; }

        public DescriptorHeapType DescriptorHeapType { get; }

        public int CurrentDescriptorCount { get; private set; }

        public int TotalDescriptorCount { get; private set; }

        internal DescriptorAllocator DescriptorAllocator { get; }

        internal CpuDescriptorHandle StartCpuDescriptorHandle { get; private set; }

        public void AddDescriptor(GraphicsResource resource)
        {
            if (CurrentDescriptorCount + 1 > TotalDescriptorCount) throw new InvalidOperationException();

            CpuDescriptorHandle destinationDescriptor = StartCpuDescriptorHandle + CurrentDescriptorCount * DescriptorAllocator.DescriptorIncrementSize;

            GraphicsDevice.NativeDevice.CopyDescriptorsSimple(1, destinationDescriptor, resource.NativeCpuDescriptorHandle, (Vortice.DirectX.Direct3D12.DescriptorHeapType)DescriptorHeapType);

            CurrentDescriptorCount++;
        }

        public void AddDescriptors(IEnumerable<GraphicsResource> resources)
        {
            if (CurrentDescriptorCount + resources.Count() > TotalDescriptorCount) throw new InvalidOperationException();

            CpuDescriptorHandle[] descriptors = resources.Select(t => t.NativeCpuDescriptorHandle).ToArray();
            int[] sourceDescriptorRangeStarts = new int[descriptors.Length];

            //Array.Fill(srcDescriptorRangeStarts, 1);
            for (int i = 0; i < sourceDescriptorRangeStarts.Length; i++)
            {
                sourceDescriptorRangeStarts[i] = 1;
            }

            CpuDescriptorHandle destinationDescriptor = StartCpuDescriptorHandle + CurrentDescriptorCount * DescriptorAllocator.DescriptorIncrementSize;

            GraphicsDevice.NativeDevice.CopyDescriptors(
                1, new[] { destinationDescriptor }, new[] { descriptors.Length },
                descriptors.Length, descriptors, sourceDescriptorRangeStarts,
                (Vortice.DirectX.Direct3D12.DescriptorHeapType)DescriptorHeapType);

            CurrentDescriptorCount += descriptors.Length;
        }

        private DescriptorAllocator GetDescriptorAllocator() => DescriptorHeapType switch
        {
            DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView => GraphicsDevice.ShaderResourceViewAllocator,
            DescriptorHeapType.Sampler => GraphicsDevice.SamplerAllocator,
            _ => throw new NotSupportedException("This descriptor heap type is not supported.")
        };
    }
}
