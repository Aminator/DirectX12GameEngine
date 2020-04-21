using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public abstract class GameBase : IGame
    {
        private readonly object tickLock = new object();

        private bool isExiting;
        private readonly Stopwatch stopwatch = new Stopwatch();

        protected GameBase(GameContext context)
        {
            ServiceCollection services = new ServiceCollection();

            context.ConfigureServices(services);
            ConfigureServices(services);

            Services = services.BuildServiceProvider();
            Content = Services.GetRequiredService<IContentManager>();
        }

        public IServiceProvider Services { get; }

        public IContentManager Content { get; }

        public IList<IGameSystem> GameSystems { get; } = new List<IGameSystem>();

        public GameTime Time { get; } = new GameTime();

        public bool IsRunning { get; private set; }

        public virtual void Dispose()
        {
            foreach (IGameSystem system in GameSystems)
            {
                system.Dispose();
            }
        }

        public void Run()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("This game is already running.");
            }

            IsRunning = true;

            Initialize();
            LoadContentAsync();

            stopwatch.Start();
            Time.Update(stopwatch.Elapsed, TimeSpan.Zero);

            BeginRun();
        }

        public void Exit()
        {
            if (IsRunning)
            {
                isExiting = true;

                lock (tickLock)
                {
                    CheckEndRun();
                }
            }
        }

        public void Tick()
        {
            lock (tickLock)
            {
                if (isExiting)
                {
                    CheckEndRun();
                    return;
                }

                try
                {
                    TimeSpan elapsedTime = stopwatch.Elapsed - Time.Total;
                    Time.Update(stopwatch.Elapsed, elapsedTime);

                    Update(Time);

                    BeginDraw();
                    Draw(Time);
                }
                finally
                {
                    EndDraw();

                    CheckEndRun();
                }
            }
        }

        public virtual void Initialize()
        {
        }

        public virtual Task LoadContentAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void BeginRun()
        {
        }

        public virtual void Update(GameTime gameTime)
        {
            foreach (IGameSystem system in GameSystems)
            {
                system.Update(gameTime);
            }
        }

        public virtual void BeginDraw()
        {
            foreach (IGameSystem system in GameSystems)
            {
                system.BeginDraw();
            }
        }

        public virtual void Draw(GameTime gameTime)
        {
            foreach (IGameSystem system in GameSystems)
            {
                system.Draw(gameTime);
            }
        }

        public virtual void EndDraw()
        {
            foreach (IGameSystem system in GameSystems)
            {
                system.EndDraw();
            }
        }

        public virtual void EndRun()
        {
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IGame>(this);
            services.AddSingleton<IContentManager, ContentManager>();
        }

        private void CheckEndRun()
        {
            if (isExiting && IsRunning)
            {
                EndRun();

                stopwatch.Stop();

                IsRunning = false;
                isExiting = false;
            }
        }
    }
}
