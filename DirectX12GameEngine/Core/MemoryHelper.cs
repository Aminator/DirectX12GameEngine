using System;

namespace DirectX12GameEngine.Core
{
    public static class MemoryHelper
    {
        public static void Copy(IntPtr source, IntPtr destination, int sizeInBytesToCopy)
        {
            SharpDX.Utilities.CopyMemory(destination, source, sizeInBytesToCopy);
        }

        public static unsafe void Copy<T>(Span<T> source, IntPtr destination) where T : unmanaged
        {
            fixed (T* pointer = source)
            {
                Copy((IntPtr)pointer, destination, source.Length * sizeof(T));
            }
        }

        public static unsafe void Copy<T>(in T source, IntPtr destination) where T : unmanaged
        {
            fixed (T* pointer = &source)
            {
                Copy((IntPtr)pointer, destination, sizeof(T));
            }
        }
    }
}
