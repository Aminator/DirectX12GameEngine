using System;

namespace DirectX12GameEngine.Games
{
    public abstract class GameSystem : IGameSystem
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool diposing)
        {
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
