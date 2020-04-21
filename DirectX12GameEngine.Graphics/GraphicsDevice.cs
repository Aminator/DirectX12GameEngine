using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;

namespace DirectX12GameEngine.Graphics
{
    public sealed class GraphicsDevice : IDisposable
    {
        private Vortice.Direct3D11.ID3D11Device? nativeDirect3D11Device;

        public unsafe GraphicsDevice(FeatureLevel minFeatureLevel = FeatureLevel.Level11_0, bool enableDebugLayer = false)
        {
            if (enableDebugLayer)
            {
                Result debugResult = D3D12.D3D12GetDebugInterface(out ID3D12Debug debugInterface);

                if (debugResult.Success)
                {
                    ID3D12Debug1 debug = debugInterface.QueryInterface<ID3D12Debug1>();

                    debug.EnableDebugLayer();
                }
            }

            FeatureLevel = minFeatureLevel < FeatureLevel.Level11_0 ? FeatureLevel.Level11_0 : minFeatureLevel;

            Result result = D3D12.D3D12CreateDevice(null, (Vortice.Direct3D.FeatureLevel)FeatureLevel, out ID3D12Device device);

            if (result.Failure)
            {
                throw new COMException("Device creation failed.", result.Code);
            }

            NativeDevice = device;

            DirectCommandQueue = new CommandQueue(this, CommandListType.Direct);
            ComputeCommandQueue = new CommandQueue(this, CommandListType.Compute);
            CopyCommandQueue = new CommandQueue(this, CommandListType.Copy);

            DepthStencilViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.DepthStencilView, 1);
            RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.RenderTargetView, 2);
            ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 4096);
            SamplerAllocator = new DescriptorAllocator(this, DescriptorHeapType.Sampler, 256);

            ShaderVisibleShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 4096, DescriptorHeapFlags.ShaderVisible);
            ShaderVisibleSamplerAllocator = new DescriptorAllocator(this, DescriptorHeapType.Sampler, 256, DescriptorHeapFlags.ShaderVisible);

            CommandList = new CommandList(this, CommandListType.Direct);
            CommandList.Close();
        }

        public int DeviceRemovedReason => (int)NativeDevice.DeviceRemovedReason;

        public FeatureLevel FeatureLevel { get; }

        public CommandList CommandList { get; }

        public GraphicsPresenter? Presenter { get; set; }

        public CommandQueue DirectCommandQueue { get; }

        public CommandQueue ComputeCommandQueue { get; }

        public CommandQueue CopyCommandQueue { get; }

        public DescriptorAllocator DepthStencilViewAllocator { get; set; }

        public DescriptorAllocator RenderTargetViewAllocator { get; set; }

        public DescriptorAllocator ShaderResourceViewAllocator { get; set; }

        public DescriptorAllocator SamplerAllocator { get; set; }

        public DescriptorAllocator ShaderVisibleShaderResourceViewAllocator { get; }

        public DescriptorAllocator ShaderVisibleSamplerAllocator { get; }

        public object Direct3D12Device => NativeDevice;

        public object Direct3D11Device => NativeDirect3D11Device;

        internal ID3D12Device NativeDevice { get; }

        internal Vortice.Direct3D11.ID3D11Device NativeDirect3D11Device
        {
            get
            {
                if (nativeDirect3D11Device is null)
                {
                    Result result = Vortice.Direct3D11.D3D11.D3D11On12CreateDevice(
                        NativeDevice, Vortice.Direct3D11.DeviceCreationFlags.BgraSupport, new[] { (Vortice.Direct3D.FeatureLevel)FeatureLevel }, new[] { DirectCommandQueue.NativeCommandQueue }, 0,
                        out nativeDirect3D11Device, out _, out _);

                    if (result.Failure)
                    {
                        throw new COMException("Device creation failed.", result.Code);
                    }
                }

                return nativeDirect3D11Device;
            }
        }

        public void Dispose()
        {
            NativeDevice.Dispose();
        }
    }
}
