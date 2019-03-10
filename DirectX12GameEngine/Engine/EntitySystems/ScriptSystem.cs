using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DirectX12GameEngine.Engine
{
    public class ScriptSystem : EntitySystem<ScriptComponent>
    {
        private readonly HashSet<ScriptComponent> scriptsToStart = new HashSet<ScriptComponent>();

        public ScriptSystem(IServiceProvider services) : base(services)
        {
            Order = -100000;

            Components.CollectionChanged += Components_CollectionChanged;
        }

        public override void Update(TimeSpan deltaTime)
        {
        }

        private void Add(ScriptComponent script)
        {
            script.Initialize(Services);

            scriptsToStart.Add(script);
        }

        private void Remove(ScriptComponent script)
        {
            bool startWasPending = scriptsToStart.Remove(script);

            if (!startWasPending)
            {
                script.Cancel();
            }
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (ScriptComponent script in e.NewItems)
                    {
                        Add(script);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (ScriptComponent script in e.OldItems)
                    {
                        Remove(script);
                    }
                    break;
            }
        }
    }
}
