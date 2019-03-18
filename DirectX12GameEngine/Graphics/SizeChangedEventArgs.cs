using System;
using System.Drawing;

namespace DirectX12GameEngine.Graphics
{
    public sealed class SizeChangedEventArgs : EventArgs
    {
        public SizeChangedEventArgs(Size size, double resolutionScale)
        {
            Size = size;
            ResolutionScale = resolutionScale;
        }

        public Size Size { get; }

        public double ResolutionScale { get; }
    }
}
