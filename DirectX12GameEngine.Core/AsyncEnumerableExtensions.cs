using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Core
{
    public static class AsyncEnumerableExtensions
    {
        public static async ValueTask<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source)
        {
            int count = 0;

            await using (IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator())
            {
                checked
                {
                    while (await e.MoveNextAsync())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static async ValueTask<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            int count = 0;

            await foreach (TSource element in source)
            {
                checked
                {
                    if (predicate(element))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static async ValueTask<long> LongCountAsync<TSource>(this IAsyncEnumerable<TSource> source)
        {
            long count = 0;

            await using (IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator())
            {
                checked
                {
                    while (await e.MoveNextAsync())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static async ValueTask<long> LongCountAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            long count = 0;

            await foreach (TSource element in source)
            {
                checked
                {
                    if (predicate(element))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
