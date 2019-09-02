using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class DescriptorSet
    {
        public DescriptorSet(GraphicsDevice device, int descriptorCount, DescriptorHeapType descriptorHeapType = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)
        {
            if (descriptorCount < 1) throw new ArgumentOutOfRangeException(nameof(descriptorCount));

            GraphicsDevice = device;
            DescriptorHeapType = descriptorHeapType;
            TotalDescriptorCount = descriptorCount;

            DescriptorAllocator = GetDescriptorAllocator();
            StartCpuDescriptorHandle = DescriptorAllocator.Allocate(TotalDescriptorCount);
        }

        public GraphicsDevice GraphicsDevice { get; }

        public DescriptorHeapType DescriptorHeapType { get; }

        public int CurrentDescriptorCount { get; private set; }

        public int TotalDescriptorCount { get; private set; }

        internal DescriptorAllocator DescriptorAllocator { get; }

        internal CpuDescriptorHandle StartCpuDescriptorHandle { get; }

        public void AddConstantBufferViews(params GraphicsBuffer[] buffers)
        {
            AddConstantBufferViews(buffers.AsEnumerable());
        }

        public void AddConstantBufferViews(IEnumerable<GraphicsBuffer> buffers)
        {
            AddDescriptors(buffers.Select(r => r.CreateConstantBufferView()).ToArray());
        }

        public void AddShaderResourceViews(params GraphicsResource[] resources)
        {
            AddShaderResourceViews(resources.AsEnumerable());
        }

        public void AddShaderResourceViews(IEnumerable<GraphicsResource> resources)
        {
            AddDescriptors(resources.Select(r => r.NativeShaderResourceView).ToArray());
        }

        public void AddUnorderedAccessViews(params GraphicsResource[] resources)
        {
            AddUnorderedAccessViews(resources.AsEnumerable());
        }

        public void AddUnorderedAccessViews(IEnumerable<GraphicsResource> resources)
        {
            AddDescriptors(resources.Select(r => r.NativeUnorderedAccessView).ToArray());
        }

        public void AddSamplers(params SamplerState[] samplers)
        {
            AddSamplers(samplers);
        }

        public void AddSamplers(IEnumerable<SamplerState> samplers)
        {
            AddDescriptors(samplers.Select(r => r.NativeCpuDescriptorHandle).ToArray());
        }

        private void AddDescriptor(CpuDescriptorHandle descriptorHandle)
        {
            if (CurrentDescriptorCount + 1 > TotalDescriptorCount) throw new InvalidOperationException();

            CpuDescriptorHandle destinationDescriptor = StartCpuDescriptorHandle + CurrentDescriptorCount * DescriptorAllocator.DescriptorHandleIncrementSize;

            GraphicsDevice.NativeDevice.CopyDescriptorsSimple(1, destinationDescriptor, descriptorHandle, (Vortice.Direct3D12.DescriptorHeapType)DescriptorHeapType);

            CurrentDescriptorCount++;
        }

        private void AddDescriptors(CpuDescriptorHandle[] descriptors)
        {
            if (CurrentDescriptorCount + descriptors.Length > TotalDescriptorCount) throw new InvalidOperationException();

            int[] sourceDescriptorRangeStarts = new int[descriptors.Length];

            //Array.Fill(srcDescriptorRangeStarts, 1);
            for (int i = 0; i < sourceDescriptorRangeStarts.Length; i++)
            {
                sourceDescriptorRangeStarts[i] = 1;
            }

            CpuDescriptorHandle destinationDescriptor = StartCpuDescriptorHandle + CurrentDescriptorCount * DescriptorAllocator.DescriptorHandleIncrementSize;

            GraphicsDevice.NativeDevice.CopyDescriptors(
                1, new[] { destinationDescriptor }, new[] { descriptors.Length },
                descriptors.Length, descriptors, sourceDescriptorRangeStarts,
                (Vortice.Direct3D12.DescriptorHeapType)DescriptorHeapType);

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
