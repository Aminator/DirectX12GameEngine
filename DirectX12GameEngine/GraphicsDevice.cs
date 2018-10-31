using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using Windows.Graphics.DirectX.Direct3D11;

using Device = SharpDX.Direct3D12.Device;

namespace DirectX12GameEngine
{
    public sealed class GraphicsDevice : IDisposable, ICollector
    {
        internal const int ConstantBufferDataPlacementAlignment = 256;

        private static readonly Guid ID3D11Resource = new Guid("DC8E63F3-D12B-4952-B47B-5E45026A862D");

        private readonly Queue<CommandList> copyCommandLists = new Queue<CommandList>();
        private readonly AutoResetEvent fenceEvent = new AutoResetEvent(false);

        public GraphicsDevice(FeatureLevel minFeatureLevel)
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
            ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, DescriptorHeapFlags.ShaderVisible);
            RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.RenderTargetView, descriptorCount: 2);

            NativeCopyFence = NativeDevice.CreateFence(0, FenceFlags.None);
            NativeFence = NativeDevice.CreateFence(0, FenceFlags.None);

            CommandList = new CommandList(this, CommandListType.Direct);
            CommandList.Close();
        }

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IDirect3DDxgiInterfaceAccess : IDisposable
        {
            IntPtr GetInterface(in Guid iid);
        }

        public CommandList CommandList { get; }

        public ICollection<IDisposable> Disposables { get; } = new List<IDisposable>();

        public FeatureLevel FeatureLevel { get; }

        public GraphicsPresenter? Presenter { get; internal set; }

        internal CommandAllocatorPool BundleAllocatorPool { get; }

        internal CommandAllocatorPool CopyAllocatorPool { get; }

        internal CommandAllocatorPool DirectAllocatorPool { get; }

        internal CommandQueue NativeCommandQueue { get; }

        internal CommandQueue NativeCopyCommandQueue { get; }

        internal Device NativeDevice { get; }

        internal SharpDX.Direct3D11.Device NativeDirect3D11Device { get; }

        internal Fence NativeCopyFence { get; }

        internal Fence NativeFence { get; }

        internal DescriptorAllocator DepthStencilViewAllocator { get; }

        internal DescriptorAllocator ShaderResourceViewAllocator { get; }

        internal DescriptorAllocator RenderTargetViewAllocator { get; set; }

        internal long NextCopyFenceValue { get; private set; } = 1;

        internal long NextFenceValue { get; private set; } = 1;

        public static SharpDX.D3DCompiler.ShaderBytecode? CompileShaders(string filePath, out SharpDX.D3DCompiler.ShaderBytecode vertexShader, out SharpDX.D3DCompiler.ShaderBytecode pixelShader)
        {
            SharpDX.D3DCompiler.ShaderFlags shaderFlags = SharpDX.D3DCompiler.ShaderFlags.PackMatrixRowMajor;
#if DEBUG
            shaderFlags |= SharpDX.D3DCompiler.ShaderFlags.Debug | SharpDX.D3DCompiler.ShaderFlags.SkipOptimization;
#endif
            string shaderSource = SharpDX.D3DCompiler.ShaderBytecode.PreprocessFromFile(filePath, null, new Includer());

            vertexShader = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                    shaderSource, "VSMain", "vs_5_1", shaderFlags);

            pixelShader = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                    shaderSource, "PSMain", "ps_5_1", shaderFlags);

            try
            {
                return vertexShader.GetPart(SharpDX.D3DCompiler.ShaderBytecodePart.RootSignature);
            }
            catch
            {
                return null;
            }
        }

        public unsafe Texture CreateBuffer<T>(Span<T> data, DescriptorHeapType? descriptorHeapType = null) where T : unmanaged
        {
            int bufferSize = data.Length * sizeof(T);

            Texture buffer = Texture.NewBuffer(this, bufferSize, descriptorHeapType, ResourceStates.CopyDestination, heapType: HeapType.Default);
            Texture uploadBuffer = Texture.New(this, new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), buffer.NativeResource.Description);

            IntPtr uploadPointer = uploadBuffer.Map(0);
            Utilities.Write(uploadPointer, data.ToArray(), 0, data.Length);
            uploadBuffer.Unmap(0);

            CommandList copyCommandList = GetOrCreateCopyCommandList();

            copyCommandList.CopyResource(buffer, uploadBuffer);
            copyCommandList.Flush();

            copyCommandLists.Enqueue(copyCommandList);

            return buffer;
        }

        public Texture CreateConstantBufferView<T>(in T data) where T : unmanaged
        {
            Span<T> span = stackalloc T[] { data };
            return CreateConstantBufferView(span);
        }

        public unsafe Texture CreateConstantBufferView<T>(Span<T> data) where T : unmanaged
        {
            int bufferSize = data.Length * sizeof(T);

            int constantBufferSize = (bufferSize + 255) & ~255;

            Texture constantBuffer = Texture.NewBuffer(this, constantBufferSize, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            ConstantBufferViewDescription cbvDescription = new ConstantBufferViewDescription
            {
                BufferLocation = constantBuffer.NativeResource.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            NativeDevice.CreateConstantBufferView(cbvDescription, constantBuffer.NativeCpuDescriptorHandle);

            IntPtr constantBufferPointer = constantBuffer.Map(0);
            Utilities.Write(constantBufferPointer, data.ToArray(), 0, data.Length);

            return constantBuffer;
        }

        public IndexBufferView CreateIndexBufferView(Texture indexBuffer, Format format, int size)
        {
            switch (format)
            {
                case Format.R16_UInt:
                case Format.R32_UInt:
                    break;
                default:
                    throw new ArgumentException("Index buffer type must be ushort or uint");
            }

            return new IndexBufferView
            {
                BufferLocation = indexBuffer.NativeResource.GPUVirtualAddress,
                Format = format,
                SizeInBytes = size
            };
        }

        public IndexBufferView CreateIndexBufferView<T>(Span<T> indices, Format format, out Texture indexBuffer) where T : unmanaged
        {
            indexBuffer = CreateBuffer(indices);

            int indexBufferSize = indexBuffer.Width * indexBuffer.Height;

            return CreateIndexBufferView(indexBuffer, format, indexBufferSize);
        }

        public RootSignature CreateRootSignature(RootSignatureDescription rootSignatureDescription)
        {
            return NativeDevice.CreateRootSignature(rootSignatureDescription.Serialize());
        }

        public RootSignature CreateRootSignature(byte[] bytecode)
        {
            return NativeDevice.CreateRootSignature(bytecode);
        }

        public unsafe Texture CreateTexture2D<T>(Span<T> data, Format format, int width, int height) where T : unmanaged
        {
            int texturePixelSize;

            switch (format)
            {
                case Format.R8G8B8A8_UNorm:
                case Format.B8G8R8A8_UNorm:
                    texturePixelSize = 4;
                    break;
                default:
                    throw new ArgumentException("This format is not supported.");
            }

            Texture texture = Texture.New2D(this, format, width, height, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, ResourceStates.CopyDestination);
            Texture textureUploadBuffer = Texture.New(this, new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), texture.NativeResource.Description);

            fixed (T* ptr = data)
            {
                textureUploadBuffer.NativeResource.WriteToSubresource(0, null, (IntPtr)ptr, texturePixelSize * width, data.Length * sizeof(T));
            }

            ShaderResourceViewDescription srvDescription = new ShaderResourceViewDescription
            {
                Shader4ComponentMapping = D3DXUtilities.DefaultComponentMapping(),
                Format = format,
                Dimension = SharpDX.Direct3D12.ShaderResourceViewDimension.Texture2D,
                Texture2D = { MipLevels = 1 },
            };

            NativeDevice.CreateShaderResourceView(texture.NativeResource, srvDescription, texture.NativeCpuDescriptorHandle);

            CommandList copyCommandList = GetOrCreateCopyCommandList();

            copyCommandList.CopyResource(texture, textureUploadBuffer);
            copyCommandList.Flush();

            copyCommandLists.Enqueue(copyCommandList);

            return texture;
        }

        public VertexBufferView CreateVertexBufferView(Texture vertexBuffer, int size, int stride)
        {
            return new VertexBufferView
            {
                BufferLocation = vertexBuffer.NativeResource.GPUVirtualAddress,
                StrideInBytes = stride,
                SizeInBytes = size
            };
        }

        public unsafe VertexBufferView CreateVertexBufferView<T>(Texture vertexBuffer, int size) where T : unmanaged
        {
            return CreateVertexBufferView(vertexBuffer, size, sizeof(T));
        }

        public VertexBufferView CreateVertexBufferView<T>(Span<T> vertices, out Texture vertexBuffer, int? stride = null) where T : unmanaged
        {
            vertexBuffer = CreateBuffer(vertices);

            int vertexBufferSize = vertexBuffer.Width * vertexBuffer.Height;

            return stride.HasValue
                ? CreateVertexBufferView(vertexBuffer, vertexBufferSize, stride.Value)
                : CreateVertexBufferView<T>(vertexBuffer, vertexBufferSize);
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

            foreach (CommandList commandList in copyCommandLists)
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
            Fence fence;

            switch (commandLists[0].NativeCommandList.TypeInfo)
            {
                case CommandListType.Direct:
                    fence = NativeFence;
                    break;
                case CommandListType.Copy:
                    fence = NativeCopyFence;
                    break;
                default:
                    throw new ArgumentException("This command list type is not supported.");
            }

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
                    throw new ArgumentException("This command list type is not supported.");
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

        internal static IDirect3DDevice CreateDirect3DDevice(SharpDX.DXGI.Device dxgiDevice)
        {
            Result result = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr graphicsDevice);

            IDirect3DDevice d3DInteropDevice = (IDirect3DDevice)Marshal.GetObjectForIUnknown(graphicsDevice);
            Marshal.Release(graphicsDevice);

            return d3DInteropDevice;
        }

        internal static IDirect3DSurface CreateDirect3DSurface(Surface dxgiSurface)
        {
            Result result = CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface.NativePointer, out IntPtr graphicsSurface);

            IDirect3DSurface d3DSurface = (IDirect3DSurface)Marshal.GetObjectForIUnknown(graphicsSurface);
            Marshal.Release(graphicsSurface);

            return d3DSurface;
        }

        internal static SharpDX.DXGI.Device CreateDXGIDevice(IDirect3DDevice direct3DDevice)
        {
            IDirect3DDxgiInterfaceAccess dxgiDeviceInterfaceAccess = (IDirect3DDxgiInterfaceAccess)direct3DDevice;
            IntPtr device = dxgiDeviceInterfaceAccess.GetInterface(ID3D11Resource);

            return new SharpDX.DXGI.Device(device);
        }

        internal static Surface CreateDXGISurface(IDirect3DSurface direct3DSurface)
        {
            IDirect3DDxgiInterfaceAccess dxgiSurfaceInterfaceAccess = (IDirect3DDxgiInterfaceAccess)direct3DSurface;
            IntPtr surface = dxgiSurfaceInterfaceAccess.GetInterface(ID3D11Resource);

            return new Surface(surface);
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

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
            SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern Result CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface",
            SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern Result CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);

        private CommandList GetOrCreateCopyCommandList()
        {
            CommandList commandList;

            lock (copyCommandLists)
            {
                if (copyCommandLists.Count > 0)
                {
                    commandList = copyCommandLists.Dequeue();
                    commandList.Reset();
                }
                else
                {
                    commandList = new CommandList(this, CommandListType.Copy);
                }
            }

            return commandList;
        }

        //public Texture CreateBuffer<T>(T[] data, int offset = 0, int? length = null, DescriptorHeapType? descriptorHeapType = null) where T : struct
        //{
        //    return CreateBuffer(data.AsSpan(offset, length ?? data.Length), descriptorHeapType);
        //}

        //public IntPtr CreateConstantBufferView<T>(T[] data, out Texture constantBuffer, int offset = 0, int? length = null) where T : struct
        //{
        //    return CreateConstantBufferView(data.AsSpan(offset, length ?? data.Length), out constantBuffer);
        //}

        //public IndexBufferView CreateIndexBufferView<T>(T[] indices, Format format, out Texture indexBuffer, int offset = 0, int? length = null) where T : struct
        //{
        //    return CreateIndexBufferView(indices.AsSpan(offset, length ?? indices.Length), format, out indexBuffer);
        //}

        //public VertexBufferView CreateVertexBufferView<T>(T[] vertices, out Texture vertexBuffer, int offset = 0, int? length = null, int? stride = null) where T : struct
        //{
        //    return CreateVertexBufferView(vertices.AsSpan(offset, length ?? vertices.Length), out vertexBuffer, stride);
        //}

        //public unsafe Texture CreateBuffer<T>(Span<T> data, DescriptorHeapType? descriptorHeapType = null) where T : unmanaged
        //{
        //    int bufferSize = data.Length * sizeof(T);

        //    Texture buffer = Texture.NewBuffer(this, bufferSize, descriptorHeapType);

        //    IntPtr vertexBufferPointer = buffer.Map(0);
        //    Utilities.Write(vertexBufferPointer, data.ToArray(), 0, data.Length);
        //    buffer.Unmap(0);

        //    return buffer;
        //}
    }

    internal class D3DXUtilities
    {
        public const int ComponentMappingMask = 0x7;

        public const int ComponentMappingShift = 3;

        public const int ComponentMappingAlwaysSetBitAvoidingZeromemMistakes = (1 << (ComponentMappingShift * 4));

        public static int ComponentMapping(int src0, int src1, int src2, int src3)
        {
            return ((((src0) & ComponentMappingMask)
                | (((src1) & ComponentMappingMask) << ComponentMappingShift)
                | (((src2) & ComponentMappingMask) << (ComponentMappingShift * 2))
                | (((src3) & ComponentMappingMask) << (ComponentMappingShift * 3))
                | ComponentMappingAlwaysSetBitAvoidingZeromemMistakes));
        }

        public static int DefaultComponentMapping()
        {
            return ComponentMapping(0, 1, 2, 3);
        }

        public static int ComponentMapping(int ComponentToExtract, int Mapping)
        {
            return ((Mapping >> (ComponentMappingShift * ComponentToExtract) & ComponentMappingMask));
        }
    }

    internal class Includer : SharpDX.D3DCompiler.Include
    {
        public IDisposable? Shadow { get; set; }

        public void Close(Stream stream)
        {
            stream.Close();
        }

        public void Dispose()
        {
        }

        public Stream Open(SharpDX.D3DCompiler.IncludeType type, string fileName, Stream parentStream)
        {
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                MemoryStream stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);

                writer.Write(streamReader.ReadToEnd());
                writer.Flush();
                stream.Position = 0;

                return stream;
            }
        }
    }
}
