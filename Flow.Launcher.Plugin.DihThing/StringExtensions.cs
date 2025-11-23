using System;

namespace Flow.Launcher.Plugin.DihThing
{
	/// <summary>
	/// Provides extension methods for string operations.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Computes the Levenshtein distance between two strings.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="target">The target string.</param>
		/// <returns>The Levenshtein distance between the two strings.</returns>
		public static int LevenshteinDistance(this string source, string target)
		{
			if (string.IsNullOrEmpty(source))
			{
				return string.IsNullOrEmpty(target) ? 0 : target.Length;
			}

			if (string.IsNullOrEmpty(target))
			{
				return source.Length;
			}

			var sourceLength = source.Length;
			var targetLength = target.Length;

			var distance = new int[sourceLength + 1, targetLength + 1];

			// Initialize the distance matrix
			for (var i = 0; i <= sourceLength; i++)
			{
				distance[i, 0] = i;
			}

			for (var j = 0; j <= targetLength; j++)
			{
				distance[0, j] = j;
			}

			for (var i = 1; i <= sourceLength; i++)
			{
				for (var j = 1; j <= targetLength; j++)
				{
					var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

					distance[i, j] = Math.Min(
						Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
						distance[i - 1, j - 1] + cost);
				}
			}

			return distance[sourceLength, targetLength];
		}
	}
}
