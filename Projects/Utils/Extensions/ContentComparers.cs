using System;
using System.Collections.Generic;

namespace Armat.Utils.Extensions
{
	public static class ContentComparer
	{
		public static Boolean ContentsEquals<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
		{
			return ContentsEquals(first, second, EqualityComparer<TValue>.Default);
		}

		public static Boolean ContentsEquals<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second, IEqualityComparer<TValue> valueComparer)
		{
			if (first == second)
				return true;
			if (first.Count != second.Count)
				return false;

#pragma warning disable IDE0018 // Inline variable declaration
			TValue? secondValue;
#pragma warning restore IDE0018 // Inline variable declaration
			foreach (KeyValuePair<TKey, TValue> kvp in first)
			{
				if (!second.TryGetValue(kvp.Key, out secondValue))
					return false;
				if (!valueComparer.Equals(kvp.Value, secondValue))
					return false;
			}

			return true;
		}

		public static Boolean ContentsEquals<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> first, IReadOnlyDictionary<TKey, TValue> second)
		{
			return ContentsEquals(first, second, EqualityComparer<TValue>.Default);
		}

		public static Boolean ContentsEquals<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> first, IReadOnlyDictionary<TKey, TValue> second, IEqualityComparer<TValue> valueComparer)
		{
			if (first == second)
				return true;
			if (first.Count != second.Count)
				return false;

#pragma warning disable IDE0018 // Inline variable declaration
			TValue? secondValue;
#pragma warning restore IDE0018 // Inline variable declaration
			foreach (KeyValuePair<TKey, TValue> kvp in first)
			{
				if (!second.TryGetValue(kvp.Key, out secondValue))
					return false;
				if (!valueComparer.Equals(kvp.Value, secondValue))
					return false;
			}

			return true;
		}

		public static Boolean ContentsEquals<TItem>(this IReadOnlyCollection<TItem> first, IReadOnlyCollection<TItem> second)
		{
			return ContentsEquals(first, second, EqualityComparer<TItem>.Default);
		}

		public static Boolean ContentsEquals<TItem>(this IReadOnlyCollection<TItem> first, IReadOnlyCollection<TItem> second, IEqualityComparer<TItem> valueComparer)
		{
			if (first == second)
				return true;
			if (first.Count != second.Count)
				return false;

			return ContentsEquals((IEnumerable<TItem>)first, (IEnumerable<TItem>)second, valueComparer);
		}

		public static Boolean ContentsEquals<TItem>(this IEnumerable<TItem> first, IEnumerable<TItem> second)
		{
			return ContentsEquals(first, second, EqualityComparer<TItem>.Default);
		}

		public static Boolean ContentsEquals<TItem>(this IEnumerable<TItem> first, IEnumerable<TItem> second, IEqualityComparer<TItem> valueComparer)
		{
			if (first == second)
				return true;

			IEnumerator<TItem> enumFirst = first.GetEnumerator();
			IEnumerator<TItem> enumSecond = second.GetEnumerator();

			while (enumFirst.MoveNext())
			{
				if (!enumSecond.MoveNext())
					return false;

				if (!valueComparer.Equals(enumFirst.Current, enumSecond.Current))
					return false;
			}
			if (enumSecond.MoveNext())
				return false;

			return true;
		}

	}
}
