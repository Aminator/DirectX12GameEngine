using System;

namespace DirectX12GameEngine.Games
{
    public class GameTime
    {
        public GameTime()
        {
        }

        public GameTime(TimeSpan totalTime, TimeSpan elapsedTime)
        {
            Total = totalTime;
            Elapsed = elapsedTime;
        }

        internal void Update(TimeSpan totalTime, TimeSpan elapsedTime)
        {
            Total = totalTime;
            Elapsed = elapsedTime;
        }

        public TimeSpan Elapsed { get; private set; }

        public TimeSpan Total { get; private set; }
    }
}
