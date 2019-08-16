﻿using System;
using System.Collections.Generic;
using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    internal sealed class CommandAllocatorPool : IDisposable
    {
        private readonly Queue<(ID3D12CommandAllocator, ulong)> commandAllocatorQueue = new Queue<(ID3D12CommandAllocator, ulong)>();

        public CommandAllocatorPool(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            CommandListType = commandListType;
        }

        public CommandListType CommandListType { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public void Dispose()
        {
            lock (commandAllocatorQueue)
            {
                foreach ((ID3D12CommandAllocator commandAllocator, ulong _) in commandAllocatorQueue)
                {
                    commandAllocator.Dispose();
                }

                commandAllocatorQueue.Clear();
            }
        }

        public void Enqueue(ID3D12CommandAllocator commandAllocator, ulong fenceValue)
        {
            lock (commandAllocatorQueue)
            {
                commandAllocatorQueue.Enqueue((commandAllocator, fenceValue));
            }
        }

        public ID3D12CommandAllocator GetCommandAllocator()
        {
            lock (commandAllocatorQueue)
            {
                if (commandAllocatorQueue.Count > 0)
                {
                    (ID3D12CommandAllocator commandAllocator, ulong fenceValue) = commandAllocatorQueue.Peek();

                    ulong completedValue = CommandListType switch
                    {
                        CommandListType.Bundle => GraphicsDevice.NativeDirectFence.CompletedValue,
                        CommandListType.Compute => GraphicsDevice.NativeComputeFence.CompletedValue,
                        CommandListType.Copy => GraphicsDevice.NativeCopyFence.CompletedValue,
                        CommandListType.Direct => GraphicsDevice.NativeDirectFence.CompletedValue,
                        _ => throw new NotSupportedException("This command list type is not supported.")
                    };

                    if (fenceValue <= completedValue)
                    {
                        commandAllocatorQueue.Dequeue();
                        commandAllocator.Reset();

                        return commandAllocator;
                    }
                }

                return GraphicsDevice.NativeDevice.CreateCommandAllocator((Vortice.DirectX.Direct3D12.CommandListType)CommandListType);
            }
        }
    }
}
