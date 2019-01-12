using System;

namespace DirectX12GameEngine
{
    public sealed class SizeChangedEventArgs : EventArgs
    {
        public SizeChangedEventArgs(double width, double height, double resolutionScale = 1.0)
        {
            Width = width;
            Height = height;
            ResolutionScale = resolutionScale;
        }

        public double Height { get; }

        public double ResolutionScale { get; }

        public double Width { get; }
    }
}
