using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public class GameBase : IDisposable
    {
        private readonly object tickLock = new object();

        private DateTime previousTime;
        private TimeSpan totalTime;

        public GameBase(GameContext gameContext)
        {
            GameContext = gameContext;

            Services = ConfigureServices().BuildServiceProvider();

            GameSystems = Services.GetRequiredService<List<GameSystemBase>>();
        }

        public GameContext GameContext { get; }

        public IList<GameSystemBase> GameSystems { get; }

        public IServiceProvider Services { get; }

        public GameTime Time { get; } = new GameTime();

        public virtual void Dispose()
        {
            foreach (GameSystemBase gameSystem in GameSystems)
            {
                gameSystem.Dispose();
            }
        }

        public void Run()
        {
            Initialize();
            LoadContentAsync();

            previousTime = DateTime.Now;

            switch (GameContext)
            {
#if WINDOWS_UWP
                case GameContextXaml context:
                    Windows.UI.Xaml.Media.CompositionTarget.Rendering += (s, e) => Tick();
                    return;
#elif NETCOREAPP
                case GameContextWinForms context:
                    System.Windows.Media.CompositionTarget.Rendering += (s, e) => Tick();
                    return;
#endif
            }

#if WINDOWS_UWP
            Windows.UI.Core.CoreWindow? coreWindow = (GameContext as GameContextCoreWindow)?.Control;
#endif

            while (true)
            {
#if WINDOWS_UWP
                coreWindow?.Dispatcher.ProcessEvents(Windows.UI.Core.CoreProcessEventsOption.ProcessAllIfPresent);
#endif
                Tick();
            }
        }

        public void Tick()
        {
            lock (tickLock)
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan elapsedTime = currentTime - previousTime;

                previousTime = currentTime;
                totalTime += elapsedTime;

                Time.Update(totalTime, elapsedTime);

                Update(Time);

                BeginDraw();
                Draw(Time);
                EndDraw();
            }
        }

        protected void Initialize()
        {
            foreach (GameSystemBase gameSystem in GameSystems)
            {
                gameSystem.Initialize();
            }
        }

        protected virtual Task LoadContentAsync()
        {
            List<Task> loadingTasks = new List<Task>(GameSystems.Count);

            foreach (GameSystemBase gameSystem in GameSystems)
            {
                loadingTasks.Add(gameSystem.LoadContentAsync());
            }

            return Task.WhenAll(loadingTasks);
        }

        protected virtual void Update(GameTime gameTime)
        {
            foreach (GameSystemBase gameSystem in GameSystems)
            {
                gameSystem.Update(gameTime);
            }
        }

        protected virtual void BeginDraw()
        {
            foreach (GameSystemBase gameSystem in GameSystems)
            {
                gameSystem.BeginDraw();
            }
        }

        protected virtual void Draw(GameTime gameTime)
        {
            foreach (GameSystemBase gameSystem in GameSystems)
            {
                gameSystem.Draw(gameTime);
            }
        }

        protected virtual void EndDraw()
        {
            foreach (GameSystemBase gameSystem in GameSystems)
            {
                gameSystem.EndDraw();
            }
        }

        protected virtual IServiceCollection ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<List<GameSystemBase>>();
        }
    }
}
