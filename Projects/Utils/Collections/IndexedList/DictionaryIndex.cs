using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace Armat.Collections;

public abstract class DictionaryIndex<TIndexType, T> : IndexBase<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	#region Constructors

	protected DictionaryIndex(IDictionary<TIndexType, Int32> indexMap, IEqualityComparer<TIndexType>? keyComparer)
		: base(keyComparer)
	{
		_indexMap = indexMap;
	}

	#endregion // Constructors

	#region Properties

	protected IDictionary<TIndexType, Int32> _indexMap;

	// cached key / value views; they wrap the current _indexMap instance
	// and must be reset whenever _indexMap is replaced
	private KeyCollection? _keysCollection;
	private ValueCollection? _valuesCollection;

	#endregion // Properties

	#region IIndex implementation

	protected abstract IDictionary<TIndexType, Int32> CreateIndexMap();
	protected abstract void EnsureIndexMapCapacity(IDictionary<TIndexType, Int32> map, Int32 capacity);

	protected override void OnInitialized()
	{
		base.OnInitialized();

		// drop views cached before initialization - they wrap the placeholder data accessor
		_keysCollection = null;
		_valuesCollection = null;
	}

	protected override void CopyFrom(IndexBase<TIndexType, T> sourceIndex)
	{
		if (_indexMap.Count > 0)
			throw new NotSupportedException("Cannot copy into non-empty index");

		DictionaryIndex<TIndexType, T> source = (DictionaryIndex<TIndexType, T>)sourceIndex;

		// copy current class data
		EnsureIndexMapCapacity(_indexMap, source._indexMap.Count);
		foreach (KeyValuePair<TIndexType, Int32> pair in source._indexMap)
			_indexMap.Add(pair.Key, pair.Value);
	}

	protected override void CopyToArray(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex)
	{
		KVCollection kVCollection = new(Data, _indexMap, Owner.ValueComparer);
		kVCollection.CopyTo(array, arrayIndex);
	}

	protected override void RecomputeIndex()
	{
		// create new index
		IDictionary<TIndexType, Int32> newIndex = CreateIndexMap();
		EnsureIndexMapCapacity(newIndex, _indexMap.Count);

		// compute the index
		foreach (KeyValuePair<TIndexType, Int32> pair in _indexMap)
		{
			Int32 index = pair.Value;
			T value = Data[index];
			TIndexType key = IndexReader.GetIndexValue(value);

			newIndex.Add(key, index);
		}

		// update index in this index
		_indexMap = newIndex;

		// the cached key / value views wrap the replaced map instance - reset them
		_keysCollection = null;
		_valuesCollection = null;
	}

	public override ICollection<TIndexType> Keys
	{
		get => _keysCollection ??= new KeyCollection(Data, _indexMap, Owner.ValueComparer);
	}

	public override ICollection<T> Values
	{
		get => _valuesCollection ??= new ValueCollection(Data, _indexMap, Owner.ValueComparer);
	}

	public override IEnumerator<KeyValuePair<TIndexType, T>> GetEnumerator()
	{
		KVCollection kVCollection = new(Data, _indexMap, Owner.ValueComparer);
		return (IEnumerator<KeyValuePair<TIndexType, T>>)kVCollection.GetEnumerator();
	}

	protected override Int32 IndexOfKey(TIndexType key)
	{
		if (_indexMap.TryGetValue(key, out Int32 index))
			return index;

		return -1;
	}

	#endregion // IIndex implementation

	#region IListChangeHandler implementation

	protected override Object? OnBeginInsertValue(Int32 index, T value)
	{
		// add the value
		TIndexType key = IndexReader.GetIndexValue(value);
		_indexMap.Add(key, index);

		return key;
	}

	protected override void OnCommitInsertValue(Int32 index, T value, Object? state)
	{
		// nothing to do
	}

	protected override void OnRollbackInsertValue(Int32 index, T value, Object? state)
	{
		// remove the added key
		_indexMap.Remove((TIndexType)state!);
	}

	protected override Object? OnBeginRemoveValue(Int32 index, T prevValue)
	{
		// compute the key up front: the user-provided IndexReader may throw,
		// and Begin is the only phase where a failure can safely cancel the operation
		return IndexReader.GetIndexValue(prevValue);
	}

	protected override void OnCommitRemoveValue(Int32 index, T prevValue, Object? state)
	{
		// remove it using the key captured at Begin (commit must never fail)
		_indexMap.Remove((TIndexType)state!);
	}

	protected override void OnRollbackRemoveValue(Int32 index, T prevValue, Object? state)
	{
		// nothing to do
	}

	protected override Object? OnBeginSetValue(Int32 index, T value, T prevValue)
	{
		// set the new key
		TIndexType key = IndexReader.GetIndexValue(value);
		TIndexType prevKey = IndexReader.GetIndexValue(prevValue);

		Int32 prevIndex = -1;
		if (!KeyComparer.Equals(prevKey, key))
		{
			// validate BEFORE mutating - a duplicate key must not leave the index half-updated
			// (a Begin failure never triggers a rollback of the very handler that failed)
			if (_indexMap.ContainsKey(key))
				throw new ArgumentException($"An item with the same key already exists in index '{Id}'", nameof(value));

			if (_indexMap.TryGetValue(prevKey, out prevIndex))
				_indexMap.Remove(prevKey);
			_indexMap.Add(key, index);
		}

		return new Tuple<TIndexType, TIndexType, Int32>(key, prevKey, prevIndex);
	}

	protected override void OnCommitSetValue(Int32 index, T value, T prevValue, Object? state)
	{
		// nothing to do
	}

	protected override void OnRollbackSetValue(Int32 index, T value, T prevValue, Object? state)
	{
		Tuple<TIndexType, TIndexType, Int32> data = (Tuple<TIndexType, TIndexType, Int32>)state!;

		// restore previous index
		TIndexType key = data.Item1;
		TIndexType prevKey = data.Item2;
		Int32 prevIndex = data.Item3;

		if (!KeyComparer.Equals(prevKey, key))
		{
			_indexMap.Remove(key);
			if (prevIndex != -1)
				_indexMap.Add(prevKey, prevIndex);
		}
	}

	protected override Object? OnBeginClear(Int32 count)
	{
		// nothing to do
		return null;
	}

	protected override void OnCommitClear(Int32 count, Object? state)
	{
		_indexMap.Clear();
	}

	protected override void OnRollbackClear(Int32 count, Object? state)
	{
		// nothing to do
	}

	#endregion // IListChangeHandler implementation
}

