using System;

namespace DirectX12GameEngine
{
    public static class Extensions
    {
        public static T DisposeBy<T>(this T item, ICollector collector) where T : IDisposable
        {
            collector.Disposables.Add(item);
            return item;
        }
    }
}
