using System;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer
    {
        public static class Vertex
        {
            public static unsafe Buffer New(GraphicsDevice device, int size, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, size, BufferFlags.VertexBuffer, usage);
            }

            public static unsafe Buffer New<T>(GraphicsDevice device, in T data, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.VertexBuffer, usage);
            }

            public static unsafe Buffer New<T>(GraphicsDevice device, Span<T> data, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.VertexBuffer, usage);
            }

            public static VertexBufferView CreateVertexBufferView(Buffer vertexBuffer, int size, int stride)
            {
                return new VertexBufferView
                {
                    BufferLocation = vertexBuffer.NativeResource.GPUVirtualAddress,
                    StrideInBytes = stride,
                    SizeInBytes = size
                };
            }

            public static unsafe VertexBufferView CreateVertexBufferView<T>(Buffer vertexBuffer, int size) where T : unmanaged
            {
                return CreateVertexBufferView(vertexBuffer, size, sizeof(T));
            }
        }
    }
}
