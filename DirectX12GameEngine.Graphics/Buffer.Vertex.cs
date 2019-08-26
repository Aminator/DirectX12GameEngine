using System;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer
    {
        public static class Vertex
        {
            public static unsafe Buffer New(GraphicsDevice device, int size, int structureByteStride, GraphicsHeapType heapType = GraphicsHeapType.Default)
            {
                return Buffer.New(device, size, structureByteStride, BufferFlags.VertexBuffer, heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, int elementCount, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount, BufferFlags.VertexBuffer, heapType: heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, in T data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.VertexBuffer, heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, Span<T> data, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.VertexBuffer, heapType);
            }

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, Span<T> data, int structureByteStride, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New(device, data, structureByteStride, BufferFlags.VertexBuffer, heapType);
            }
        }
    }
}
