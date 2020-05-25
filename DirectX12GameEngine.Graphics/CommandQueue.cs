using Nito.AsyncEx.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class CommandQueue : IDisposable
    {
        public CommandQueue(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            NativeCommandQueue = GraphicsDevice.NativeDevice.CreateCommandQueue(new CommandQueueDescription((Vortice.Direct3D12.CommandListType)commandListType));
            Fence = GraphicsDevice.NativeDevice.CreateFence(0);
        }

        public GraphicsDevice GraphicsDevice { get; }

        internal ID3D12CommandQueue NativeCommandQueue { get; }

        internal ID3D12Fence Fence { get; }

        internal long NextFenceValue { get; private set; } = 1;

        public void Dispose()
        {
            NativeCommandQueue.Dispose();
            Fence.Dispose();
        }

        public void ExecuteCommandLists(params CompiledCommandList[] commandLists)
        {
            ExecuteCommandLists(commandLists.AsEnumerable());
        }

        public void ExecuteCommandLists(IEnumerable<CompiledCommandList> commandLists)
        {
            if (commandLists.Count() == 0) return;

            long fenceValue = ExecuteCommandListsInternal(commandLists);

            WaitForFence(Fence, fenceValue);
        }

        public Task ExecuteCommandListsAsync(params CompiledCommandList[] commandLists)
        {
            return ExecuteCommandListsAsync(commandLists.AsEnumerable());
        }

        public Task ExecuteCommandListsAsync(IEnumerable<CompiledCommandList> commandLists)
        {
            if (commandLists.Count() == 0) return Task.CompletedTask;

            long fenceValue = ExecuteCommandListsInternal(commandLists);

            return WaitForFenceAsync(Fence, fenceValue);
        }

        private long ExecuteCommandListsInternal(IEnumerable<CompiledCommandList> commandLists)
        {
            long fenceValue = NextFenceValue++;
            ID3D12CommandList[] nativeCommandLists = commandLists.Select(c => c.NativeCommandList).ToArray();

            NativeCommandQueue.ExecuteCommandLists(nativeCommandLists);
            NativeCommandQueue.Signal(Fence, fenceValue);

            return fenceValue;
        }

        internal bool IsFenceComplete(ID3D12Fence fence, long fenceValue)
        {
            return fence.CompletedValue >= fenceValue;
        }

        internal void WaitForFence(ID3D12Fence fence, long fenceValue)
        {
            if (IsFenceComplete(fence, fenceValue)) return;

            using ManualResetEvent fenceEvent = new ManualResetEvent(false);
            fence.SetEventOnCompletion(fenceValue, fenceEvent);

            fenceEvent.WaitOne();
        }

        internal Task WaitForFenceAsync(ID3D12Fence fence, long fenceValue)
        {
            if (IsFenceComplete(fence, fenceValue)) return Task.CompletedTask;

            ManualResetEvent fenceEvent = new ManualResetEvent(false);
            fence.SetEventOnCompletion(fenceValue, fenceEvent);

            return WaitHandleAsyncFactory.FromWaitHandle(fenceEvent);
        }
    }
}
