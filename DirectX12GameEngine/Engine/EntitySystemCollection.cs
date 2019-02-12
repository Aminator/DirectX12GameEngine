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
    }
}
