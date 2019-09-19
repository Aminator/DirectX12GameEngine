using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    internal sealed class DescriptorAllocator : IDisposable
    {
        private const int DescriptorsPerHeap = 4096;

        private readonly object allocatorLock = new object();

        public DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType, int descriptorCount = DescriptorsPerHeap, DescriptorHeapFlags descriptorHeapFlags = DescriptorHeapFlags.None)
        {
            if (descriptorCount < 1 || descriptorCount > DescriptorsPerHeap)
            {
                throw new ArgumentOutOfRangeException(nameof(descriptorCount), $"Descriptor count must be between 1 and {DescriptorsPerHeap}.");
            }

            DescriptorHandleIncrementSize = device.NativeDevice.GetDescriptorHandleIncrementSize((Vortice.Direct3D12.DescriptorHeapType)descriptorHeapType);

            DescriptorHeapDescription descriptorHeapDescription = new DescriptorHeapDescription((Vortice.Direct3D12.DescriptorHeapType)descriptorHeapType, descriptorCount, descriptorHeapFlags);

            DescriptorHeap = device.NativeDevice.CreateDescriptorHeap(descriptorHeapDescription);

            DescriptorCapacity = descriptorCount;
        }

        public int CurrentDescriptorCount { get; private set; }

        public int DescriptorCapacity { get; private set; }

        internal ID3D12DescriptorHeap DescriptorHeap { get; }

        internal int DescriptorHandleIncrementSize { get; }

        public GpuDescriptorHandle GetGpuDescriptorHandle(CpuDescriptorHandle descriptor)
        {
            if (!DescriptorHeap.Description.Flags.HasFlag(DescriptorHeapFlags.ShaderVisible)) throw new InvalidOperationException();

            return DescriptorHeap.GetGPUDescriptorHandleForHeapStart() + (descriptor.Ptr - DescriptorHeap.GetCPUDescriptorHandleForHeapStart().Ptr);
        }

        public CpuDescriptorHandle Allocate(int count)
        {
            lock (allocatorLock)
            {
                if (count < 1 || count > DescriptorCapacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and the total descriptor count.");
                }

                if (CurrentDescriptorCount + count > DescriptorCapacity)
                {
                    Reset();
                }

                CpuDescriptorHandle descriptor = DescriptorHeap.GetCPUDescriptorHandleForHeapStart() + CurrentDescriptorCount * DescriptorHandleIncrementSize;

                CurrentDescriptorCount += count;

                return descriptor;
            }
        }

        public CpuDescriptorHandle AllocateSlot(int slot)
        {
            lock (allocatorLock)
            {
                if (slot < 0 || slot > DescriptorCapacity - 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be between 0 and the total descriptor count - 1.");
                }

                CpuDescriptorHandle cpuDescriptorHandle = DescriptorHeap.GetCPUDescriptorHandleForHeapStart() + slot * DescriptorHandleIncrementSize;

                return cpuDescriptorHandle;
            }
        }

        public void Reset()
        {
            CurrentDescriptorCount = 0;
        }

        public void Dispose()
        {
            DescriptorHeap.Dispose();
        }
    }
}
