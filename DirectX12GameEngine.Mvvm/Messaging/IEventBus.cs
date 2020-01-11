using System;

namespace DirectX12GameEngine.Mvvm.Messaging
{
    public interface IEventBus
    {
        public IEvent<TEventArgs> GetEvent<TEventArgs>();

        public void Publish<TEventArgs>(TEventArgs e);

        public void Subscribe<TEventArgs>(EventHandler<TEventArgs> eventHandler);

        public void Unsubscribe<TEventArgs>(EventHandler<TEventArgs> eventHandler);
    }
}
