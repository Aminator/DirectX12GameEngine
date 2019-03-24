using System.Collections.Generic;
using System.Linq;

namespace DirectX12GameEngine.Engine
{
    public class EntitySystemCollection : List<EntitySystem>
    {
        public T? Get<T>() where T : EntitySystem
        {
            return this.OfType<T>().FirstOrDefault();
        }

        public class EntitySystemComparer : Comparer<EntitySystem>
        {
            public static new EntitySystemComparer Default { get; } = new EntitySystemComparer();

            public override int Compare(EntitySystem x, EntitySystem y)
            {
                return x.Order.CompareTo(y.Order);
            }
        }
    }
}
