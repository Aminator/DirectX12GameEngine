using System;

namespace DirectX12GameEngine.Games
{
    public interface IGameSystem : IDisposable
    {
        void Update(GameTime gameTime);

        void BeginDraw();

        void Draw(GameTime gameTime);

        void EndDraw();
    }
}
