using System;

namespace DirectX12GameEngine.Core
{
    public sealed class WindowHandle
    {
        public WindowHandle(AppContextType contextType, object nativeWindow, IntPtr handle = default)
        {
            ContextType = contextType;
            NativeWindow = nativeWindow;
            Handle = handle;
        }

        public AppContextType ContextType { get; }

        public object NativeWindow { get; }

        public IntPtr Handle { get; }
    }
}
