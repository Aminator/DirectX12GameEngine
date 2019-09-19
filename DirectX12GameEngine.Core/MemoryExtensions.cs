using System;

namespace DirectX12GameEngine.Core
{
    public static class MemoryExtensions
    {
        public static unsafe void CopyTo(this IntPtr source, IntPtr destination, int sizeInBytesToCopy)
        {
            CopyTo(new ReadOnlySpan<byte>(source.ToPointer(), sizeInBytesToCopy), destination);
        }

        public static unsafe void CopyTo<T>(this Span<T> source, IntPtr destination) where T : unmanaged
        {
            source.CopyTo(new Span<T>(destination.ToPointer(), source.Length));
        }

        public static unsafe void CopyTo<T>(this ReadOnlySpan<T> source, IntPtr destination) where T : unmanaged
        {
            source.CopyTo(new Span<T>(destination.ToPointer(), source.Length));
        }

        public static unsafe void CopyTo<T>(this IntPtr source, Span<T> destination) where T : unmanaged
        {
            new Span<T>(source.ToPointer(), destination.Length).CopyTo(destination);
        }
    }
}
