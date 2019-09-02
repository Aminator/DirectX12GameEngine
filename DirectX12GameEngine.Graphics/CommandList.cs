using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace DirectX12GameEngine.Graphics
{
    public sealed class CommandList : IDisposable
    {
        private const int MaxRenderTargetCount = 8;
        private const int MaxViewportAndScissorRectangleCount = 16;

        private readonly CompiledCommandList currentCommandList;

        private DescriptorAllocator? srvDescriptorHeap;
        private DescriptorAllocator? samplerDescriptorHeap;

        public CommandList(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            CommandListType = commandListType;

            ID3D12CommandAllocator commandAllocator = GetCommandAllocator();

            ID3D12GraphicsCommandList nativeCommandList = GraphicsDevice.NativeDevice.CreateCommandList((Vortice.Direct3D12.CommandListType)CommandListType, commandAllocator, null);
            currentCommandList = new CompiledCommandList(this, commandAllocator, nativeCommandList);

            SetDescriptorHeaps(GraphicsDevice.ShaderVisibleShaderResourceViewAllocator, GraphicsDevice.ShaderVisibleSamplerAllocator);
        }

        public CommandListType CommandListType { get; }

        public Texture? DepthStencilBuffer { get; private set; }

        public GraphicsDevice GraphicsDevice { get; }

        public GraphicsResource[] RenderTargets { get; private set; } = Array.Empty<GraphicsResource>();

        public Rectangle[] ScissorRectangles { get; private set; } = Array.Empty<Rectangle>();

        public RectangleF[] Viewports { get; private set; } = Array.Empty<RectangleF>();

        public void BeginRenderPass()
        {
            BeginRenderPass(DepthStencilBuffer, RenderTargets);
        }

        public void BeginRenderPass(Texture? depthStencilView, params GraphicsResource[] renderTargetViews)
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
                    CpuDescriptor = renderTargetViews[i].NativeRenderTargetView
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
                    CpuDescriptor = depthStencilView.NativeDepthStencilView,
                };
            }

            BeginRenderPass(renderTargetViews.Length, renderPassRenderTargetDescriptions, renderPassDepthStencilDescription, RenderPassFlags.None);
        }

        public void BeginRenderPass(int numRenderTargets, RenderPassRenderTargetDescription[] renderTargetsRef, RenderPassDepthStencilDescription? depthStencilRef, RenderPassFlags flags)
        {
            using ID3D12GraphicsCommandList4? commandList = currentCommandList.NativeCommandList.QueryInterfaceOrNull<ID3D12GraphicsCommandList4>();

            if (commandList != null)
            {
                commandList.BeginRenderPass(numRenderTargets, renderTargetsRef, depthStencilRef, flags);
            }
        }

        public void EndRenderPass()
        {
            using ID3D12GraphicsCommandList4 commandList = currentCommandList.NativeCommandList.QueryInterface<ID3D12GraphicsCommandList4>();

            if (commandList != null)
            {
                commandList.EndRenderPass();
            }
        }

        public void Clear(Texture depthStencilBuffer, ClearFlags clearFlags, float depth = 1, byte stencil = 0)
        {
            currentCommandList.NativeCommandList.ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView, (Vortice.Direct3D12.ClearFlags)clearFlags, depth, stencil);
        }

        public unsafe void Clear(GraphicsResource renderTarget, Vector4 color)
        {
            currentCommandList.NativeCommandList.ClearRenderTargetView(renderTarget.NativeRenderTargetView, new Color4(color));
        }

        public void ClearState()
        {
            Array.Clear(Viewports, 0, Viewports.Length);
            Array.Clear(ScissorRectangles, 0, ScissorRectangles.Length);

            Texture? depthStencilBuffer = GraphicsDevice.Presenter?.DepthStencilBuffer;
            GraphicsResource? backBuffer = GraphicsDevice.Presenter?.BackBuffer;

            if (backBuffer != null)
            {
                SetRenderTargets(depthStencilBuffer, backBuffer);

                if (backBuffer is Texture texture)
                {
                    SetScissorRectangles(new Rectangle(0, 0, texture.Width, texture.Height));
                    SetViewports(new RectangleF(0, 0, texture.Width, texture.Height));
                }
            }
            else if (depthStencilBuffer != null)
            {
                SetRenderTargets(depthStencilBuffer);
                SetScissorRectangles(new Rectangle(0, 0, depthStencilBuffer.Width, depthStencilBuffer.Height));
                SetViewports(new RectangleF(0, 0, depthStencilBuffer.Width, depthStencilBuffer.Height));
            }
            else
            {
                SetRenderTargets(null);
            }
        }

        public CompiledCommandList Close()
        {
            currentCommandList.NativeCommandList.Close();

            return currentCommandList;
        }

        public void CopyBufferRegion(GraphicsResource source, long sourceOffset, GraphicsResource destination, long destinationOffset, long numBytes)
        {
            currentCommandList.NativeCommandList.CopyBufferRegion(destination.NativeResource, destinationOffset, source.NativeResource, sourceOffset, numBytes);
        }

        public void CopyResource(GraphicsResource source, GraphicsResource destination)
        {
            currentCommandList.NativeCommandList.CopyResource(destination.NativeResource, source.NativeResource);
        }

        public void CopyTextureRegion(TextureCopyLocation source, TextureCopyLocation destination)
        {
            currentCommandList.NativeCommandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
        }

        public void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
        {
            currentCommandList.NativeCommandList.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
        }

        public void Dispose()
        {
            switch (CommandListType)
            {
                case CommandListType.Bundle:
                    GraphicsDevice.BundleAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextDirectFenceValue - 1);
                    break;
                case CommandListType.Compute:
                    GraphicsDevice.ComputeAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextComputeFenceValue - 1);
                    break;
                case CommandListType.Copy:
                    GraphicsDevice.CopyAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextCopyFenceValue - 1);
                    break;
                case CommandListType.Direct:
                    GraphicsDevice.DirectAllocatorPool.Enqueue(currentCommandList.NativeCommandAllocator, GraphicsDevice.NextDirectFenceValue - 1);
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
            Task task = FlushAsync();

            if (wait)
            {
                task.Wait();
            }
        }

        public Task FlushAsync()
        {
            return GraphicsDevice.ExecuteCommandListsAsync(Close());
        }

        public void Reset()
        {
            ID3D12CommandAllocator commandAllocator = GetCommandAllocator();

            currentCommandList.NativeCommandAllocator = commandAllocator;
            currentCommandList.NativeCommandList.Reset(currentCommandList.NativeCommandAllocator, null);

            SetDescriptorHeaps(GraphicsDevice.ShaderVisibleShaderResourceViewAllocator, GraphicsDevice.ShaderVisibleSamplerAllocator);
        }

        public void ResourceBarrierTransition(GraphicsResource resource, ResourceStates stateBefore, ResourceStates stateAfter)
        {
            currentCommandList.NativeCommandList.ResourceBarrierTransition(resource.NativeResource, stateBefore, stateAfter);
        }

        private void SetDescriptorHeaps(params DescriptorAllocator[] descriptorHeaps)
        {
            if (CommandListType != CommandListType.Copy)
            {
                currentCommandList.NativeCommandList.SetDescriptorHeaps(descriptorHeaps.Length, descriptorHeaps.Select(d => d.DescriptorHeap).ToArray());

                srvDescriptorHeap = descriptorHeaps.SingleOrDefault(d => d.DescriptorHeap.Description.Type == Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
                samplerDescriptorHeap = descriptorHeaps.SingleOrDefault(d => d.DescriptorHeap.Description.Type == Vortice.Direct3D12.DescriptorHeapType.Sampler);
            }
        }

        public void SetComputeRoot32BitConstant(int rootParameterIndex, int srcData, int destOffsetIn32BitValues)
        {
            currentCommandList.NativeCommandList.SetComputeRoot32BitConstant(rootParameterIndex, srcData, destOffsetIn32BitValues);
        }

        public void SetComputeConstantBuffers(int rootParameterIndex, params GraphicsBuffer[] buffers)
        {
            if (srvDescriptorHeap is null) throw new InvalidOperationException();

            foreach (GraphicsBuffer buffer in buffers)
            {
                SetComputeRootDescriptorTable(rootParameterIndex, srvDescriptorHeap, buffer.CreateConstantBufferView(), 1);
            }
        }

        public void SetComputeSamplers(int rootParameterIndex, params SamplerState[] samplers)
        {
            if (samplerDescriptorHeap is null) throw new InvalidOperationException();

            foreach (SamplerState sampler in samplers)
            {
                SetComputeRootDescriptorTable(rootParameterIndex, samplerDescriptorHeap, sampler.NativeCpuDescriptorHandle, 1);
            }
        }

        public void SetComputeRootDescriptorTable(int rootParameterIndex, DescriptorSet descriptorSet)
        {
            DescriptorAllocator? descriptorAllocator = descriptorSet.DescriptorHeapType == DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView ? srvDescriptorHeap : samplerDescriptorHeap;

            if (descriptorAllocator is null) throw new InvalidOperationException();

            SetComputeRootDescriptorTable(rootParameterIndex, descriptorAllocator, descriptorSet.StartCpuDescriptorHandle, descriptorSet.TotalDescriptorCount);
        }

        private void SetComputeRootDescriptorTable(int rootParameterIndex, DescriptorAllocator descriptorAllocator, CpuDescriptorHandle baseDescriptor, int descriptorCount)
        {
            GpuDescriptorHandle gpuDescriptorHandle = CopyDescriptors(descriptorAllocator, baseDescriptor, descriptorCount);

            currentCommandList.NativeCommandList.SetComputeRootDescriptorTable(rootParameterIndex, gpuDescriptorHandle);
        }

        public void SetGraphicsRoot32BitConstant(int rootParameterIndex, int srcData, int destOffsetIn32BitValues)
        {
            currentCommandList.NativeCommandList.SetGraphicsRoot32BitConstant(rootParameterIndex, srcData, destOffsetIn32BitValues);
        }

        public void SetGraphicsConstantBuffers(int rootParameterIndex, params GraphicsBuffer[] buffers)
        {
            if (srvDescriptorHeap is null) throw new InvalidOperationException();

            foreach (GraphicsBuffer buffer in buffers)
            {
                SetGraphicsRootDescriptorTable(rootParameterIndex, srvDescriptorHeap, buffer.NativeConstantBufferView, 1);
            }
        }

        public void SetGraphicsSamplers(int rootParameterIndex, params SamplerState[] samplers)
        {
            if (samplerDescriptorHeap is null) throw new InvalidOperationException();

            foreach (SamplerState sampler in samplers)
            {
                SetGraphicsRootDescriptorTable(rootParameterIndex, samplerDescriptorHeap, sampler.NativeCpuDescriptorHandle, 1);
            }
        }

        public void SetGraphicsRootDescriptorTable(int rootParameterIndex, DescriptorSet descriptorSet)
        {
            DescriptorAllocator? descriptorAllocator = descriptorSet.DescriptorHeapType == DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView ? srvDescriptorHeap : samplerDescriptorHeap;

            if (descriptorAllocator is null) throw new InvalidOperationException();

            SetGraphicsRootDescriptorTable(rootParameterIndex, descriptorAllocator, descriptorSet.StartCpuDescriptorHandle, descriptorSet.TotalDescriptorCount);
        }

        private void SetGraphicsRootDescriptorTable(int rootParameterIndex, DescriptorAllocator descriptorAllocator, CpuDescriptorHandle baseDescriptor, int descriptorCount)
        {
            GpuDescriptorHandle gpuDescriptorHandle = CopyDescriptors(descriptorAllocator, baseDescriptor, descriptorCount);

            currentCommandList.NativeCommandList.SetGraphicsRootDescriptorTable(rootParameterIndex, gpuDescriptorHandle);
        }

        private GpuDescriptorHandle CopyDescriptors(DescriptorAllocator descriptorAllocator, CpuDescriptorHandle baseDescriptor, int descriptorCount)
        {
            GpuDescriptorHandle gpuDescriptorHandle = descriptorAllocator.CurrentGpuDescriptorHandle;
            CpuDescriptorHandle destinationDescriptor = descriptorAllocator.Allocate(descriptorCount);
            GraphicsDevice.NativeDevice.CopyDescriptorsSimple(descriptorCount, destinationDescriptor, baseDescriptor, descriptorAllocator.DescriptorHeap.Description.Type);

            return gpuDescriptorHandle;
        }

        public void SetIndexBuffer(GraphicsBuffer? indexBuffer)
        {
            if (indexBuffer is null)
            {
                currentCommandList.NativeCommandList.IASetIndexBuffer(null);
            }
            else
            {
                IndexBufferView indexBufferView = new IndexBufferView
                {
                    BufferLocation = indexBuffer.NativeResource!.GPUVirtualAddress,
                    SizeInBytes = indexBuffer.SizeInBytes,
                    Format = indexBuffer.StructureByteStride == sizeof(int) ? Format.R32_UInt : Format.R16_UInt
                };

                currentCommandList.NativeCommandList.IASetIndexBuffer(indexBufferView);
            }
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (pipelineState.IsCompute)
            {
                currentCommandList.NativeCommandList.SetComputeRootSignature(pipelineState.RootSignature);
            }
            else
            {
                currentCommandList.NativeCommandList.SetGraphicsRootSignature(pipelineState.RootSignature);
            }

            currentCommandList.NativeCommandList.SetPipelineState(pipelineState.NativePipelineState);
        }

        public void SetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            currentCommandList.NativeCommandList.IASetPrimitiveTopology((Vortice.DirectX.Direct3D.PrimitiveTopology)primitiveTopology);
        }

        public void SetRenderTargets(Texture? depthStencilView, params GraphicsResource[] renderTargetViews)
        {
            DepthStencilBuffer = depthStencilView;

            if (renderTargetViews.Length > MaxRenderTargetCount)
            {
                throw new ArgumentOutOfRangeException(nameof(renderTargetViews), renderTargetViews.Length, $"The maximum number of render targets is {MaxRenderTargetCount}.");
            }

            if (RenderTargets.Length != renderTargetViews.Length)
            {
                RenderTargets = new GraphicsResource[renderTargetViews.Length];
            }

            renderTargetViews.CopyTo(RenderTargets, 0);

            CpuDescriptorHandle[] renderTargetDescriptors = new CpuDescriptorHandle[renderTargetViews.Length];

            for (int i = 0; i < renderTargetViews.Length; i++)
            {
                renderTargetDescriptors[i] = renderTargetViews[i].NativeRenderTargetView;
            }

            currentCommandList.NativeCommandList.OMSetRenderTargets(renderTargetDescriptors, depthStencilView?.NativeDepthStencilView);
        }

        public void SetScissorRectangles(params Rectangle[] scissorRectangles)
        {
            if (scissorRectangles.Length > MaxViewportAndScissorRectangleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(scissorRectangles), scissorRectangles.Length, $"The maximum number of scissor rectangles is {MaxViewportAndScissorRectangleCount}.");
            }

            if (ScissorRectangles.Length != scissorRectangles.Length)
            {
                ScissorRectangles = new Rectangle[scissorRectangles.Length];
            }

            scissorRectangles.CopyTo(ScissorRectangles, 0);

            currentCommandList.NativeCommandList.RSSetScissorRects(scissorRectangles.Select(s => (Rect)s).ToArray());
        }

        public void SetVertexBuffers(int startSlot, params GraphicsBuffer[] vertexBuffers)
        {
            VertexBufferView[] vertexBufferViews = vertexBuffers.Select(b => new VertexBufferView
            {
                BufferLocation = b.NativeResource!.GPUVirtualAddress,
                SizeInBytes = b.SizeInBytes,
                StrideInBytes = b.StructureByteStride
            }).ToArray();

            currentCommandList.NativeCommandList.IASetVertexBuffers(startSlot, vertexBufferViews);
        }

        public void SetViewports(params RectangleF[] viewports)
        {
            if (viewports.Length > MaxViewportAndScissorRectangleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(viewports), viewports.Length, $"The maximum number of viewporst is {MaxViewportAndScissorRectangleCount}.");
            }

            if (Viewports.Length != viewports.Length)
            {
                Viewports = new RectangleF[viewports.Length];
            }

            viewports.CopyTo(Viewports, 0);

            currentCommandList.NativeCommandList.RSSetViewports(viewports.Select(v => new Viewport(v)).ToArray());
        }

        private ID3D12CommandAllocator GetCommandAllocator() => CommandListType switch
        {
            CommandListType.Bundle => GraphicsDevice.BundleAllocatorPool.GetCommandAllocator(),
            CommandListType.Compute => GraphicsDevice.ComputeAllocatorPool.GetCommandAllocator(),
            CommandListType.Copy => GraphicsDevice.CopyAllocatorPool.GetCommandAllocator(),
            CommandListType.Direct => GraphicsDevice.DirectAllocatorPool.GetCommandAllocator(),
            _ => throw new NotSupportedException("This command list type is not supported.")
        };
    }
}
