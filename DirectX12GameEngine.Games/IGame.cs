using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;

namespace DirectX12GameEngine.Games
{
    public interface IGame : IGameSystem
    {
        IServiceProvider Services { get; }

        IContentManager Content { get; }

        IList<IGameSystem> GameSystems { get; }

        GameTime Time { get; }

        bool IsRunning { get; }

        void Run();

        void Exit();

        void Tick();

        void Initialize();

        Task LoadContentAsync();

        void BeginRun();

        void EndRun();
    }
}
