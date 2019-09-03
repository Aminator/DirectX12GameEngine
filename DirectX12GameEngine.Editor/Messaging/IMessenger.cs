using System;
using System.Threading;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Editor.Messaging
{
    public interface IMessenger
    {
        void Register<TMessage>(object recipient, Action<TMessage> action) where TMessage : notnull;

        void Send<TMessage>(TMessage message) where TMessage : notnull;

        void Unregister(object recipient);

        void Unregister<TMessage>(object recipient) where TMessage : notnull;

        public Task<TMessage> WaitAsync<TMessage>(object recipient, CancellationToken cancellationToken) where TMessage : notnull;
    }
}
