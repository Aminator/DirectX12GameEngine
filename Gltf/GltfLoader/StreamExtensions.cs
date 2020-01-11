using System.IO;

namespace GltfLoader
{
    internal static class StreamExtensions
    {
        public static void Align(this Stream stream, int size, byte fillByte = 0)
        {
            var mod = stream.Position % size;

            if (mod == 0)
            {
                return;
            }

            for (var i = 0; i < size - mod; i++)
            {
                stream.WriteByte(fillByte);
            }
        }
    }
}
