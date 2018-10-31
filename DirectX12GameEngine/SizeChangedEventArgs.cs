using System;

namespace DirectX12GameEngine
{
    public sealed class SizeChangedEventArgs : EventArgs
    {
        public SizeChangedEventArgs(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Height { get; }

        public double Width { get; }
    }
}
