using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Armat.Collections;

/// <summary>
/// Represents a view to a specific segment of the dictionary.
/// This class wraps the parent dictionary and automatically handles segment key composition.
/// </summary>
public sealed class SegmentedStringDictionaryView : ISegmentedStringDictionary
{
	private readonly SegmentedStringDictionary _parent;
	private readonly String _segmentKey;

	public SegmentedStringDictionaryView(SegmentedStringDictionary parent, String segmentKey)
	{
		ISegmentedStringDictionary.ValidateSegmentKey(segmentKey);

		_parent = parent;
		_segmentKey = segmentKey;
	}

	private String ToViewKey(String parentKey)
	{
		if (parentKey.Length == 0)
			throw new ArgumentException("Key cannot be empty", nameof(parentKey));

		if (ISegmentedStringDictionary.DecomposeDictionaryKey(parentKey, out String key, out String fullSegmentKey))
		{
			if (ISegmentedStringDictionary.DecomposeSegmentKey(fullSegmentKey, _segmentKey, out String childSegmentKey))
			{
				key = ISegmentedStringDictionary.ComposeSegmentKey(key, childSegmentKey);
			}
			else
			{
				key = String.Empty;
			}
		}
		else
		{
			key = String.Empty;
		}

		return key;
	}
	private String ToParentKey(String viewKey)
	{
		if (viewKey.Length == 0)
			throw new ArgumentException("Key cannot be empty", nameof(viewKey));

		if (ISegmentedStringDictionary.DecomposeDictionaryKey(viewKey, out String key, out String childSegmentKey))
		{
			String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(childSegmentKey, _segmentKey);
			key = ISegmentedStringDictionary.ComposeSegmentKey(key, fullSegmentKey);
		}
		else
		{
			key = String.Empty;
		}

		return key;
	}

	public String this[String key]
	{
		get => _parent[ToParentKey(key)];
		set => _parent[ToParentKey(key)] = value;
	}

	public ICollection<String> Keys
	{
		get
		{
			// filter keys that belong to this segment
			return _parent.GetInnerReadOnlyDictionary().Keys
				.Select(dictKey => ToViewKey(dictKey))
				.Where(dictKey => dictKey.Length > 0)
				.ToList();
		}
	}

	IEnumerable<String> IReadOnlyDictionary<String, String>.Keys => Keys;

	public ICollection<String> Values
	{
		get
		{
			// filter values for keys that belong to this segment
			return _parent.GetInnerReadOnlyDictionary()
				.Select(kvp => new KeyValuePair<String, String>(ToViewKey(kvp.Key), kvp.Value))
				.Where(kvp => kvp.Key.Length > 0)
				.Select(kvp => kvp.Value)
				.ToList();
		}
	}

	IEnumerable<String> IReadOnlyDictionary<String, String>.Values => Values;

	public Int32 Count
	{
		get
		{
			return _parent.GetInnerReadOnlyDictionary().Keys
				.Select(dictKey => ToViewKey(dictKey))
				.Count();
		}
	}

	public Boolean IsReadOnly => _parent.IsReadOnly;

	public void Add(String key, String value)
	{
		_parent.Add(ToParentKey(key), value);
	}

	public void Add(KeyValuePair<String, String> item)
	{
		_parent.Add(new KeyValuePair<String, String>(ToParentKey(item.Key), item.Value));
	}

	public void Add(String key, String segmentKey, String value)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		_parent.Add(key, fullSegmentKey, value);
	}

	public void Clear()
	{
		_parent.Clear(_segmentKey);
	}

	public void Clear(String segmentKey)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		_parent.Clear(fullSegmentKey);
	}

	public Boolean Contains(KeyValuePair<String, String> item)
	{
		item = new KeyValuePair<String, String>(ToParentKey(item.Key), item.Value);
		return _parent.Contains(item);
	}

	public Boolean ContainsKey(String key)
	{
		return _parent.ContainsKey(ToParentKey(key));
	}

	public Boolean ContainsKey(String key, String segmentKey)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		return _parent.ContainsKey(key, fullSegmentKey);
	}

	public void CopyTo(KeyValuePair<String, String>[] array, Int32 arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		if (arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		var items = _parent.GetInnerReadOnlyDictionary()
				.Select(kvp => new KeyValuePair<String, String>(ToViewKey(kvp.Key), kvp.Value))
				.Where(kvp => kvp.Key.Length > 0)
				.ToList();

		if (array.Length - arrayIndex < items.Count)
			throw new ArgumentException("The number of elements is greater than the available space.");

		foreach (var item in items)
		{
			array[arrayIndex++] = item;
		}
	}

	public IEnumerator<KeyValuePair<String, String>> GetEnumerator()
	{
		return _parent.GetInnerReadOnlyDictionary()
				.Select(kvp => new KeyValuePair<String, String>(ToViewKey(kvp.Key), kvp.Value))
				.Where(kvp => kvp.Key.Length > 0)
				.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public ISegmentedStringDictionary GetSegment(String segmentKey)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		return new SegmentedStringDictionaryView(_parent, fullSegmentKey);
	}

	public String GetValue(String key, String segmentKey)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		return _parent.GetValue(key, fullSegmentKey);
	}

	public Boolean Remove(String key)
	{
		return _parent.Remove(ToParentKey(key));
	}

	public Boolean Remove(String key, String segmentKey)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		return _parent.Remove(key, fullSegmentKey);
	}

	public Boolean Remove(KeyValuePair<String, String> item)
	{
		item = new KeyValuePair<String, String>(ToParentKey(item.Key), item.Value);

		return _parent.Remove(item);
	}

	public void SetValue(String key, String segmentKey, String value)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		_parent.SetValue(key, fullSegmentKey, value);
	}

	public Boolean TryGetValue(String key, [MaybeNullWhen(false)] out String value)
	{
		return _parent.TryGetValue(ToParentKey(key), out value);
	}

	public Boolean TryGetValue(String key, String segmentKey, [MaybeNullWhen(false)] out String value)
	{
		// compose the full segment key
		String fullSegmentKey = ISegmentedStringDictionary.ComposeSegmentKey(segmentKey, _segmentKey);
		return _parent.TryGetValue(key, fullSegmentKey, out value);
	}
}
