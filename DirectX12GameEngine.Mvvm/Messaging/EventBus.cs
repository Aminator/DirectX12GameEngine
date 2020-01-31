using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Mvvm.Messaging
{
    public class EventBus : IEventBus
    {
        private static IEventBus? defaultInstance;

        private readonly Dictionary<Type, object> events = new Dictionary<Type, object>();

        public static IEventBus Default
        {
            get => defaultInstance ?? (defaultInstance = new EventBus());
            set => defaultInstance = value;
        }

        public IEvent<TEventArgs> GetEvent<TEventArgs>()
        {
            if (!events.TryGetValue(typeof(TEventArgs), out object eventObject))
            {
                eventObject = new Event<TEventArgs>();
                events.Add(typeof(TEventArgs), eventObject);
            }

            return (IEvent<TEventArgs>)eventObject;
        }

        public void Publish<TEventArgs>(object sender, TEventArgs e)
        {
            GetEvent<TEventArgs>().Publish(sender, e);
        }

        public void Subscribe<TEventArgs>(EventHandler<TEventArgs> handler)
        {
            GetEvent<TEventArgs>().Invoked += handler;
        }

        public void Unsubscribe<TEventArgs>(EventHandler<TEventArgs> handler)
        {
            GetEvent<TEventArgs>().Invoked -= handler;
        }
    }

    public class Event<TEventArgs> : IEvent<TEventArgs>
    {
        private readonly EventHandlerList staticEventHandlers = new EventHandlerList();
        private readonly ConditionalWeakTable<object, EventHandlerList> instanceEventHandlers = new ConditionalWeakTable<object, EventHandlerList>();

        public event EventHandler<TEventArgs>? Invoked
        {
            add
            {
                if (value != null)
                {
                    if (value.Target is null)
                    {
                        staticEventHandlers.AddHandler(this, value);
                    }
                    else
                    {
                        EventHandlerList eventHandlers = instanceEventHandlers.GetOrCreateValue(value.Target);
                        eventHandlers.AddHandler(this, value);
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    if (value.Target is null)
                    {
                        staticEventHandlers.RemoveHandler(this, value);
                    }
                    else
                    {
                        if (instanceEventHandlers.TryGetValue(value.Target, out EventHandlerList eventHandlers))
                        {
                            eventHandlers.RemoveHandler(this, value);
                        }
                    }
                }
            }
        }

        public void Publish(object sender, TEventArgs e)
        {
            EventHandler<TEventArgs>? staticHandler = (EventHandler<TEventArgs>?)staticEventHandlers[this];
            staticHandler?.Invoke(sender, e);

            foreach (var item in (IEnumerable<KeyValuePair<object, EventHandlerList>>)(object)instanceEventHandlers)
            {
                EventHandler<TEventArgs>? instanceHandler = (EventHandler<TEventArgs>?)item.Value[this];
                instanceHandler?.Invoke(sender, e);
            }
        }
    }
}
