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
        }

        public override void Draw(GameTime gameTime)
        {
            if (EntityManager is SceneInstance sceneInstance && sceneInstance.RootScene != null)
            {
                UpdateTransformationsRecursive(sceneInstance.RootScene);
            }

            UpdateTransformations(TransformationRoots);
        }

        protected override void OnEntityComponentAdded(TransformComponent component)
        {
            if (component.Parent is null)
            {
                TransformationRoots.Add(component);
            }

            foreach (TransformComponent childTransform in component)
            {
                if (childTransform.Entity != null)
                {
                    InternalAddEntity(childTransform.Entity);
                }
            }

            component.Children.CollectionChanged += Children_CollectionChanged;
        }

        protected override void OnEntityComponentRemoved(TransformComponent component)
        {
            component.Children.CollectionChanged -= Children_CollectionChanged;

            foreach (TransformComponent childTransform in component)
            {
                if (childTransform.Entity != null)
                {
                    InternalRemoveEntity(childTransform.Entity, false);
                }
            }

            if (component.Parent is null)
            {
                TransformationRoots.Remove(component);
            }
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
                            if (transformComponent.Entity != null)
                            {
                                InternalAddEntity(transformComponent.Entity);
                            }
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
                            if (transformComponent.Entity != null)
                            {
                                InternalRemoveEntity(transformComponent.Entity, false);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
