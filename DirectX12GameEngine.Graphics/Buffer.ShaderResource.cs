using System;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer
    {
        public static class ShaderResource
        {
            public static unsafe Buffer New(GraphicsDevice device, int size, GraphicsHeapType heapType = GraphicsHeapType.Default)
            {
                return Buffer.New(device, size, BufferFlags.ShaderResource, heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, int elementCount, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount, BufferFlags.ShaderResource, heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, in T data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.ShaderResource, heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, Span<T> data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.ShaderResource, heapType);
            }
        }
    }
}
