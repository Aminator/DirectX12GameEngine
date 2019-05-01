using System;
using System.Collections.Specialized;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneInstance : EntityManager
    {
        private Scene? rootScene;

        public SceneInstance(IServiceProvider services) : base(services)
        {
        }

        public SceneInstance(IServiceProvider services, Scene rootScene) : this(services)
        {
            RootScene = rootScene;
        }

        public Scene? RootScene
        {
            get => rootScene;
            set
            {
                if (rootScene == value) return;

                if (rootScene != null)
                {
                    Remove(rootScene);
                }

                if (value != null)
                {
                    Add(value);
                }

                rootScene = value;
            }
        }

        private void Add(Scene scene)
        {
            foreach (Entity entity in scene.Entities)
            {
                Add(entity);
            }

            foreach (Scene childScene in scene.Children)
            {
                Add(childScene);
            }

            scene.Children.CollectionChanged += Children_CollectionChanged;
            scene.Entities.CollectionChanged += Entities_CollectionChanged;
        }

        private void Remove(Scene scene)
        {
            scene.Entities.CollectionChanged -= Entities_CollectionChanged;
            scene.Children.CollectionChanged -= Children_CollectionChanged;

            foreach (Scene childScene in scene.Children)
            {
                Remove(childScene);
            }

            foreach (Entity entity in scene.Entities)
            {
                Remove(entity);
            }
        }

        private void Entities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Entity entity in e.NewItems)
                    {
                        Add(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Entity entity in e.OldItems)
                    {
                        Remove(entity);
                    }
                    break;
            }
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Scene scene in e.NewItems)
                    {
                        Add(scene);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Scene scene in e.OldItems)
                    {
                        Remove(scene);
                    }
                    break;
            }
        }
    }
}
