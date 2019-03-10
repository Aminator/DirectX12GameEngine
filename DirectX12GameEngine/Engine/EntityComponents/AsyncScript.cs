using System.Threading.Tasks;

namespace DirectX12GameEngine.Engine
{
    public abstract class AsyncScript : ScriptComponent
    {
        public abstract Task ExecuteAsync();
    }
}
