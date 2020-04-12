using System;
using System.Runtime.CompilerServices;
using SharpGen.Runtime;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class DescriptorAllocator : IDisposable
    {
        private const int DescriptorsPerHeap = 4096;

        private readonly object allocatorLock = new object();

        public DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType, int descriptorCount = DescriptorsPerHeap, DescriptorHeapFlags descriptorHeapFlags = DescriptorHeapFlags.None)
        {
            if (descriptorCount < 1 || descriptorCount > DescriptorsPerHeap)
            {
                throw new ArgumentOutOfRangeException(nameof(descriptorCount), $"Descriptor count must be between 1 and {DescriptorsPerHeap}.");
            }

            Type = descriptorHeapType;
            Flags = descriptorHeapFlags;

            DescriptorHandleIncrementSize = device.NativeDevice.GetDescriptorHandleIncrementSize((Vortice.Direct3D12.DescriptorHeapType)descriptorHeapType);

            DescriptorHeapDescription descriptorHeapDescription = new DescriptorHeapDescription((Vortice.Direct3D12.DescriptorHeapType)descriptorHeapType, descriptorCount, (Vortice.Direct3D12.DescriptorHeapFlags)descriptorHeapFlags);

            DescriptorHeap = device.NativeDevice.CreateDescriptorHeap(descriptorHeapDescription);

            DescriptorCapacity = descriptorCount;
        }

        public int CurrentDescriptorCount { get; private set; }

        public int DescriptorCapacity { get; private set; }

        public int DescriptorHandleIncrementSize { get; }

        public DescriptorHeapType Type { get; }

        public DescriptorHeapFlags Flags { get; }

        internal ID3D12DescriptorHeap DescriptorHeap { get; }

        public long GetGpuDescriptorHandle(IntPtr descriptor)
        {
            if (!Flags.HasFlag(DescriptorHeapFlags.ShaderVisible)) throw new InvalidOperationException();

            return DescriptorHeap.GetGPUDescriptorHandleForHeapStart().Ptr + ((PointerSize)descriptor - DescriptorHeap.GetCPUDescriptorHandleForHeapStart().Ptr);
        }

        public IntPtr Allocate(int count)
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

                IntPtr descriptor = DescriptorHeap.GetCPUDescriptorHandleForHeapStart().Ptr + CurrentDescriptorCount * DescriptorHandleIncrementSize;

                CurrentDescriptorCount += count;

                return descriptor;
            }
        }

        public IntPtr AllocateSlot(int slot)
        {
            lock (allocatorLock)
            {
                if (slot < 0 || slot > DescriptorCapacity - 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be between 0 and the total descriptor count - 1.");
                }

                IntPtr descriptor = DescriptorHeap.GetCPUDescriptorHandleForHeapStart().Ptr + slot * DescriptorHandleIncrementSize;

                return descriptor;
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

    internal static class DescriptorExtensions
    {
        public static CpuDescriptorHandle ToCpuDescriptorHandle(this IntPtr value) => Unsafe.As<IntPtr, CpuDescriptorHandle>(ref value);

        public static GpuDescriptorHandle ToGpuDescriptorHandle(this long value) => Unsafe.As<long, GpuDescriptorHandle>(ref value);
    }
}
