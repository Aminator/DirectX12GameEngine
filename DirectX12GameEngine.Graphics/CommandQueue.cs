using Nito.AsyncEx.Interop;
using System;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class CommandQueue : IDisposable
    {
        private readonly AutoResetEvent fenceEvent = new AutoResetEvent(false);
        private readonly object fenceLock = new object();

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
            long fenceValue = ExecuteCommandListsInternal(commandLists);

            WaitForFence(Fence, fenceValue);
        }

        public Task ExecuteCommandListsAsync(params CompiledCommandList[] commandLists)
        {
            long fenceValue = ExecuteCommandListsInternal(commandLists);

            return WaitForFenceAsync(Fence, fenceValue);
        }

        private long ExecuteCommandListsInternal(CompiledCommandList[] commandLists)
        {
            long fenceValue = NextFenceValue++;
            ID3D12CommandList[] nativeCommandLists = new ID3D12CommandList[commandLists.Length];

            for (int i = 0; i < commandLists.Length; i++)
            {
                nativeCommandLists[i] = commandLists[i].NativeCommandList;
            }

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

            lock (fenceLock)
            {
                fence.SetEventOnCompletion(fenceValue, fenceEvent);

                fenceEvent.WaitOne();
            }
        }

        internal Task WaitForFenceAsync(ID3D12Fence fence, long fenceValue)
        {
            if (IsFenceComplete(fence, fenceValue)) return Task.CompletedTask;

            lock (fenceLock)
            {
                fence.SetEventOnCompletion(fenceValue, fenceEvent);

                return WaitHandleAsyncFactory.FromWaitHandle(fenceEvent);
            }
        }
    }
}
