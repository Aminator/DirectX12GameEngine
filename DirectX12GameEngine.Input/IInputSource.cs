using System;

namespace DirectX12GameEngine.Input
{
    public interface IInputSource : IDisposable
    {
        void Scan();

        void Update();
    }
}
