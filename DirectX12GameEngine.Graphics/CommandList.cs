using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vortice;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public sealed class CommandList : IDisposable
    {
        private const int MaxRenderTargetCount = 8;

        private DescriptorAllocator? srvDescriptorHeap;
        private DescriptorAllocator? samplerDescriptorHeap;

        public CommandList(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            CommandListType = commandListType;

            NativeCommandAllocator = GraphicsDevice.NativeDevice.CreateCommandAllocator((Vortice.Direct3D12.CommandListType)CommandListType);
            NativeCommandList = GraphicsDevice.NativeDevice.CreateCommandList((Vortice.Direct3D12.CommandListType)CommandListType, NativeCommandAllocator, null);

            SetDescriptorHeaps(GraphicsDevice.ShaderVisibleShaderResourceViewAllocator, GraphicsDevice.ShaderVisibleSamplerAllocator);
        }

        public CommandListType CommandListType { get; }

        public DepthStencilView? DepthStencilBuffer { get; private set; }

        public GraphicsDevice GraphicsDevice { get; }

        public RenderTargetView[] RenderTargets { get; private set; } = Array.Empty<RenderTargetView>();

        public Rectangle[] ScissorRectangles { get; private set; } = Array.Empty<Rectangle>();

        public Viewport[] Viewports { get; private set; } = Array.Empty<Viewport>();

        internal ID3D12CommandAllocator NativeCommandAllocator { get; set; }

        internal ID3D12GraphicsCommandList NativeCommandList { get; }

        public void BeginRenderPass()
        {
            BeginRenderPass(DepthStencilBuffer, RenderTargets);
        }

        public void BeginRenderPass(DepthStencilView? depthStencilView, params RenderTargetView[] renderTargetViews)
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
                    CpuDescriptor = renderTargetViews[i].CpuDescriptorHandle.ToCpuDescriptorHandle()
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
                    CpuDescriptor = depthStencilView.CpuDescriptorHandle.ToCpuDescriptorHandle()
                };
            }

            BeginRenderPass(renderTargetViews.Length, renderPassRenderTargetDescriptions, renderPassDepthStencilDescription, RenderPassFlags.None);
        }

        public void BeginRenderPass(int numRenderTargets, RenderPassRenderTargetDescription[] renderTargetsRef, RenderPassDepthStencilDescription? depthStencilRef, RenderPassFlags flags)
        {
            using ID3D12GraphicsCommandList4? commandList = NativeCommandList.QueryInterfaceOrNull<ID3D12GraphicsCommandList4>();

            if (commandList != null)
            {
                commandList.BeginRenderPass(numRenderTargets, renderTargetsRef, depthStencilRef, flags);
            }
        }

        public void EndRenderPass()
        {
            using ID3D12GraphicsCommandList4 commandList = NativeCommandList.QueryInterface<ID3D12GraphicsCommandList4>();

            if (commandList != null)
            {
                commandList.EndRenderPass();
            }
        }

        public void ClearDepthStencilView(DepthStencilView depthStencilView, ClearFlags clearFlags, float depth = 1, byte stencil = 0, params Rectangle[] rectangles)
        {
            NativeCommandList.ClearDepthStencilView(depthStencilView.CpuDescriptorHandle.ToCpuDescriptorHandle(), (Vortice.Direct3D12.ClearFlags)clearFlags, depth, stencil, rectangles.Select(r => new RawRect(r.Left, r.Top, r.Right, r.Bottom)).ToArray());
        }

        public void ClearRenderTargetView(RenderTargetView renderTargetView, in Vector4 color, params Rectangle[] rectangles)
        {
            NativeCommandList.ClearRenderTargetView(renderTargetView.CpuDescriptorHandle.ToCpuDescriptorHandle(), new Vortice.Mathematics.Color4(color), rectangles.Select(r => new RawRect(r.Left, r.Top, r.Right, r.Bottom)).ToArray());
        }

        public void ClearState()
        {
            Array.Clear(Viewports, 0, Viewports.Length);
            Array.Clear(ScissorRectangles, 0, ScissorRectangles.Length);

            DepthStencilView? depthStencilBuffer = GraphicsDevice.Presenter?.DepthStencilBuffer;
            RenderTargetView? backBuffer = GraphicsDevice.Presenter?.BackBuffer;

            if (backBuffer != null)
            {
                SetRenderTargets(depthStencilBuffer, backBuffer);
                SetScissorRectangles(new Rectangle(0, 0, (int)backBuffer.Resource.Width, backBuffer.Resource.Height));
                SetViewports(new Viewport(0, 0, backBuffer.Resource.Width, backBuffer.Resource.Height));
            }
            else if (depthStencilBuffer != null)
            {
                SetRenderTargets(depthStencilBuffer);
                SetScissorRectangles(new Rectangle(0, 0, (int)depthStencilBuffer.Resource.Width, depthStencilBuffer.Resource.Height));
                SetViewports(new Viewport(0, 0, depthStencilBuffer.Resource.Width, depthStencilBuffer.Resource.Height));
            }
            else
            {
                SetRenderTargets(null);
            }
        }

        public CompiledCommandList Close()
        {
            NativeCommandList.Close();

            return new CompiledCommandList(this, NativeCommandAllocator, NativeCommandList);
        }

        public void CopyBufferRegion(GraphicsResource source, long sourceOffset, GraphicsResource destination, long destinationOffset, long numBytes)
        {
            NativeCommandList.CopyBufferRegion(destination.NativeResource, destinationOffset, source.NativeResource, sourceOffset, numBytes);
        }

        public void CopyResource(GraphicsResource source, GraphicsResource destination)
        {
            NativeCommandList.CopyResource(destination.NativeResource, source.NativeResource);
        }

        public void CopyTextureRegion(TextureCopyLocation source, TextureCopyLocation destination)
        {
            NativeCommandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
        }

        public void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
        {
            NativeCommandList.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
        }

        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            NativeCommandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation = 0, int startInstanceLocation = 0)
        {
            NativeCommandList.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
        }

        public void ExecuteBundle(CompiledCommandList commandList)
        {
            if (commandList.Builder.CommandListType == CommandListType.Bundle)
            {
                NativeCommandList.ExecuteBundle(commandList.NativeCommandList);
            }
        }

        public void Flush()
        {
            GetCommandQueue().ExecuteCommandLists(Close());
        }

        public Task FlushAsync()
        {
            return GetCommandQueue().ExecuteCommandListsAsync(Close());
        }

        private CommandQueue GetCommandQueue() => CommandListType switch
        {
            CommandListType.Direct => GraphicsDevice.DirectCommandQueue,
            CommandListType.Compute => GraphicsDevice.ComputeCommandQueue,
            CommandListType.Copy => GraphicsDevice.CopyCommandQueue,
            _ => throw new NotSupportedException()
        };

        public void Reset()
        {
            NativeCommandAllocator.Reset();
            NativeCommandList.Reset(NativeCommandAllocator, null);

            SetDescriptorHeaps(GraphicsDevice.ShaderVisibleShaderResourceViewAllocator, GraphicsDevice.ShaderVisibleSamplerAllocator);
        }

        public void ResourceBarrierTransition(GraphicsResource resource, ResourceStates stateBefore, ResourceStates stateAfter)
        {
            NativeCommandList.ResourceBarrierTransition(resource.NativeResource, stateBefore, stateAfter);
        }

        private void SetDescriptorHeaps(params DescriptorAllocator[] descriptorHeaps)
        {
            if (CommandListType != CommandListType.Copy)
            {
                NativeCommandList.SetDescriptorHeaps(descriptorHeaps.Length, descriptorHeaps.Select(d => d.DescriptorHeap).ToArray());

                srvDescriptorHeap = descriptorHeaps.SingleOrDefault(d => d.DescriptorHeap.Description.Type == Vortice.Direct3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
                samplerDescriptorHeap = descriptorHeaps.SingleOrDefault(d => d.DescriptorHeap.Description.Type == Vortice.Direct3D12.DescriptorHeapType.Sampler);
            }
        }

        public void SetComputeRoot32BitConstant(int rootParameterIndex, int srcData, int destOffsetIn32BitValues)
        {
            NativeCommandList.SetComputeRoot32BitConstant(rootParameterIndex, srcData, destOffsetIn32BitValues);
        }

        public void SetComputeRootConstantBufferView(int rootParameterIndex, ConstantBufferView constantBufferView)
        {
            if (srvDescriptorHeap is null) throw new InvalidOperationException();

            SetComputeRootDescriptorTable(rootParameterIndex, srvDescriptorHeap, constantBufferView.CpuDescriptorHandle, 1);
        }

        public void SetComputeRootSampler(int rootParameterIndex, Sampler sampler)
        {
            if (samplerDescriptorHeap is null) throw new InvalidOperationException();

            SetComputeRootDescriptorTable(rootParameterIndex, samplerDescriptorHeap, sampler.CpuDescriptorHandle, 1);
        }

        public void SetComputeRootDescriptorTable(int rootParameterIndex, DescriptorSet descriptorSet)
        {
            DescriptorAllocator? descriptorAllocator = descriptorSet.DescriptorHeapType == DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView ? srvDescriptorHeap : samplerDescriptorHeap;

            if (descriptorAllocator is null) throw new InvalidOperationException();

            SetComputeRootDescriptorTable(rootParameterIndex, descriptorAllocator, descriptorSet.StartCpuDescriptorHandle, descriptorSet.DescriptorCapacity);
        }

        private void SetComputeRootDescriptorTable(int rootParameterIndex, DescriptorAllocator descriptorAllocator, IntPtr baseDescriptor, int descriptorCount)
        {
            long gpuDescriptorHandle = CopyDescriptors(descriptorAllocator, baseDescriptor, descriptorCount);

            NativeCommandList.SetComputeRootDescriptorTable(rootParameterIndex, gpuDescriptorHandle.ToGpuDescriptorHandle());
        }

        public void SetGraphicsRoot32BitConstant(int rootParameterIndex, int srcData, int destOffsetIn32BitValues)
        {
            NativeCommandList.SetGraphicsRoot32BitConstant(rootParameterIndex, srcData, destOffsetIn32BitValues);
        }

        public void SetGraphicsRootConstantBufferView(int rootParameterIndex, ConstantBufferView constantBufferView)
        {
            if (srvDescriptorHeap is null) throw new InvalidOperationException();

            SetGraphicsRootDescriptorTable(rootParameterIndex, srvDescriptorHeap, constantBufferView.CpuDescriptorHandle, 1);
        }

        public void SetGraphicsRootSampler(int rootParameterIndex, Sampler sampler)
        {
            if (samplerDescriptorHeap is null) throw new InvalidOperationException();

            SetGraphicsRootDescriptorTable(rootParameterIndex, samplerDescriptorHeap, sampler.CpuDescriptorHandle, 1);
        }

        public void SetGraphicsRootDescriptorTable(int rootParameterIndex, DescriptorSet descriptorSet)
        {
            DescriptorAllocator? descriptorAllocator = descriptorSet.DescriptorHeapType == DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView ? srvDescriptorHeap : samplerDescriptorHeap;

            if (descriptorAllocator is null) throw new InvalidOperationException();

            SetGraphicsRootDescriptorTable(rootParameterIndex, descriptorAllocator, descriptorSet.StartCpuDescriptorHandle, descriptorSet.DescriptorCapacity);
        }

        private void SetGraphicsRootDescriptorTable(int rootParameterIndex, DescriptorAllocator descriptorAllocator, IntPtr baseDescriptor, int descriptorCount)
        {
            long gpuDescriptor = CopyDescriptors(descriptorAllocator, baseDescriptor, descriptorCount);

            NativeCommandList.SetGraphicsRootDescriptorTable(rootParameterIndex, gpuDescriptor.ToGpuDescriptorHandle());
        }

        private long CopyDescriptors(DescriptorAllocator descriptorAllocator, IntPtr baseDescriptor, int descriptorCount)
        {
            IntPtr destinationDescriptor = descriptorAllocator.Allocate(descriptorCount);
            GraphicsDevice.NativeDevice.CopyDescriptorsSimple(descriptorCount, destinationDescriptor.ToCpuDescriptorHandle(), baseDescriptor.ToCpuDescriptorHandle(), descriptorAllocator.DescriptorHeap.Description.Type);

            return descriptorAllocator.GetGpuDescriptorHandle(destinationDescriptor);
        }

        public void SetIndexBuffer(IndexBufferView? indexBufferView)
        {
            if (indexBufferView is null)
            {
                NativeCommandList.IASetIndexBuffer(null);
            }
            else
            {
                NativeCommandList.IASetIndexBuffer(Unsafe.As<IndexBufferView, Vortice.Direct3D12.IndexBufferView>(ref Unsafe.AsRef(indexBufferView.Value)));
            }
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (pipelineState.IsCompute)
            {
                NativeCommandList.SetComputeRootSignature(pipelineState.RootSignature.NativeRootSignature);
            }
            else
            {
                NativeCommandList.SetGraphicsRootSignature(pipelineState.RootSignature.NativeRootSignature);
            }

            NativeCommandList.SetPipelineState(pipelineState.NativePipelineState);
        }

        public void SetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            NativeCommandList.IASetPrimitiveTopology((Vortice.Direct3D.PrimitiveTopology)primitiveTopology);
        }

        public void SetRenderTargets(DepthStencilView? depthStencilView, params RenderTargetView[] renderTargetViews)
        {
            DepthStencilBuffer = depthStencilView;

            if (renderTargetViews.Length > MaxRenderTargetCount)
            {
                throw new ArgumentOutOfRangeException(nameof(renderTargetViews), renderTargetViews.Length, $"The maximum number of render targets is {MaxRenderTargetCount}.");
            }

            if (RenderTargets.Length != renderTargetViews.Length)
            {
                RenderTargets = new RenderTargetView[renderTargetViews.Length];
            }

            renderTargetViews.CopyTo(RenderTargets, 0);

            CpuDescriptorHandle[] renderTargetDescriptors = new CpuDescriptorHandle[renderTargetViews.Length];

            for (int i = 0; i < renderTargetViews.Length; i++)
            {
                renderTargetDescriptors[i] = renderTargetViews[i].CpuDescriptorHandle.ToCpuDescriptorHandle();
            }

            NativeCommandList.OMSetRenderTargets(renderTargetDescriptors, depthStencilView?.CpuDescriptorHandle.ToCpuDescriptorHandle());
        }

        public void SetScissorRectangles(params Rectangle[] scissorRectangles)
        {
            if (scissorRectangles.Length > D3D12.ViewportAndScissorRectObjectCountPerPipeline)
            {
                throw new ArgumentOutOfRangeException(nameof(scissorRectangles), scissorRectangles.Length, $"The maximum number of scissor rectangles is {D3D12.ViewportAndScissorRectObjectCountPerPipeline}.");
            }

            if (ScissorRectangles.Length != scissorRectangles.Length)
            {
                ScissorRectangles = new Rectangle[scissorRectangles.Length];
            }

            scissorRectangles.CopyTo(ScissorRectangles, 0);

            NativeCommandList.RSSetScissorRects(scissorRectangles.Select(r => new RawRect(r.Left, r.Top, r.Right, r.Bottom)).ToArray());
        }

        public void SetVertexBuffers(int startSlot, params VertexBufferView[] vertexBufferViews)
        {
            NativeCommandList.IASetVertexBuffers(startSlot, Unsafe.As<Vortice.Direct3D12.VertexBufferView[]>(vertexBufferViews));
        }

        public void SetViewports(params Viewport[] viewports)
        {
            if (viewports.Length > D3D12.ViewportAndScissorRectObjectCountPerPipeline)
            {
                throw new ArgumentOutOfRangeException(nameof(viewports), viewports.Length, $"The maximum number of viewports is {D3D12.ViewportAndScissorRectObjectCountPerPipeline}.");
            }

            if (Viewports.Length != viewports.Length)
            {
                Viewports = new Viewport[viewports.Length];
            }

            viewports.CopyTo(Viewports, 0);

            NativeCommandList.RSSetViewports(viewports);
        }

        public void Dispose()
        {
            NativeCommandList.Dispose();
            NativeCommandAllocator.Dispose();
        }
    }
}
