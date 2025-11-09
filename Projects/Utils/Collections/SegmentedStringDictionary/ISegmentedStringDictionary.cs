using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Armat.Collections;

// represents an interface of string dictionary where keys can be grouped in segments
// it's useful for storing properties & settings grouped by categories
public interface ISegmentedStringDictionary : IDictionary<String, String>, IReadOnlyDictionary<String, String>
{
	public static readonly StringComparison KeyComparison = StringComparison.Ordinal;

	// SegmentedDictionaryKeys are formatted as Key@RootSegmentKey|ParentSegmentKey|ChildSegmentKey|LeafSegmentKey
	public const Char DictionaryKeySeparator = '@';     // separates key and segment key in a dictionary key
	public const Char SegmentKeySeparator = '|';        // separates segments in a segment key

	// ISegmentedDictionary interface
	Boolean ContainsKey(String key, String segmentKey);
	Boolean TryGetValue(String key, String segmentKey, [MaybeNullWhen(false)] out String value);
	String GetValue(String key, String segmentKey);

	void Add(String key, String segmentKey, String value);
	Boolean Remove(String key, String segmentKey);
	void SetValue(String key, String segmentKey, String value);
	void Clear(String segmentKey);

	// returns a view to the segment
	ISegmentedStringDictionary GetSegment(String segmentKey);

	static void ValidateSegmentedDictionaryKey(String segmentedDictionaryKey)
	{
		if (!DecomposeDictionaryKey(segmentedDictionaryKey, out String key, out String segmentKey))
			throw new ArgumentException("Invalid directory key format");

		ValidateSegmentedDictionaryKey(key, segmentKey);
	}

	static void ValidateSegmentedDictionaryKey(String key, String segmentKey)
	{
		// ensure the key is valid
		ValidateDictionaryKey(key);

		// ensure the segment key is valid
		ValidateSegmentKey(segmentKey);
	}

	static void ValidateDictionaryKey(String key)
	{
		if (key.Length == 0)
			throw new ArgumentException("Key cannot be empty", nameof(key));

		// ensure that the key doesn't contain any of DictionaryKeySeparator or SegmentKeySeparator character
		if (key.Contains(DictionaryKeySeparator) || key.Contains(SegmentKeySeparator))
			throw new ArgumentException("Key cannot contain any of DictionaryKeySeparator or SegmentKeySeparator characters", nameof(key));
	}

	static void ValidateSegmentKey(String segmentKey)
	{
		// ensure that the key doesn't contain DictionaryKeySeparator character
		if (segmentKey.Contains(DictionaryKeySeparator))
			throw new ArgumentException("Segment Key cannot contain DictionaryKeySeparator character", nameof(segmentKey));
	}

	public static String ComposeDictionaryKey(String key, String segmentKey)
	{
		// ensure the key is valid
		ValidateSegmentedDictionaryKey(key, segmentKey);

		return segmentKey.Length > 0 ?
			key + DictionaryKeySeparator + segmentKey :
			key;
	}

	public static Boolean DecomposeDictionaryKey(String dictionaryKey, out String key, out String segmentKey)
	{
		if (dictionaryKey.Length == 0)
		{
			key = String.Empty;
			segmentKey = String.Empty;
			return false;
		}

		Int32 sepIndex = dictionaryKey.IndexOf(DictionaryKeySeparator);
		if (sepIndex == -1)
		{
			// if there's no separator, then the segment key is an empty string
			key = dictionaryKey;
			segmentKey = String.Empty;
		}
		else
		{
			// key goes the first, then it comes the segment key
			key = dictionaryKey[..sepIndex];
			segmentKey = dictionaryKey[(sepIndex + 1)..];
		}

		return key.Length > 0;
	}

	public static Boolean DictionaryKeyBelongsToSegment(String dictionaryKey, String parentSegmentKey)
	{
		if (dictionaryKey.Length == 0)
			throw new ArgumentException("Dictionary Key cannot be empty", nameof(dictionaryKey));

		// all keys belong to the root segment
		if (parentSegmentKey.Length == 0)
			return true;

		// get the segment key out of the dictionary key
		if (DecomposeDictionaryKey(dictionaryKey, out _, out String segmentKey) && segmentKey.Length >= parentSegmentKey.Length)
		{
			// ensure that this segment belongs to the parent segment
			return segmentKey.StartsWith(parentSegmentKey, KeyComparison) &&
				(segmentKey.Length == parentSegmentKey.Length || segmentKey[parentSegmentKey.Length] == SegmentKeySeparator);
		}

		return false;
	}

	public static String ComposeSegmentKey(String segmentKey, String parentSegmentKey)
	{
		if (segmentKey.Length == 0)
			throw new ArgumentException("Segment key cannot be empty", nameof(segmentKey));

		return parentSegmentKey.Length > 0 ?
			parentSegmentKey + SegmentKeySeparator + segmentKey :
			segmentKey;
	}

	public static Boolean DecomposeLeafSegmentKey(String fullSegmentKey, out String leafSegmentKey, out String parentSegmentKey)
	{
		if (fullSegmentKey.Length == 0)
		{
			parentSegmentKey = String.Empty;
			leafSegmentKey = String.Empty;
			return false;
		}

		Int32 sepIndex = fullSegmentKey.LastIndexOf(SegmentKeySeparator);
		if (sepIndex == -1)
		{
			// if there's no separator, then the parent segment key is an empty string
			parentSegmentKey = String.Empty;
			leafSegmentKey = fullSegmentKey;
		}
		else
		{
			// parent segment key goes the first
			parentSegmentKey = fullSegmentKey[..sepIndex];
			leafSegmentKey = fullSegmentKey[(sepIndex + 1)..];
		}

		return true;
	}

	public static Boolean DecomposeRootSegmentKey(String fullSegmentKey, out String rootSegmentKey, out String childSegmentKey)
	{
		if (fullSegmentKey.Length == 0)
		{
			childSegmentKey = String.Empty;
			rootSegmentKey = String.Empty;
			return false;
		}

		Int32 sepIndex = fullSegmentKey.IndexOf(SegmentKeySeparator);
		if (sepIndex == -1)
		{
			// if there's no separator, then the parent segment key is an empty string
			childSegmentKey = String.Empty;
			rootSegmentKey = fullSegmentKey;
		}
		else
		{
			// parent segment key goes the first
			childSegmentKey = fullSegmentKey[..sepIndex];
			rootSegmentKey = fullSegmentKey[(sepIndex + 1)..];
		}

		return true;
	}

	public static Boolean DecomposeSegmentKey(String fullSegmentKey, String parentSegmentKey, out String childSegmentKey)
	{
		if (fullSegmentKey.Length == 0)
		{
			childSegmentKey = String.Empty;
			return false;
		}

		if (parentSegmentKey.Length == 0)
		{
			// if parent segment key is empty, then the child segment key is the full segment key
			childSegmentKey = fullSegmentKey;
			return true;
		}

		// ensure that this segment belongs to the parent segment
		if (!fullSegmentKey.StartsWith(parentSegmentKey, KeyComparison))
		{
			childSegmentKey = String.Empty;
			return false;
		}

		if (fullSegmentKey.Length == parentSegmentKey.Length)
		{
			// exact match
			childSegmentKey = String.Empty;
			return true;
		}
		else if (fullSegmentKey[parentSegmentKey.Length] == SegmentKeySeparator)
		{
			// valid child segment
			childSegmentKey = fullSegmentKey[(parentSegmentKey.Length + 1)..];
			return true;
		}

		// doesn't belong to the parent segment
		childSegmentKey = String.Empty;
		return false;
	}
}
