using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class CompiledCommandList
    {
        internal CompiledCommandList(CommandList builder, ID3D12CommandAllocator nativeCommandAllocator, ID3D12GraphicsCommandList nativeCommandList)
        {
            Builder = builder;
            NativeCommandAllocator = nativeCommandAllocator;
            NativeCommandList = nativeCommandList;
        }

        internal CommandList Builder { get; }

        internal ID3D12CommandAllocator NativeCommandAllocator { get; }

        internal ID3D12GraphicsCommandList NativeCommandList { get; }
    }
}
