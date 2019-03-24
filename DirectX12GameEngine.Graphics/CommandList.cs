using System;
using SharpDX.Direct3D12;
using SharpDX.Mathematics.Interop;

namespace DirectX12GameEngine.Graphics
{
    public sealed class CommandList : IDisposable
    {
        private const int MaxRenderTargetCount = 8;

        private readonly CompiledCommandList currentCommandList;

        public CommandList(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            CommandListType = commandListType;

            CommandAllocator commandAllocator = GetCommandAllocator();

            GraphicsCommandList nativeCommandList = GraphicsDevice.NativeDevice.CreateCommandList(CommandListType, commandAllocator, null);
            currentCommandList = new CompiledCommandList(this, commandAllocator, nativeCommandList);

            SetDescriptorHeaps(GraphicsDevice.ShaderResourceViewAllocator.DescriptorHeap);
        }

        public CommandListType CommandListType { get; }

        public Texture? DepthStencilBuffer { get; private set; }

        public GraphicsDevice GraphicsDevice { get; }

        public Texture[] RenderTargets { get; private set; } = Array.Empty<Texture>();

        public void BeginRenderPass()
        {
            BeginRenderPass(DepthStencilBuffer, RenderTargets);
        }

        public void BeginRenderPass(Texture? depthStencilView, params Texture[] renderTargetViews)
        {
            RenderPassRenderTargetDescription[] renderPassRenderTargetDescriptions = new RenderPassRenderTargetDescription[renderTargetViews.Length];

            RenderPassBeginningAccess renderPassBeginningAccessPreserve = new RenderPassBeginningAccess { Type = RenderPassBeginningAccessType.Preserve };
            RenderPassEndingAccess renderPassEndingAccessPreserve = new RenderPassEndingAccess { Type = RenderPassEndingAccessType.Preserve };

            for (int i = 0; i < renderTargetViews.Length; i++)
            {
                RenderPassRenderTargetDescription renderPassRenderTargetDescription = new RenderPassRenderTargetDescription
                {
                    BeginningAccess = renderPassBeginningAccessPreserve,
                    EndingAccess = renderPassEndingAccessPreserve,
                    CpuDescriptor = renderTargetViews[i].NativeCpuDescriptorHandle
                };

                renderPassRenderTargetDescriptions[i] = renderPassRenderTargetDescription;
            }

            RenderPassBeginningAccess renderPassBeginningAccessNoAccess = new RenderPassBeginningAccess { Type = RenderPassBeginningAccessType.NoAccess };
            RenderPassEndingAccess renderPassEndingAccessNoAccess = new RenderPassEndingAccess { Type = RenderPassEndingAccessType.NoAccess };

            RenderPassDepthStencilDescription? renderPassDepthStencilDescription = null;

            if (depthStencilView != null)
            {
                renderPassDepthStencilDescription = new RenderPassDepthStencilDescription
                {
                    DepthBeginningAccess = renderPassBeginningAccessNoAccess,
                    DepthEndingAccess = renderPassEndingAccessNoAccess,
                    StencilBeginningAccess = renderPassBeginningAccessNoAccess,
                    StencilEndingAccess = renderPassEndingAccessNoAccess,
                    CpuDescriptor = depthStencilView.NativeCpuDescriptorHandle,
                };
            }

            BeginRenderPass(renderTargetViews.Length, renderPassRenderTargetDescriptions, renderPassDepthStencilDescription, RenderPassFlags.None);
        }

        public void BeginRenderPass(int numRenderTargets, RenderPassRenderTargetDescription[] renderTargetsRef, RenderPassDepthStencilDescription? depthStencilRef, RenderPassFlags flags)
        {
            using GraphicsCommandList4 commandList = currentCommandList.NativeCommandList.QueryInterface<GraphicsCommandList4>();
            commandList.BeginRenderPass(numRenderTargets, renderTargetsRef, depthStencilRef, flags);
        }

