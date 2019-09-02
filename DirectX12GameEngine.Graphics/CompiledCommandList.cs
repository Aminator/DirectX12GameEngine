using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class CompiledCommandList
    {
        internal CompiledCommandList(CommandList builder, ID3D12CommandAllocator nativeCommandAllocator, ID3D12GraphicsCommandList nativeCommandList)
        {
            Builder = builder;
            NativeCommandAllocator = nativeCommandAllocator;
            NativeCommandList = nativeCommandList;
        }

        internal CommandList Builder { get; set; }

        internal ID3D12CommandAllocator NativeCommandAllocator { get; set; }

        internal ID3D12GraphicsCommandList NativeCommandList { get; set; }
    }
}
