using System;
using System.Runtime.CompilerServices;
using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer
    {
        public static class Vertex
        {
            public static unsafe Buffer New(GraphicsDevice device, int size, int structuredByteStride, GraphicsHeapType heapType = GraphicsHeapType.Default)
            {
                return Buffer.New(device, size, structuredByteStride, BufferFlags.VertexBuffer, heapType);
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

            public static unsafe Buffer<T> New<T>(GraphicsDevice device, Span<T> data, int structuredByteStride, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
            {
                return Buffer.New(device, data, structuredByteStride, BufferFlags.VertexBuffer, heapType);
            }
        }
    }
}
