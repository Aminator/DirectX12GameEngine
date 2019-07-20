using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace DirectX12GameEngine.Editor.Messaging
{
    public class Messenger : IMessenger
    {
        private static IMessenger defaultInstance;

        private readonly Dictionary<Type, Dictionary<object, List<Action<object>>>> registeredRecipientsPerMessageType = new Dictionary<Type, Dictionary<object, List<Action<object>>>>();

        public static IMessenger Default
        {
            get => defaultInstance ?? (defaultInstance = new Messenger());
            set => defaultInstance = value;
        }   

        public void Register<TMessage>(object recipient, Action<TMessage> action)
        {
            Type messageType = typeof(TMessage);

            if (!registeredRecipientsPerMessageType.TryGetValue(messageType, out var registeredActionsPerRecipient))
            {
                registeredActionsPerRecipient = new Dictionary<object, List<Action<object>>>();
                registeredRecipientsPerMessageType.Add(messageType, registeredActionsPerRecipient);
            }

            if (!registeredActionsPerRecipient.TryGetValue(recipient, out var registeredActions))
            {
                registeredActions = new List<Action<object>>();
                registeredActionsPerRecipient.Add(recipient, registeredActions);
            }

            registeredActions.Add(x => action((TMessage)x));
        }

        public void Send<TMessage>(TMessage message)
        {
            Type messageType = typeof(TMessage);

            if (registeredRecipientsPerMessageType.TryGetValue(messageType, out var registeredActionsPerRecipient))
            {
                foreach (var registeredActions in registeredActionsPerRecipient.Values)
                {
                    foreach (var action in registeredActions)
                    {
                        action(message);
                    }
                }
            }
        }

        public void Unregister(object recipient)
        {
            foreach (var registeredActionsPerRecipient in registeredRecipientsPerMessageType.Values)
            {
                registeredActionsPerRecipient.Remove(recipient);
            }
        }

        public void Unregister<TMessage>(object recipient)
        {
            Type messageType = typeof(TMessage);

            if (registeredRecipientsPerMessageType.TryGetValue(messageType, out var registeredActionsPerRecipient))
            {
                registeredActionsPerRecipient.Remove(recipient);
            }
        }

        public async Task<TMessage> WaitAsync<TMessage>(object recipient, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<TMessage> tcs = new TaskCompletionSource<TMessage>();
            Register<TMessage>(recipient, m => tcs.TrySetResult(m));

            TMessage result = await tcs.Task.ContinueWith(t => t.Status == TaskStatus.RanToCompletion ? t.Result : default, cancellationToken);
            Unregister<TMessage>(recipient);

            return result;
        }
    }
}
