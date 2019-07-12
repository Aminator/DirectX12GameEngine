using System;
using System.Threading;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Editor.Messaging
{
    public interface IMessenger
    {
        void Register<TMessage>(object recipient, Action<TMessage> action);

        void Send<TMessage>(TMessage message);

        void Unregister(object recipient);

        void Unregister<TMessage>(object recipient);

        public Task<TMessage> WaitAsync<TMessage>(object recipient, CancellationToken cancellationToken);
    }
}
