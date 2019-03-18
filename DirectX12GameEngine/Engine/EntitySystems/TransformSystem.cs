using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Engine
{
    public sealed class TransformSystem : EntitySystem<TransformComponent>
    {
        internal HashSet<TransformComponent> TransformationRoots { get; } = new HashSet<TransformComponent>();

        public TransformSystem(IServiceProvider services) : base(services)
        {
            Order = -200;

            Components.CollectionChanged += Components_CollectionChanged;
        }

        public override void Draw(GameTime gameTime)
        {
            if (SceneSystem.RootScene != null)
            {
                UpdateTransformationsRecursive(SceneSystem.RootScene);
            }

            UpdateTransformations(TransformationRoots);
        }

        private void UpdateTransformations(IEnumerable<TransformComponent> transformationRoots)
        {
            Parallel.ForEach(transformationRoots, UpdateTransformationsRecursive);
        }

        private void UpdateTransformationsRecursive(TransformComponent transformComponent)
        {
            transformComponent.UpdateLocalMatrix();
            transformComponent.UpdateWorldMatrixInternal(false);

            foreach (TransformComponent child in transformComponent.Children)
            {
                UpdateTransformationsRecursive(child);
            }
        }

        private static void UpdateTransformationsRecursive(Scene scene)
        {
            scene.UpdateWorldMatrixInternal(false);

            foreach (Scene childScene in scene.Children)
            {
                UpdateTransformationsRecursive(childScene);
            }
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TransformComponent transformComponent in e.NewItems)
                    {
                        if (transformComponent.Parent is null)
                        {
                            TransformationRoots.Add(transformComponent);
                        }

                        foreach (Entity childEntity in transformComponent.ChildEntities)
                        {
                            InternalAddEntity(childEntity);
                        }

                        transformComponent.Children.CollectionChanged += Children_CollectionChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (TransformComponent transformComponent in e.OldItems)
                    {
                        transformComponent.Children.CollectionChanged -= Children_CollectionChanged;

                        foreach (Entity childEntity in transformComponent.ChildEntities)
                        {
                            InternalRemoveEntity(childEntity, false);
                        }

                        if (transformComponent.Parent is null)
                        {
                            TransformationRoots.Remove(transformComponent);
                        }
                    }
                    break;
            }
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TransformComponent transformComponent in e.NewItems)
                    {
                        if (transformComponent.IsMovingInsideRootScene)
                        {
                            if (transformComponent.Parent is null)
                            {
                                TransformationRoots.Add(transformComponent);
                            }
                        }
                        else
                        {
                            InternalAddEntity(transformComponent.Entity);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (TransformComponent transformComponent in e.OldItems)
                    {
                        if (transformComponent.IsMovingInsideRootScene)
                        {
                            if (transformComponent.Parent is null)
                            {
                                TransformationRoots.Remove(transformComponent);
                            }
                        }
                        else
                        {
                            InternalRemoveEntity(transformComponent.Entity, false);
                        }
                    }
                    break;
            }
        }
    }
}
