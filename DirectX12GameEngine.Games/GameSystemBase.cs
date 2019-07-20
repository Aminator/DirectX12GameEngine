using System;
using System.Threading.Tasks;
namespace DirectX12GameEngine.Games
{
    public class GameSystemBase
    {
        public virtual void Dispose()
        {
        }

        public virtual void Initialize()
        {
        }

        public virtual Task LoadContentAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void BeginDraw()
        {
        }

        public virtual void Draw(GameTime gameTime)
        {
        }

        public virtual void EndDraw()
        {
        }
    }
}
