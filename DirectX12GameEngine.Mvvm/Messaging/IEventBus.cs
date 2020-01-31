using System;

namespace DirectX12GameEngine.Mvvm.Messaging
{
    public interface IEventBus
    {
        public IEvent<TEventArgs> GetEvent<TEventArgs>();

        public void Publish<TEventArgs>(object sender, TEventArgs e);

        public void Subscribe<TEventArgs>(EventHandler<TEventArgs> handler);

        public void Unsubscribe<TEventArgs>(EventHandler<TEventArgs> handler);
    }

    public interface IEvent<TEventArgs>
    {
        public event EventHandler<TEventArgs>? Invoked;

        public void Publish(object sender, TEventArgs e);
    }
}
