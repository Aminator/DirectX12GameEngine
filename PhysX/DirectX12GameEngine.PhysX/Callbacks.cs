using System;
using System.Runtime.InteropServices;
using SharpGen.Runtime;

namespace DirectX12GameEngine.PhysX
{
    public class PxDefaultAllocator : PxAllocatorCallback
    {
        public ShadowContainer? Shadow { get; set; }

        public IntPtr Allocate(PointerSize size, string typeName, string filename, int line)
        {
            var rawPointer = Marshal.AllocHGlobal((IntPtr)(size + 8));
            var alignedPointer = new IntPtr(16 * (((long)rawPointer + 15) / 16));

            return alignedPointer;
        }

        public void Deallocate(IntPtr pointer)
        {
            Marshal.FreeHGlobal(pointer);
        }

        public void Dispose()
        {
            Shadow?.Dispose();
        }
    }

    public class PxDefaultErrorCallback : PxErrorCallback
    {
        public ShadowContainer? Shadow { get; set; }

        public void ReportError(PxErrors code, string message, string file, int line)
        {
            throw new Exception(message);
        }

        public void Dispose()
        {
            Shadow?.Dispose();
        }
    }
}
