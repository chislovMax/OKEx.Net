using System;
using System.Collections.Generic;

namespace Okex.Net.Helpers
{
	public static class EnumerableExtender
	{
		public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (size < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(size));
			}

			return ChunkIterator(source, size);
		}

		private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
		{
			using IEnumerator<TSource> e = source.GetEnumerator();
			while (e.MoveNext())
			{
				var chunk = new TSource[size];
				chunk[0] = e.Current;

				int i = 1;
				for (; i < chunk.Length && e.MoveNext(); i++)
				{
					chunk[i] = e.Current;
				}

				if (i == chunk.Length)
				{
					yield return chunk;
				}
				else
				{
					Array.Resize(ref chunk, i);
					yield return chunk;
					yield break;
				}
			}
		}
	}
}
