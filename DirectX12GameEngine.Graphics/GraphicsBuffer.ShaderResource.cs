using System;

namespace DirectX12GameEngine.Graphics
{
    public partial class GraphicsBuffer
    {
        public static class ShaderResource
        {
            public static unsafe GraphicsBuffer New(GraphicsDevice device, int size, GraphicsHeapType heapType = GraphicsHeapType.Default)
            {
                return GraphicsBuffer.New(device, size, BufferFlags.ShaderResource, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, int elementCount, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New<T>(device, elementCount, BufferFlags.ShaderResource, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, in T data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, BufferFlags.ShaderResource, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, Span<T> data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, BufferFlags.ShaderResource, heapType);
            }
        }
    }
}
