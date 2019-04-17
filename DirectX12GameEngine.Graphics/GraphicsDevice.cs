using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DirectX12GameEngine.Core;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class GraphicsDevice : IDisposable, ICollector
    {
        private readonly AutoResetEvent fenceEvent = new AutoResetEvent(false);

        public GraphicsDevice(FeatureLevel minFeatureLevel = FeatureLevel.Level_11_0)
        {
#if DEBUG
            //DebugInterface.Get().EnableDebugLayer();
#endif
            FeatureLevel = minFeatureLevel < FeatureLevel.Level_11_0 ? FeatureLevel.Level_11_0 : minFeatureLevel;

            NativeDevice = new Device(null, FeatureLevel);

            NativeCommandQueue = NativeDevice.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            NativeCopyCommandQueue = NativeDevice.CreateCommandQueue(new CommandQueueDescription(CommandListType.Copy));

            NativeDirect3D11Device = SharpDX.Direct3D11.Device.CreateFromDirect3D12(
                NativeDevice,
                SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                null,
                null,
                NativeCommandQueue);

            BundleAllocatorPool = new CommandAllocatorPool(this, CommandListType.Bundle);
            CopyAllocatorPool = new CommandAllocatorPool(this, CommandListType.Copy);
            DirectAllocatorPool = new CommandAllocatorPool(this, CommandListType.Direct);

            DepthStencilViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.DepthStencilView, descriptorCount: 1);
            RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.RenderTargetView, descriptorCount: 2);
            ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, DescriptorHeapFlags.ShaderVisible);

            NativeCopyFence = NativeDevice.CreateFence(0, FenceFlags.None);
            NativeFence = NativeDevice.CreateFence(0, FenceFlags.None);

            CommandList = new CommandList(this, CommandListType.Direct);
            CommandList.Close();
        }

        public CommandList CommandList { get; }

        public ICollection<IDisposable> Disposables { get; } = new List<IDisposable>();

        public FeatureLevel FeatureLevel { get; }

        public GraphicsPresenter? Presenter { get; set; }

        internal CommandAllocatorPool BundleAllocatorPool { get; }

        internal CommandAllocatorPool CopyAllocatorPool { get; }

        internal CommandAllocatorPool DirectAllocatorPool { get; }

        internal DescriptorAllocator DepthStencilViewAllocator { get; set; }

        internal DescriptorAllocator RenderTargetViewAllocator { get; set; }

        internal DescriptorAllocator ShaderResourceViewAllocator { get; set; }

        internal CommandQueue NativeCommandQueue { get; }

        internal CommandQueue NativeCopyCommandQueue { get; }

        internal Queue<CommandList> CopyCommandLists { get; } = new Queue<CommandList>();

        internal Device NativeDevice { get; }

        internal SharpDX.Direct3D11.Device NativeDirect3D11Device { get; }

        internal Fence NativeCopyFence { get; }

        internal Fence NativeFence { get; }

        internal long NextCopyFenceValue { get; private set; } = 1;

        internal long NextFenceValue { get; private set; } = 1;

        public void CopyDescriptors(int numDestDescriptorRanges, CpuDescriptorHandle[] destDescriptorRangeStartsRef, int[] destDescriptorRangeSizesRef, int numSrcDescriptorRanges, CpuDescriptorHandle[] srcDescriptorRangeStartsRef, int[] srcDescriptorRangeSizesRef, DescriptorHeapType descriptorHeapsType)
        {
            NativeDevice.CopyDescriptors(numDestDescriptorRanges, destDescriptorRangeStartsRef, destDescriptorRangeSizesRef, numSrcDescriptorRanges, srcDescriptorRangeStartsRef, srcDescriptorRangeSizesRef, descriptorHeapsType);
        }

        public (CpuDescriptorHandle, GpuDescriptorHandle) CopyDescriptorsToOneDescriptorHandle(IEnumerable<GraphicsResource> resources)
        {
            return CopyDescriptorsToOneDescriptorHandle(resources.Select(t => t.NativeCpuDescriptorHandle).ToArray());
        }

        public (CpuDescriptorHandle, GpuDescriptorHandle) CopyDescriptorsToOneDescriptorHandle(CpuDescriptorHandle[] descriptors)
        {
            if (descriptors.Length == 0) return default;

            int[] srcDescriptorRangeStarts = new int[descriptors.Length];
            //Array.Fill(srcDescriptorRangeStarts, 1);

            for (int i = 0; i < srcDescriptorRangeStarts.Length; i++)
            {
                srcDescriptorRangeStarts[i] = 1;
            }

            var (cpuDescriptorHandle, gpuDescriptorHandle) = ShaderResourceViewAllocator.Allocate(descriptors.Length);

            CopyDescriptors(
                1, new[] { cpuDescriptorHandle }, new[] { descriptors.Length },
                descriptors.Length, descriptors, srcDescriptorRangeStarts,
                DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            return (cpuDescriptorHandle, gpuDescriptorHandle);
        }

        public RootSignature CreateRootSignature(RootSignatureDescription rootSignatureDescription)
        {
            return NativeDevice.CreateRootSignature(rootSignatureDescription.Serialize());
        }

        public RootSignature CreateRootSignature(byte[] bytecode)
        {
            return NativeDevice.CreateRootSignature(bytecode);
        }

        public void Dispose()
        {
            NativeCommandQueue.Signal(NativeFence, NextFenceValue);
            NativeCommandQueue.Wait(NativeFence, NextFenceValue);

            CommandList.Dispose();
            BundleAllocatorPool.Dispose();
            DirectAllocatorPool.Dispose();
            NativeCopyCommandQueue.Dispose();
            NativeCommandQueue.Dispose();
            NativeFence.Dispose();
            Presenter?.Dispose();
            DepthStencilViewAllocator.Dispose();
            ShaderResourceViewAllocator.Dispose();
            RenderTargetViewAllocator.Dispose();

            foreach (CommandList commandList in CopyCommandLists)
            {
                commandList.Dispose();
            }

            foreach (IDisposable disposable in Disposables)
            {
                disposable.Dispose();
            }

            NativeDevice.Dispose();
        }

        public void ExecuteCommandLists(bool wait, params CompiledCommandList[] commandLists)
        {
            Fence fence = commandLists[0].NativeCommandList.TypeInfo switch
            {
                CommandListType.Direct => NativeFence,
                CommandListType.Copy => NativeCopyFence,
                _ => throw new NotSupportedException("This command list type is not supported.")
            };

            long fenceValue = ExecuteCommandLists(commandLists);

            if (wait)
            {
                WaitForFence(fence, fenceValue);
            }
        }

        public long ExecuteCommandLists(params CompiledCommandList[] commandLists)
        {
            CommandAllocatorPool commandAllocatorPool;
            CommandQueue commandQueue;
            Fence fence;
            long fenceValue;

            switch (commandLists[0].NativeCommandList.TypeInfo)
            {
                case CommandListType.Direct:
                    commandAllocatorPool = DirectAllocatorPool;
                    commandQueue = NativeCommandQueue;

                    fence = NativeFence;
                    fenceValue = NextFenceValue;
                    NextFenceValue++;
                    break;
                case CommandListType.Copy:
                    commandAllocatorPool = CopyAllocatorPool;
                    commandQueue = NativeCopyCommandQueue;

                    fence = NativeCopyFence;
                    fenceValue = NextCopyFenceValue;
                    NextCopyFenceValue++;
                    break;
                default:
                    throw new NotSupportedException("This command list type is not supported.");
            }

            SharpDX.Direct3D12.CommandList[] nativeCommandLists = new SharpDX.Direct3D12.CommandList[commandLists.Length];

            for (int i = 0; i < commandLists.Length; i++)
            {
                nativeCommandLists[i] = commandLists[i].NativeCommandList;
                commandAllocatorPool.Enqueue(commandLists[i].NativeCommandAllocator, fenceValue);
            }

            commandQueue.ExecuteCommandLists(nativeCommandLists);
            commandQueue.Signal(fence, fenceValue);

            return fenceValue;
        }

        internal CommandList GetOrCreateCopyCommandList()
        {
            CommandList commandList;

            lock (CopyCommandLists)
            {
                if (CopyCommandLists.Count > 0)
                {
                    commandList = CopyCommandLists.Dequeue();
                    commandList.Reset();
                }
                else
                {
                    commandList = new CommandList(this, CommandListType.Copy);
                }
            }

            return commandList;
        }

        internal bool IsFenceComplete(Fence fence, long fenceValue)
        {
            return fenceValue <= fence.CompletedValue;
        }

        internal void WaitForFence(Fence fence, long fenceValue)
        {
            if (IsFenceComplete(fence, fenceValue)) return;

            lock (fence)
            {
                fence.SetEventOnCompletion(fenceValue, fenceEvent.SafeWaitHandle.DangerousGetHandle());
                fenceEvent.WaitOne();
            }
        }
    }

    internal class D3DXUtilities
    {
        public const int ComponentMappingMask = 0x7;

        public const int ComponentMappingShift = 3;

        public const int ComponentMappingAlwaysSetBitAvoidingZeromemMistakes = 1 << (ComponentMappingShift * 4);

        public static int ComponentMapping(int src0, int src1, int src2, int src3)
        {
            return ((src0) & ComponentMappingMask)
                | (((src1) & ComponentMappingMask) << ComponentMappingShift)
                | (((src2) & ComponentMappingMask) << (ComponentMappingShift * 2))
                | (((src3) & ComponentMappingMask) << (ComponentMappingShift * 3))
                | ComponentMappingAlwaysSetBitAvoidingZeromemMistakes;
        }

        public static int DefaultComponentMapping()
        {
            return ComponentMapping(0, 1, 2, 3);
        }

        public static int ComponentMapping(int ComponentToExtract, int Mapping)
        {
            return (Mapping >> (ComponentMappingShift * ComponentToExtract)) & ComponentMappingMask;
        }
    }
}
