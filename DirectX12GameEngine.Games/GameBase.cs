using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public abstract class GameBase : IDisposable
    {
        private readonly object tickLock = new object();

        private DateTime previousTime;
        private TimeSpan totalTime;

        public GameBase(GameContext gameContext)
        {
            Context = gameContext;

            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);

            Services = services.BuildServiceProvider();

            Window = GameWindow.Create(this);

            Content = Services.GetRequiredService<ContentManager>();
            GameSystems = Services.GetRequiredService<List<GameSystemBase>>();
        }

        public ContentManager Content { get; }

        public GameContext Context { get; }

        public IList<GameSystemBase> GameSystems { get; }

        public GameWindow Window { get; }

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

            Window.Run();
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

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<GameBase>(this);
            services.AddSingleton<ContentManager>();
            services.AddSingleton<List<GameSystemBase>>();
        }
    }
}
