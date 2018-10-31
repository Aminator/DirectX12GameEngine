using System;
using System.Collections.Generic;

namespace DirectX12GameEngine
{
    public interface ICollector
    {
        ICollection<IDisposable> Disposables { get; }
    }
}
