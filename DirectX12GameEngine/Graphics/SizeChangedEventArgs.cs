using System;
using Windows.Foundation;

namespace DirectX12GameEngine.Graphics
{
    public sealed class SizeChangedEventArgs : EventArgs
    {
        public SizeChangedEventArgs(Size size, Size resolutionScale)
        {
            Size = size;
            ResolutionScale = resolutionScale;
        }

        public Size Size { get; }

        public Size ResolutionScale { get; }
    }
}
