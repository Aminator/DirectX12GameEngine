using System;

namespace DirectX12GameEngine.Graphics
{
    public partial class GraphicsBuffer
    {
        public static class Index
        {
            public static unsafe GraphicsBuffer New(GraphicsDevice device, int size, int structureByteStride, GraphicsHeapType heapType = GraphicsHeapType.Default)
            {
                return GraphicsBuffer.New(device, size, structureByteStride, BufferFlags.IndexBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, int elementCount, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New<T>(device, elementCount, BufferFlags.IndexBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, in T data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, BufferFlags.IndexBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, Span<T> data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, BufferFlags.IndexBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, Span<T> data, int structureByteStride, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, structureByteStride, BufferFlags.IndexBuffer, heapType);
            }
        }
    }
}
