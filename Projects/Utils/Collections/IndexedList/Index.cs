using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Armat.Collections;

// all indexes implement this interface
// the interface is used by IndexedList to maintain list of all indexes irrespective of the Key types
// (this interface depends only on element type T)
// practically this interface should not be directly used by consumers, IIndex<TIndexType, T> should be used instead
public interface IIndexBase<T>
	where T : notnull
{
	String Id { get; }
	IndexedList<T> Owner { get; }
	IIndexReaderBase<T> IndexReader { get; }

	// is called to fully recalculate indexes
	void ReIndex();

	// number of elements in the index
	Int32 Count { get; }
}

// index interface representing
// indexed keys, values, and methods to lookup values by keys
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Name is set intentionally")]
public interface IIndex<TIndexType, T> : IIndexBase<T>, 
	IDictionary<TIndexType, T>, IReadOnlyDictionary<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	// new keyword is used for resolving ambiguity of appropriate properties of IDictionary and IReadOnlyDictionary
	new Int32 Count { get; }
	new T this[TIndexType key] { get; set; }

	new ICollection<TIndexType> Keys { get; }
	new ICollection<T> Values { get; }

	Int32 IndexOf(TIndexType key);
	new Boolean ContainsKey(TIndexType key);
	new Boolean TryGetValue(TIndexType key, [MaybeNullWhen(false)] out T value);

	IEqualityComparer<TIndexType> KeyComparer { get; }
}

// index interface representing
// indexed keys, values, and methods to lookup values by keys
// allows having multiple values associated with the same key
public interface IMultiIndex<TIndexType, T> : IIndex<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	Int32 GetCountByKey(TIndexType key);
	IReadOnlyCollection<Int32> IndexesOf(TIndexType key);
	IReadOnlyCollection<T> GetValuesByKey(TIndexType key);
}

// a delegate method to read index (key) from the value
// is used to extract keys for indexing when adding / setting IndexedList items
public delegate TIndexType IndexReaderDelegate<TIndexType, T>(T item)
	where T : notnull
	where TIndexType : notnull;

// base index reader interface used to read index (key) from the value
// used to initialize IIndexBase<T> for automatically retrieving keys from values
// practically this interface should not be directly used by consumers, IIndexReader<TIndexType, T> should be used instead
public interface IIndexReaderBase<T>
	where T : notnull
{
	Object GetIndexValue(T item);
}

// index reader interface used to read index (key) from the value
// creation of IndexedList<T> indexes requires to provide instance of this type
// one could provide own implementation to return a property to be used for indexing
public interface IIndexReader<TIndexType, T> : IIndexReaderBase<T>
	where T : notnull
	where TIndexType : notnull
{
	new TIndexType GetIndexValue(T item);

	// provide default implementation for the base interface method
	Object IIndexReaderBase<T>.GetIndexValue(T item)
	{
		TIndexType key = this.GetIndexValue(item);
		return key;
	}
}

// internal data container of IndexedList
// is used by indexes to access internal data of IndexedList
// allows conversion of internal <-> external indexes - considering IndexedList may expose those elements in a different order
// This list may be sparse, do not iterate through that list, because it might have removed / obsolete elements
public interface IIndexedListAccessor<T>
	where T : notnull
{
	// returns the count of valid entries in the list 
	Int32 InternalCount { get; }
	Int32 ExternalCount { get; }
	T this[Int32 internalIndex] { get; set; }

	void Add(T item);
	void RemoveAt(Int32 internalIndex);
	void Clear();

	Int32 ToInternalIndex(Int32 externalIndex);
	Int32 ToExternalIndex(Int32 internalIndex);
}

// must be supported by all index classes implementing IIndex<TIndexType, T> interface
// is separated from IIndex<TIndexType, T> to hide the methods from the consumers, those should not be used directly
public interface IIndexInitializer<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	// is called only once by IndexedList when creating the index
	void Initialize(String id, IndexedList<T> owner, 
		IIndexedListAccessor<T> accessor, IIndexReader<TIndexType, T> indexReader);
}

// must be supported by all index classes implementing IIndex<TIndexType, T> interface
// is separated from IIndex<TIndexType, T> to hide the methods from the consumers, those should not be used directly
public interface IIndexCloner<T>
	where T : notnull
{
	// is called to clone the index object when cloning the IndexedList
	IIndexBase<T> Clone(IndexedList<T> owner, IIndexedListAccessor<T> data);
}
