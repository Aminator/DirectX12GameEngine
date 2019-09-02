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

            TotalDescriptorCount = descriptorCount;
        }

        public int CurrentDescriptorCount { get; private set; }

        public int TotalDescriptorCount { get; private set; }

        internal ID3D12DescriptorHeap DescriptorHeap { get; }

        internal int DescriptorHandleIncrementSize { get; }

        internal CpuDescriptorHandle CurrentCpuDescriptorHandle => DescriptorHeap.GetCPUDescriptorHandleForHeapStart() + CurrentDescriptorCount * DescriptorHandleIncrementSize;

        internal GpuDescriptorHandle CurrentGpuDescriptorHandle => DescriptorHeap.GetGPUDescriptorHandleForHeapStart() + CurrentDescriptorCount * DescriptorHandleIncrementSize;

        public CpuDescriptorHandle Allocate(int count)
        {
            lock (allocatorLock)
            {
                if (count < 1 || count > TotalDescriptorCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and the total descriptor count.");
                }

                if (CurrentDescriptorCount + count > TotalDescriptorCount)
                {
                    Reset();
                }

                CpuDescriptorHandle cpuDescriptorHandle = CurrentCpuDescriptorHandle;
                CurrentDescriptorCount += count;

                return cpuDescriptorHandle;
            }
        }

        public CpuDescriptorHandle AllocateSlot(int slot)
        {
            lock (allocatorLock)
            {
                if (slot < 0 || slot > TotalDescriptorCount - 1)
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
