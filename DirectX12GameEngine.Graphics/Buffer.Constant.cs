using System;

namespace DirectX12GameEngine.Graphics
{
    public partial class GraphicsBuffer
    {
        public static class Constant
        {
            public static unsafe GraphicsBuffer New(GraphicsDevice device, int size, GraphicsHeapType heapType = GraphicsHeapType.Upload)
            {
                return GraphicsBuffer.New(device, size, BufferFlags.ConstantBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, int elementCount, GraphicsHeapType heapType = GraphicsHeapType.Upload) where T : unmanaged
            {
                return GraphicsBuffer.New<T>(device, elementCount, BufferFlags.ConstantBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, in T data, GraphicsHeapType heapType = GraphicsHeapType.Upload) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, BufferFlags.ConstantBuffer, heapType);
            }

            public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, Span<T> data, GraphicsHeapType heapType = GraphicsHeapType.Upload) where T : unmanaged
            {
                return GraphicsBuffer.New(device, data, BufferFlags.ConstantBuffer, heapType);
            }
        }
    }
}