        public void EndRenderPass()
        {
            using GraphicsCommandList4 commandList = currentCommandList.NativeCommandList.QueryInterface<GraphicsCommandList4>();
            commandList.EndRenderPass();
        }

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
            foreach (var renderTarget in RenderTargets)
            {
                ResourceBarrierTransition(renderTarget, ResourceStates.RenderTarget, ResourceStates.Present);
            }

            currentCommandList.NativeCommandList.Close();

            return currentCommandList;
        }

        public void CopyBufferRegion(Texture source, long sourceOffset, Texture destination, long destinationOffset, long? numBytes = null)
        {
            currentCommandList.NativeCommandList.CopyBufferRegion(destination.NativeResource, destinationOffset, source.NativeResource, sourceOffset, numBytes ?? source.Width * source.Height);
        }

        public void CopyResource(Texture source, Texture destination)
        {
            currentCommandList.NativeCommandList.CopyResource(destination.NativeResource, source.NativeResource);
        }

        public void CopyTextureRegion(TextureCopyLocation source, TextureCopyLocation destination)
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
                    throw new NotSupportedException("This command list type is not supported.");
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
            CommandAllocator commandAllocator = GetCommandAllocator();

            currentCommandList.NativeCommandAllocator = commandAllocator;
            currentCommandList.NativeCommandList.Reset(currentCommandList.NativeCommandAllocator, null);

            SetDescriptorHeaps(GraphicsDevice.ShaderResourceViewAllocator.DescriptorHeap);
        }

        public void ResourceBarrierTransition(Texture resource, ResourceStates stateBefore, ResourceStates stateAfter)
        {
            currentCommandList.NativeCommandList.ResourceBarrierTransition(resource.NativeResource, stateBefore, stateAfter);
        }

        public void SetDescriptorHeaps(params DescriptorHeap[] descriptorHeaps)
        {
            if (CommandListType != CommandListType.Copy)
            {
                currentCommandList.NativeCommandList.SetDescriptorHeaps(descriptorHeaps);
            }
        }

        public void SetGraphicsRoot32BitConstant(int rootParameterIndex, int srcData, int destOffsetIn32BitValues)
        {
            currentCommandList.NativeCommandList.SetGraphicsRoot32BitConstant(rootParameterIndex, srcData, destOffsetIn32BitValues);
        }

        public void SetGraphicsRootDescriptorTable(int rootParameterIndex, Texture texture)
        {
            currentCommandList.NativeCommandList.SetGraphicsRootDescriptorTable(rootParameterIndex, texture.NativeGpuDescriptorHandle);
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

        public void SetPipelineState(PipelineState pipelineState)
        {
            SetGraphicsRootSignature(pipelineState.RootSignature);

            currentCommandList.NativeCommandList.PipelineState = pipelineState.NativePipelineState;
            currentCommandList.NativeCommandList.PrimitiveTopology = pipelineState.PrimitiveTopology;
        }

        public void SetRenderTargets(Texture? depthStencilView, params Texture[] renderTargetViews)
        {
            DepthStencilBuffer = depthStencilView;

            if (renderTargetViews.Length > MaxRenderTargetCount)
            {
                throw new ArgumentOutOfRangeException(nameof(renderTargetViews), renderTargetViews.Length, $"The maximum number of render targets is {MaxRenderTargetCount}.");
            }

            if (RenderTargets.Length != renderTargetViews.Length)
            {
                RenderTargets = new Texture[renderTargetViews.Length];
            }

            renderTargetViews.CopyTo(RenderTargets, 0);

            CpuDescriptorHandle[] renderTargetDescriptors = new CpuDescriptorHandle[renderTargetViews.Length];

            for (int i = 0; i < renderTargetViews.Length; i++)
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

        private CommandAllocator GetCommandAllocator()
        {
            return CommandListType switch
            {
                CommandListType.Direct => GraphicsDevice.DirectAllocatorPool.GetCommandAllocator(),
                CommandListType.Bundle => GraphicsDevice.BundleAllocatorPool.GetCommandAllocator(),
                CommandListType.Copy => GraphicsDevice.CopyAllocatorPool.GetCommandAllocator(),
                _ => throw new NotSupportedException("This command list type is not supported.")
            };
        }
    }
}
