using System;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer
    {
        public static class Constant
        {
            public static unsafe Buffer New(GraphicsDevice device, int size, GraphicsResourceUsage usage = GraphicsResourceUsage.Upload)
            {
                return Buffer.New(device, size, BufferFlags.ConstantBuffer, usage);
            }

            public static unsafe Buffer New<T>(GraphicsDevice device, in T data, GraphicsResourceUsage usage = GraphicsResourceUsage.Upload) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.ConstantBuffer, usage);
            }

            public static unsafe Buffer New<T>(GraphicsDevice device, Span<T> data, GraphicsResourceUsage usage = GraphicsResourceUsage.Upload) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.ConstantBuffer, usage);
            }
        }
    }
}
