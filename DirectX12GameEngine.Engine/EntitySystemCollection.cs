using System.Collections.ObjectModel;
using System.Linq;

namespace DirectX12GameEngine.Engine
{
    public class EntitySystemCollection : Collection<EntitySystem>
    {
        public T Get<T>() where T : EntitySystem
        {
            return this.OfType<T>().FirstOrDefault();
        }
    }
}
