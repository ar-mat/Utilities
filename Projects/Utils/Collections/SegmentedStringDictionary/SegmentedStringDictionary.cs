using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Armat.Collections;

public class SegmentedStringDictionary : ISegmentedStringDictionary
{
	#region Constructors

	protected SegmentedStringDictionary(IDictionary<String, String> dictionary, Boolean readOnly)
	{
		_stringDictionary = dictionary;
		_readOnly = readOnly;
	}

	public static SegmentedStringDictionary Create()
	{
		// create an underlying dictionary
		StringComparer comparer = StringComparer.FromComparison(ISegmentedStringDictionary.KeyComparison);
		Dictionary<String, String> dic = new(comparer);

		// create an instance
		return new SegmentedStringDictionary(dic, false);
	}
	public static SegmentedStringDictionary Attach(IDictionary<String, String> stringDictionary)
	{
		// create an instance
		return new SegmentedStringDictionary(stringDictionary, false);
	}
	public static SegmentedStringDictionary Attach(IDictionary<String, String> stringDictionary, Boolean readOnly)
	{
		// create an instance
		return new SegmentedStringDictionary(stringDictionary, readOnly);
	}

	#endregion // Constructors

	#region Data Members

	private readonly IDictionary<String, String> _stringDictionary;
	private readonly Boolean _readOnly;

	#endregion // Data Members

	#region ISegmentedStringDictionary implementation

	public virtual String this[String key]
	{
		get
		{
			return _stringDictionary[key];
		}
		set
		{
			if (_readOnly)
				throw new InvalidOperationException("Collection is read only");

			// will throw in case of invalid key
			ISegmentedStringDictionary.ValidateSegmentedDictionaryKey(key);

			_stringDictionary[key] = value;
		}
	}

	public virtual ICollection<String> Keys
	{
		get => _stringDictionary.Keys;
	}

	IEnumerable<String> IReadOnlyDictionary<String, String>.Keys
	{
		get
		{
			return _stringDictionary.Keys;
		}
	}

	public virtual ICollection<String> Values
	{
		get => _stringDictionary.Values;
	}

	IEnumerable<String> IReadOnlyDictionary<String, String>.Values
	{
		get
		{
			return _stringDictionary.Values;
		}
	}

	public virtual Int32 Count
	{
		get => _stringDictionary.Count;
	}

	public virtual Boolean IsReadOnly => _readOnly;

	public virtual void Add(String key, String value)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		// will throw in case of invalid key
		ISegmentedStringDictionary.ValidateSegmentedDictionaryKey(key);

		_stringDictionary.Add(key, value);
	}

	public virtual void Add(KeyValuePair<String, String> item)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		// will throw in case of invalid key
		ISegmentedStringDictionary.ValidateSegmentedDictionaryKey(item.Key);

		_stringDictionary.Add(item);
	}

	public virtual void Add(String key, String segmentKey, String value)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		// will generate the dictionary key and throw in case of invalid value
		String dictKey = ISegmentedStringDictionary.ComposeDictionaryKey(key, segmentKey);

		_stringDictionary.Add(dictKey, value);
	}

	public virtual void Clear(String segmentKey)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		if (segmentKey.Length == 0)
		{
			// clear the whole dictionary if root segment is being cleared
			_stringDictionary.Clear();
		}
		else
		{
			// find keys within the segment
			String[] keysToRemove = _stringDictionary.Keys
				.Where(key => ISegmentedStringDictionary.DictionaryKeyBelongsToSegment(key, segmentKey))
				.ToArray();

			// remove filtered keys
			foreach (String key in keysToRemove)
				_stringDictionary.Remove(key);
		}
	}

	public virtual void Clear()
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		_stringDictionary.Clear();
	}

	public virtual Boolean Contains(KeyValuePair<String, String> item)
	{
		return _stringDictionary.Contains(item);
	}

	public virtual Boolean ContainsKey(String key, String segmentKey)
	{
		String dictKey = ISegmentedStringDictionary.ComposeDictionaryKey(key, segmentKey);
		return _stringDictionary.ContainsKey(dictKey);
	}

	public virtual Boolean ContainsKey(String key)
	{
		return _stringDictionary.ContainsKey(key);
	}

	public virtual void CopyTo(KeyValuePair<String, String>[] array, Int32 arrayIndex)
	{
		_stringDictionary.CopyTo(array, arrayIndex);
	}

	public virtual IEnumerator<KeyValuePair<String, String>> GetEnumerator()
	{
		return _stringDictionary.GetEnumerator();
	}

	public virtual String GetValue(String key, String segmentKey)
	{
		String dictKey = ISegmentedStringDictionary.ComposeDictionaryKey(key, segmentKey);
		return _stringDictionary[dictKey];
	}

	public virtual Boolean Remove(String key, String segmentKey)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		String dictKey = ISegmentedStringDictionary.ComposeDictionaryKey(key, segmentKey);
		return _stringDictionary.Remove(dictKey);
	}

	public virtual Boolean Remove(String key)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		return _stringDictionary.Remove(key);
	}

	public virtual Boolean Remove(KeyValuePair<String, String> item)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		return _stringDictionary.Remove(item);
	}

	public virtual void SetValue(String key, String segmentKey, String value)
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		// will generate the dictionary key and throw in case of invalid value
		String dictKey = ISegmentedStringDictionary.ComposeDictionaryKey(key, segmentKey);

		_stringDictionary[dictKey] = value;
	}

	public virtual Boolean TryGetValue(String key, String segmentKey, [MaybeNullWhen(false)] out String value)
	{
		String dictKey = ISegmentedStringDictionary.ComposeDictionaryKey(key, segmentKey);
		return _stringDictionary.TryGetValue(dictKey, out value);
	}

	public virtual Boolean TryGetValue(String key, [MaybeNullWhen(false)] out String value)
	{
		return _stringDictionary.TryGetValue(key, out value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_stringDictionary).GetEnumerator();
	}

	public IDictionary<String, String> GetInnerDictionary()
	{
		if (_readOnly)
			throw new InvalidOperationException("Collection is read only");

		return _stringDictionary;
	}

	public IReadOnlyDictionary<String, String> GetInnerReadOnlyDictionary()
	{
		return _stringDictionary is IReadOnlyDictionary<String, String> ro
			? ro
			: new System.Collections.ObjectModel.ReadOnlyDictionary<String, String>(_stringDictionary);
	}

	public virtual ISegmentedStringDictionary GetSegment(String segmentKey)
	{
		// return a segment view
		return new SegmentedStringDictionaryView(this, segmentKey);
	}

	#endregion // ISegmentedStringDictionary implementation
}

public static class SegmentedStringDictionaryExtensions
{
	public static String GetValueOrEmpty(this ISegmentedStringDictionary dictionary, String key, String segmentKey)
	{
		return GetValueOrDefault(dictionary, key, segmentKey, String.Empty);
	}

	public static String GetValueOrDefault(this ISegmentedStringDictionary dictionary, String key, String segmentKey, String defaultValue)
	{
		if (dictionary.TryGetValue(key, segmentKey, out String? value))
			return value;

		return defaultValue;
	}
}
