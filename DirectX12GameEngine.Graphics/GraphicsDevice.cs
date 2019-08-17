using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirectX12GameEngine.Core;
using Nito.AsyncEx.Interop;
using SharpGen.Runtime;
using Vortice.DirectX.Direct3D;
using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class GraphicsDevice : IDisposable, ICollector
    {
        private readonly AutoResetEvent fenceEvent = new AutoResetEvent(false);
        private Vortice.DirectX.Direct3D11.ID3D11Device? nativeDirect3D11Device;

        public GraphicsDevice(FeatureLevel minFeatureLevel = FeatureLevel.Level_11_0, bool enableDebugLayer = false)
        {
#if DEBUG
            if (enableDebugLayer)
            {
            }
#endif
            FeatureLevel = minFeatureLevel < FeatureLevel.Level_11_0 ? FeatureLevel.Level_11_0 : minFeatureLevel;

            Result result = D3D12.D3D12CreateDevice(null, (Vortice.DirectX.Direct3D.FeatureLevel)FeatureLevel, out ID3D12Device device);
            NativeDevice = device;

            NativeComputeCommandQueue = NativeDevice.CreateCommandQueue(new CommandQueueDescription(Vortice.DirectX.Direct3D12.CommandListType.Compute));
            NativeCopyCommandQueue = NativeDevice.CreateCommandQueue(new CommandQueueDescription(Vortice.DirectX.Direct3D12.CommandListType.Copy));
            NativeDirectCommandQueue = NativeDevice.CreateCommandQueue(new CommandQueueDescription(Vortice.DirectX.Direct3D12.CommandListType.Direct));

            BundleAllocatorPool = new CommandAllocatorPool(this, CommandListType.Bundle);
            ComputeAllocatorPool = new CommandAllocatorPool(this, CommandListType.Compute);
            CopyAllocatorPool = new CommandAllocatorPool(this, CommandListType.Copy);
            DirectAllocatorPool = new CommandAllocatorPool(this, CommandListType.Direct);

            NativeComputeFence = NativeDevice.CreateFence(0, FenceFlags.None);
            NativeCopyFence = NativeDevice.CreateFence(0, FenceFlags.None);
            NativeDirectFence = NativeDevice.CreateFence(0, FenceFlags.None);

            DepthStencilViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.DepthStencilView, 1);
            RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.RenderTargetView, 2);
            ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 4096, DescriptorHeapFlags.ShaderVisible);
            SamplerAllocator = new DescriptorAllocator(this, DescriptorHeapType.Sampler, 256, DescriptorHeapFlags.ShaderVisible);

            CommandList = new CommandList(this, CommandListType.Direct);
            CommandList.Close();

            CopyCommandList = new CommandList(this, CommandListType.Copy);
            CopyCommandList.Close();
        }

        public CommandList CommandList { get; }

        public CommandList CopyCommandList { get; }

        public ICollection<IDisposable> Disposables { get; } = new List<IDisposable>();

        public FeatureLevel FeatureLevel { get; }

        public GraphicsPresenter? Presenter { get; set; }

        internal ID3D12Device NativeDevice { get; }

        internal Vortice.DirectX.Direct3D11.ID3D11Device NativeDirect3D11Device
        {
            get
            {
                if (nativeDirect3D11Device is null)
                {
                    Vortice.DirectX.Direct3D11.D3D11.D3D11On12CreateDevice(
                        NativeDevice, Vortice.DirectX.Direct3D11.DeviceCreationFlags.BgraSupport, null, new[] { NativeDirectCommandQueue }, 0,
                        out nativeDirect3D11Device, out _, out _);
                }

                return nativeDirect3D11Device;
            }
        }

        internal DescriptorAllocator DepthStencilViewAllocator { get; set; }

        internal DescriptorAllocator RenderTargetViewAllocator { get; set; }

        internal DescriptorAllocator ShaderResourceViewAllocator { get; set; }

        internal DescriptorAllocator SamplerAllocator { get; set; }


        internal CommandAllocatorPool BundleAllocatorPool { get; }

        internal CommandAllocatorPool ComputeAllocatorPool { get; }

        internal CommandAllocatorPool CopyAllocatorPool { get; }

        internal CommandAllocatorPool DirectAllocatorPool { get; }


        internal ID3D12CommandQueue NativeComputeCommandQueue { get; }

        internal ID3D12CommandQueue NativeCopyCommandQueue { get; }

        internal ID3D12CommandQueue NativeDirectCommandQueue { get; }


        internal ID3D12Fence NativeComputeFence { get; }

        internal ID3D12Fence NativeCopyFence { get; }

        internal ID3D12Fence NativeDirectFence { get; }


        internal ulong NextComputeFenceValue { get; private set; } = 1;

        internal ulong NextCopyFenceValue { get; private set; } = 1;

        internal ulong NextDirectFenceValue { get; private set; } = 1;

        public ID3D12RootSignature CreateRootSignature(VersionedRootSignatureDescription rootSignatureDescription)
        {
            return NativeDevice.CreateRootSignature(rootSignatureDescription);
        }

        public void Dispose()
        {
            NativeDirectCommandQueue.Signal(NativeDirectFence, NextDirectFenceValue);
            NativeDirectCommandQueue.Wait(NativeDirectFence, NextDirectFenceValue);

            CommandList.Dispose();
            CopyCommandList.Dispose();

            DepthStencilViewAllocator.Dispose();
            RenderTargetViewAllocator.Dispose();
            ShaderResourceViewAllocator.Dispose();

            BundleAllocatorPool.Dispose();
            ComputeAllocatorPool.Dispose();
            CopyAllocatorPool.Dispose();
            DirectAllocatorPool.Dispose();

            NativeComputeCommandQueue.Dispose();
            NativeCopyCommandQueue.Dispose();
            NativeDirectCommandQueue.Dispose();

            NativeComputeFence.Dispose();
            NativeDirectFence.Dispose();
            NativeDirectFence.Dispose();

            foreach (IDisposable disposable in Disposables)
            {
                disposable.Dispose();
            }

            fenceEvent.Dispose();

            nativeDirect3D11Device?.Dispose();

            NativeDevice.Dispose();
        }

        public Task ExecuteCommandListsAsync(params CompiledCommandList[] commandLists)
        {
            ID3D12Fence fence = commandLists[0].Builder.CommandListType switch
            {
                CommandListType.Direct => NativeDirectFence,
                CommandListType.Compute => NativeComputeFence,
                CommandListType.Copy => NativeCopyFence,
                _ => throw new NotSupportedException("This command list type is not supported.")
            };

            ulong fenceValue = ExecuteCommandLists(commandLists);

            return WaitForFenceAsync(fence, fenceValue);
        }

        public ulong ExecuteCommandLists(params CompiledCommandList[] commandLists)
        {
            CommandAllocatorPool commandAllocatorPool;
            ID3D12CommandQueue commandQueue;
            ID3D12Fence fence;
            ulong fenceValue;

            switch (commandLists[0].Builder.CommandListType)
            {
                case CommandListType.Compute:
                    commandAllocatorPool = ComputeAllocatorPool;
                    commandQueue = NativeComputeCommandQueue;

                    fence = NativeComputeFence;
                    fenceValue = NextComputeFenceValue;
                    NextComputeFenceValue++;
                    break;
                case CommandListType.Copy:
                    commandAllocatorPool = CopyAllocatorPool;
                    commandQueue = NativeCopyCommandQueue;

                    fence = NativeCopyFence;
                    fenceValue = NextCopyFenceValue;
                    NextCopyFenceValue++;
                    break;
                case CommandListType.Direct:
                    commandAllocatorPool = DirectAllocatorPool;
                    commandQueue = NativeDirectCommandQueue;

                    fence = NativeDirectFence;
                    fenceValue = NextDirectFenceValue;
                    NextDirectFenceValue++;
                    break;
                default:
                    throw new NotSupportedException("This command list type is not supported.");
            }

            ID3D12CommandList[] nativeCommandLists = new ID3D12CommandList[commandLists.Length];

            for (int i = 0; i < commandLists.Length; i++)
            {
                nativeCommandLists[i] = commandLists[i].NativeCommandList;
                commandAllocatorPool.Enqueue(commandLists[i].NativeCommandAllocator, fenceValue);
            }

            commandQueue.ExecuteCommandLists(nativeCommandLists);
            commandQueue.Signal(fence, fenceValue);

            return fenceValue;
        }

        internal bool IsFenceComplete(ID3D12Fence fence, ulong fenceValue)
        {
            return fence.CompletedValue >= fenceValue;
        }

        internal Task WaitForFenceAsync(ID3D12Fence fence, ulong fenceValue)
        {
            if (IsFenceComplete(fence, fenceValue)) return Task.CompletedTask;

            lock (fence)
            {
                fence.SetEventOnCompletion(fenceValue, fenceEvent.SafeWaitHandle.DangerousGetHandle());

                return WaitHandleAsyncFactory.FromWaitHandle(fenceEvent);
            }
        }
    }
}
