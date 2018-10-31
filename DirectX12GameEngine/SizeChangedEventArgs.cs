using System;

namespace DirectX12GameEngine
{
    public sealed class SizeChangedEventArgs : EventArgs
    {
        public SizeChangedEventArgs(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Height { get; }

        public int Width { get; }
    }
}
