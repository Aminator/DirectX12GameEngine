using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Engine
{
    public sealed class TransformSystem : EntitySystem<TransformComponent>
    {
        internal HashSet<TransformComponent> TransformationRoots { get; } = new HashSet<TransformComponent>();

        public TransformSystem()
        {
            Order = -200;
        }

        public override void Draw(GameTime gameTime)
        {
            UpdateTransformations(TransformationRoots);
        }

        protected override void OnEntityComponentAdded(TransformComponent component)
        {
            if (component.Parent is null)
            {
                TransformationRoots.Add(component);
            }
        }

        protected override void OnEntityComponentRemoved(TransformComponent component)
        {
            TransformationRoots.Remove(component);
        }

        private void UpdateTransformations(IEnumerable<TransformComponent> transformationRoots)
        {
            Parallel.ForEach(transformationRoots, UpdateTransformationsRecursive);
        }

        private void UpdateTransformationsRecursive(TransformComponent transformComponent)
        {
            transformComponent.UpdateLocalMatrix();
            transformComponent.UpdateWorldMatrixInternal(false);

            foreach (TransformComponent child in transformComponent)
            {
                UpdateTransformationsRecursive(child);
            }
        }
    }
}
