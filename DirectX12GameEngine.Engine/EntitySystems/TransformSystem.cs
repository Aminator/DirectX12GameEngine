using System.Collections.Generic;

namespace DirectX12GameEngine.Engine
{
    public sealed class TransformSystem : EntitySystem<TransformComponent>
    {
        private readonly HashSet<TransformComponent> transformationRoots = new HashSet<TransformComponent>();

        public TransformSystem()
        {
        }

        public override void BeginDraw()
        {
            UpdateTransformations(transformationRoots);
        }

        protected override void OnEntityComponentAdded(TransformComponent component)
        {
            if (component.Parent is null)
            {
                transformationRoots.Add(component);
            }
        }

        protected override void OnEntityComponentRemoved(TransformComponent component)
        {
            transformationRoots.Remove(component);
        }

        private void UpdateTransformations(IEnumerable<TransformComponent> transformationRoots)
        {
            foreach (TransformComponent transformComponent in transformationRoots)
            {
                UpdateTransformationsRecursive(transformComponent);
            }
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
