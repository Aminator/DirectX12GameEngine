using System;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer
    {
        public static class Index
        {
            public static unsafe Buffer New(GraphicsDevice device, int size, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, size, BufferFlags.IndexBuffer, usage);
            }

            public static unsafe Buffer New<T>(GraphicsDevice device, in T data, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.IndexBuffer, usage);
            }

            public static unsafe Buffer New<T>(GraphicsDevice device, Span<T> data, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.IndexBuffer, usage);
            }

            public static IndexBufferView CreateIndexBufferView(Buffer indexBuffer, PixelFormat format, int size)
            {
                switch (format)
                {
                    case PixelFormat.R16_UInt:
                    case PixelFormat.R32_UInt:
                        break;
                    default:
                        throw new NotSupportedException("Index buffer type must be ushort or uint");
                }

                return new IndexBufferView
                {
                    BufferLocation = indexBuffer.NativeResource.GPUVirtualAddress,
                    Format = (Format)format,
                    SizeInBytes = size
                };
            }
        }
    }
}