public class HashIndex<TIndexType, T> : DictionaryIndex<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	// parameterless constructor is required for Type based index creation via IndexedList.CreateIndex
	public HashIndex()
		: this(null)
	{
	}

	public HashIndex(IEqualityComparer<TIndexType>? keyComparer)
		: base(new Dictionary<TIndexType, Int32>(keyComparer), keyComparer)
	{
	}

	protected override IndexBase<TIndexType, T> CreateInstance()
	{
		return new HashIndex<TIndexType, T>(KeyComparer);
	}

	protected override IDictionary<TIndexType, Int32> CreateIndexMap()
	{
		return new Dictionary<TIndexType, Int32>(KeyComparer);
	}

	protected override void EnsureIndexMapCapacity(IDictionary<TIndexType, Int32> map, Int32 capacity)
	{
		((Dictionary<TIndexType, Int32>)map).EnsureCapacity(capacity);
	}
}

public class TreeIndex<TIndexType, T> : DictionaryIndex<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	private readonly IComparer<TIndexType> _comparer;

	// parameterless constructor is required for Type based index creation via IndexedList.CreateIndex
	public TreeIndex()
		: this(null)
	{
	}

	public TreeIndex(IComparer<TIndexType>? comparer = null)
		: base(new SortedDictionary<TIndexType, Int32>(comparer),
			  new EqualityComparerHelper<TIndexType>(comparer))
	{
		_comparer = comparer ?? Comparer<TIndexType>.Default;
	}

	public new IComparer<TIndexType> KeyComparer
	{
		get => _comparer;
	}

	protected override IndexBase<TIndexType, T> CreateInstance()
	{
		return new TreeIndex<TIndexType, T>(_comparer);
	}

	protected override IDictionary<TIndexType, Int32> CreateIndexMap()
	{
		return new SortedDictionary<TIndexType, Int32>(KeyComparer);
	}

	protected override void EnsureIndexMapCapacity(IDictionary<TIndexType, Int32> map, Int32 capacity)
	{
	}
}
