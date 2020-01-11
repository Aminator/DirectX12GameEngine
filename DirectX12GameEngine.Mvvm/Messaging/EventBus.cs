using System;
using System.Collections.Generic;

namespace DirectX12GameEngine.Mvvm.Messaging
{
    public class EventBus
    {
        private readonly Dictionary<Type, object> events = new Dictionary<Type, object>();

        public IEvent<TEventArgs> GetEvent<TEventArgs>()
        {
            if (!events.TryGetValue(typeof(TEventArgs), out object eventObject))
            {
                eventObject = new Event<TEventArgs>();
                events.Add(typeof(TEventArgs), eventObject);
            }

            return (IEvent<TEventArgs>)eventObject;
        }

        public void Publish<TEventArgs>(TEventArgs e)
        {
            GetEvent<TEventArgs>().Publish(e);
        }

        public void Subscribe<TEventArgs>(EventHandler<TEventArgs> eventHandler)
        {
            GetEvent<TEventArgs>().MessageReceived += eventHandler;
        }

        public void Unsubscribe<TEventArgs>(EventHandler<TEventArgs> eventHandler)
        {
            GetEvent<TEventArgs>().MessageReceived -= eventHandler;
        }
    }

    public interface IEvent<TEventArgs>
    {
        public event EventHandler<TEventArgs>? MessageReceived;

        public void Publish(TEventArgs e);
    }

    public class Event<TEventArgs> : IEvent<TEventArgs>
    {
        public event EventHandler<TEventArgs>? MessageReceived;

        public void Publish(TEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }
}
