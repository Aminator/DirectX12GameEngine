using System;
using SharpDX.Direct3D12;
using SharpDX.Mathematics.Interop;

namespace DirectX12GameEngine
{
    public sealed class CommandList : IDisposable
    {
        private const int MaxRenderTargetCount = 8;

        private CompiledCommandList currentCommandList;

        public CommandList(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            CommandListType = commandListType;

            currentCommandList = new CompiledCommandList(this, null!, null!);

            Reset();
        }

        public CommandListType CommandListType { get; }

        public Texture? DepthStencilBuffer { get; private set; }

        public GraphicsDevice GraphicsDevice { get; }

        public int RenderTargetCount { get; private set; }

        public Texture[] RenderTargets { get; } = new Texture[MaxRenderTargetCount];

        public void Clear(Texture depthStencilBuffer, ClearFlags clearFlags, float depth = 1, byte stencil = 0)
        {
            currentCommandList.NativeCommandList.ClearDepthStencilView(depthStencilBuffer.NativeCpuDescriptorHandle, clearFlags, depth, stencil);
        }

        public void Clear(Texture renderTarget, RawColor4 color)
        {
            currentCommandList.NativeCommandList.ClearRenderTargetView(renderTarget.NativeCpuDescriptorHandle, color);
        }

        public CompiledCommandList Close()
        {
            for (int i = 0; i < RenderTargetCount; i++)
            {
                ResourceBarrierTransition(RenderTargets[i], ResourceStates.RenderTarget, ResourceStates.Present);
            }

            currentCommandList.NativeCommandList.Close();

            return currentCommandList;
        }

        public void CopyBufferRegion(Texture destination, long destinationOffset, Texture source, long sourceOffset, long? numBytes = null)
        {
            currentCommandList.NativeCommandList.CopyBufferRegion(destination.NativeResource, 0, source.NativeResource, 0, numBytes ?? source.Width * source.Height);
        }

        public void CopyResource(Texture destination, Texture source)
        {
            currentCommandList.NativeCommandList.CopyResource(destination.NativeResource, source.NativeResource);
        }

        public void CopyTextureRegion(TextureCopyLocation destination, TextureCopyLocation source)
        {
            currentCommandList.NativeCommandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
        }

        public void Dispose()
        {
            switch (CommandListType)
            {
                case CommandListType.Direct:
                    GraphicsDevice.DirectAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextFenceValue - 1);
                    break;
                case CommandListType.Bundle:
                    GraphicsDevice.BundleAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextFenceValue - 1);
                    break;
                case CommandListType.Copy:
                    GraphicsDevice.CopyAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextCopyFenceValue - 1);
                    break;
                default:
                    throw new ArgumentException("This command list type is not supported.");
            }

            currentCommandList.NativeCommandList.Dispose();
        }

        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            currentCommandList.NativeCommandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation = 0, int startInstanceLocation = 0)
        {
            currentCommandList.NativeCommandList.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
        }

        public void ExecuteBundle(CompiledCommandList commandList)
        {
            if (currentCommandList != commandList && commandList.Builder.CommandListType == CommandListType.Bundle)
            {
                currentCommandList.NativeCommandList.ExecuteBundle(commandList.NativeCommandList);
            }
        }

        public void Flush(bool wait = false)
        {
            GraphicsDevice.ExecuteCommandLists(wait, Close());
        }

        public void Reset()
        {
            CommandAllocator commandAllocator;

            switch (CommandListType)
            {
                case CommandListType.Direct:
                    commandAllocator = GraphicsDevice.DirectAllocatorPool.GetCommandAllocator();
                    break;
                case CommandListType.Bundle:
                    commandAllocator = GraphicsDevice.BundleAllocatorPool.GetCommandAllocator();
                    break;
                case CommandListType.Copy:
                    commandAllocator = GraphicsDevice.CopyAllocatorPool.GetCommandAllocator();
                    break;
                default:
                    throw new ArgumentException("This command list type is not supported.");
            }

            if (currentCommandList.NativeCommandList is null)
            {
                GraphicsCommandList nativeCommandList = GraphicsDevice.NativeDevice.CreateCommandList(CommandListType, commandAllocator, null);
                currentCommandList = new CompiledCommandList(this, commandAllocator, nativeCommandList);
            }
            else
            {
                currentCommandList.NativeCommandAllocator = commandAllocator;
                currentCommandList.NativeCommandList.Reset(currentCommandList.NativeCommandAllocator, null);
            }

            if (CommandListType != CommandListType.Copy)
            {
                SetDescriptorHeaps(GraphicsDevice.ShaderResourceViewAllocator.DescriptorHeap);
            }
        }

        public void ResourceBarrierTransition(Texture resource, ResourceStates stateBefore, ResourceStates stateAfter)
        {
            currentCommandList.NativeCommandList.ResourceBarrierTransition(resource.NativeResource, stateBefore, stateAfter);
        }

        public void SetDescriptorHeaps(params DescriptorHeap[] descriptorHeaps)
        {
            currentCommandList.NativeCommandList.SetDescriptorHeaps(descriptorHeaps);
        }

        public void SetGraphicsRootDescriptorTable(int rootParameterIndex, GpuDescriptorHandle baseDescriptor)
        {
            currentCommandList.NativeCommandList.SetGraphicsRootDescriptorTable(rootParameterIndex, baseDescriptor);
        }

        public void SetGraphicsRootSignature(RootSignature rootSignature)
        {
            currentCommandList.NativeCommandList.SetGraphicsRootSignature(rootSignature);
        }

        public void SetIndexBuffer(IndexBufferView? indexBufferView)
        {
            currentCommandList.NativeCommandList.SetIndexBuffer(indexBufferView);
        }

        public void SetPipelineState(GraphicsPipelineState pipelineState)
        {
            SetGraphicsRootSignature(pipelineState.RootSignature);

            currentCommandList.NativeCommandList.PipelineState = pipelineState.NativePipelineState;
            currentCommandList.NativeCommandList.PrimitiveTopology = pipelineState.PrimitiveTopology;
        }

        public void SetRenderTargets(Texture depthStencilView, params Texture[] renderTargetViews)
        {
            DepthStencilBuffer = depthStencilView;

            if (RenderTargetCount != renderTargetViews.Length)
            {
                RenderTargetCount = renderTargetViews.Length;
                Array.Clear(RenderTargets, 0, RenderTargetCount);
            }

            renderTargetViews.CopyTo(RenderTargets, 0);

            CpuDescriptorHandle[] renderTargetDescriptors = new CpuDescriptorHandle[RenderTargetCount];

            for (int i = 0; i < RenderTargetCount; i++)
            {
                ResourceBarrierTransition(renderTargetViews[i], ResourceStates.Present, ResourceStates.RenderTarget);
                renderTargetDescriptors[i] = renderTargetViews[i].NativeCpuDescriptorHandle;
            }

            currentCommandList.NativeCommandList.SetRenderTargets(renderTargetDescriptors, depthStencilView?.NativeCpuDescriptorHandle);
        }

        public void SetScissorRectangles(RawRectangle scissorRect)
        {
            currentCommandList.NativeCommandList.SetScissorRectangles(scissorRect);
        }

        public void SetVertexBuffers(params VertexBufferView[] vertexBufferViews)
        {
            currentCommandList.NativeCommandList.SetVertexBuffers(0, vertexBufferViews);
        }

        public void SetViewport(RawViewportF viewport)
        {
            currentCommandList.NativeCommandList.SetViewport(viewport);
        }
    }
}
