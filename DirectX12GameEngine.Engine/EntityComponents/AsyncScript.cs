using System;
using System.Threading;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Engine
{
    public abstract class AsyncScript : ScriptComponent
    {
        public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? throw new InvalidOperationException();

        internal CancellationTokenSource? CancellationTokenSource { get; set; }

        public abstract Task ExecuteAsync();
    }
}
