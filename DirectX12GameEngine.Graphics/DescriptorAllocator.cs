﻿using System;
using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    internal sealed class DescriptorAllocator : IDisposable
    {
        private const int DescriptorsPerHeap = 4096;

        private readonly object allocatorLock = new object();
        private readonly int descriptorSize;

        private CpuDescriptorHandle currentCpuHandle;
        private int remainingHandles;

        public DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType, int descriptorCount = DescriptorsPerHeap, DescriptorHeapFlags descriptorHeapFlags = DescriptorHeapFlags.None)
        {
            if (descriptorCount < 1 || descriptorCount > DescriptorsPerHeap)
            {
                throw new ArgumentOutOfRangeException(nameof(descriptorCount), $"Descriptor count must be between 1 and {DescriptorsPerHeap}.");
            }

            descriptorSize = device.NativeDevice.GetDescriptorHandleIncrementSize(descriptorHeapType);

            DescriptorHeapDescription descriptorHeapDescription = new DescriptorHeapDescription(descriptorHeapType, descriptorCount, descriptorHeapFlags);

            DescriptorHeap = device.NativeDevice.CreateDescriptorHeap(descriptorHeapDescription);

            remainingHandles = descriptorCount;
            currentCpuHandle = DescriptorHeap.GetCPUDescriptorHandleForHeapStart();
        }

        public ID3D12DescriptorHeap DescriptorHeap { get; }

        public CpuDescriptorHandle Allocate(int count)
        {
            if (count < 1 || (count > remainingHandles && remainingHandles != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and the remaining handles if the remaining handles are not 0.");
            }

            lock (allocatorLock)
            {
                if (remainingHandles == 0)
                {
                    remainingHandles = DescriptorHeap.Description.DescriptorCount;
                    currentCpuHandle = DescriptorHeap.GetCPUDescriptorHandleForHeapStart();
                }

                CpuDescriptorHandle cpuDescriptorHandle = currentCpuHandle;

                currentCpuHandle += descriptorSize * count;
                remainingHandles -= count;

                return cpuDescriptorHandle;
            }
        }

        public CpuDescriptorHandle AllocateSlot(int slot)
        {
            if (slot < 0 || slot > DescriptorHeap.Description.DescriptorCount - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be between 0 and the descript count - 1.");
            }

            lock (allocatorLock)
            {
                CpuDescriptorHandle cpuDescriptorHandle = DescriptorHeap.GetCPUDescriptorHandleForHeapStart() + descriptorSize * slot;

                return cpuDescriptorHandle;
            }
        }

        public void Dispose()
        {
            DescriptorHeap.Dispose();
        }
    }
}
