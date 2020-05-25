using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class DescriptorSet
    {
        public DescriptorSet(GraphicsDevice device, IEnumerable<ResourceView> resources) : this(device, resources.Count(), DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)
        {
            AddResourceViews(resources);
        }

        public DescriptorSet(GraphicsDevice device, IEnumerable<Sampler> samplers) : this(device, samplers.Count(), DescriptorHeapType.Sampler)
        {
            AddSamplers(samplers);
        }

        public DescriptorSet(GraphicsDevice device, int descriptorCount, DescriptorHeapType descriptorHeapType = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)
        {
            if (descriptorCount < 1) throw new ArgumentOutOfRangeException(nameof(descriptorCount));

            GraphicsDevice = device;
            DescriptorHeapType = descriptorHeapType;
            DescriptorCapacity = descriptorCount;

            DescriptorAllocator = GetDescriptorAllocator();
            StartCpuDescriptorHandle = DescriptorAllocator.Allocate(DescriptorCapacity);
        }

        public GraphicsDevice GraphicsDevice { get; }

        public DescriptorHeapType DescriptorHeapType { get; }

        public int CurrentDescriptorCount { get; private set; }

        public int DescriptorCapacity { get; private set; }

        public DescriptorAllocator DescriptorAllocator { get; }

        public IntPtr StartCpuDescriptorHandle { get; }

        public void AddResourceViews(IEnumerable<ResourceView> resources)
        {
            AddDescriptors(resources.Select(r => r.CpuDescriptorHandle));
        }

        public void AddResourceViews(params ResourceView[] resources)
        {
            AddResourceViews(resources.AsEnumerable());
        }

        public void AddSamplers(IEnumerable<Sampler> samplers)
        {
            AddDescriptors(samplers.Select(r => r.CpuDescriptorHandle));
        }

        public void AddSamplers(params Sampler[] samplers)
        {
            AddSamplers(samplers);
        }

        private void AddDescriptor(IntPtr descriptor)
        {
            if (CurrentDescriptorCount + 1 > DescriptorCapacity) throw new InvalidOperationException();

            IntPtr destinationDescriptor = StartCpuDescriptorHandle + CurrentDescriptorCount * DescriptorAllocator.DescriptorHandleIncrementSize;

            GraphicsDevice.NativeDevice.CopyDescriptorsSimple(1, destinationDescriptor.ToCpuDescriptorHandle(), descriptor.ToCpuDescriptorHandle(), (Vortice.Direct3D12.DescriptorHeapType)DescriptorHeapType);

            CurrentDescriptorCount++;
        }

        private void AddDescriptors(IEnumerable<IntPtr> descriptors)
        {
            if (descriptors.Count() == 0) return;

            CpuDescriptorHandle[] sourceDescriptors = descriptors.Select(p => p.ToCpuDescriptorHandle()).ToArray();

            if (CurrentDescriptorCount + sourceDescriptors.Length > DescriptorCapacity) throw new InvalidOperationException();

            int[] sourceDescriptorRangeStarts = new int[sourceDescriptors.Length];

            //Array.Fill(srcDescriptorRangeStarts, 1);
            for (int i = 0; i < sourceDescriptorRangeStarts.Length; i++)
            {
                sourceDescriptorRangeStarts[i] = 1;
            }

            IntPtr destinationDescriptor = StartCpuDescriptorHandle + CurrentDescriptorCount * DescriptorAllocator.DescriptorHandleIncrementSize;

            GraphicsDevice.NativeDevice.CopyDescriptors(
                1, new[] { destinationDescriptor.ToCpuDescriptorHandle() }, new[] { sourceDescriptors.Length },
                sourceDescriptors.Length, sourceDescriptors, sourceDescriptorRangeStarts,
                (Vortice.Direct3D12.DescriptorHeapType)DescriptorHeapType);

            CurrentDescriptorCount += sourceDescriptors.Length;
        }

        private DescriptorAllocator GetDescriptorAllocator() => DescriptorHeapType switch
        {
            DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView => GraphicsDevice.ShaderResourceViewAllocator,
            DescriptorHeapType.Sampler => GraphicsDevice.SamplerAllocator,
            _ => throw new NotSupportedException("This descriptor heap type is not supported.")
        };
    }
}
