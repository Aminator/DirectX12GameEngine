using System.Collections.Generic;

namespace DirectX12GameEngine.Input
{
    public interface IInputSourceConfiguration
    {
        public IList<IInputSource> Sources { get; }
    }
}
