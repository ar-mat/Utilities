using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Armat.Collections;

public abstract class MultiDictionaryIndex<TIndexType, T> : MultiIndexBase<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	#region Constructors

	public MultiDictionaryIndex(IDictionary<TIndexType, IList<Int32>> indexMap, IEqualityComparer<TIndexType>? keyComparer)
		: base(keyComparer)
	{
		_indexMap = indexMap;
	}

	#endregion // Constructors

	#region Properties

	protected IDictionary<TIndexType, IList<Int32>> _indexMap;

	#endregion // Properties

	#region IIndex implementation

	protected abstract IDictionary<TIndexType, IList<Int32>> CreateIndexMap();
	protected abstract void EnsureIndexMapCapacity(Int32 capacity);

	private static IList<Int32> CreateValueList(ICollection<Int32>? other = null)
	{
		IList<Int32> result;

		// the returned type must support IReadOnlyCollection
		if (other == null)
			result = new IndigentList<Int32>();
		else
			result = new IndigentList<Int32>(other);

		return result;
	}

	protected override void CopyFrom(IndexBase<TIndexType, T> sourceIndex)
	{
		if (_indexMap.Count > 0)
			throw new NotSupportedException("Cannot copy into non-empty index");

		MultiDictionaryIndex<TIndexType, T> source = (MultiDictionaryIndex<TIndexType, T>)sourceIndex;

		// copy current class data
		EnsureIndexMapCapacity(source._indexMap.Count);
		foreach (KeyValuePair<TIndexType, IList<Int32>> pair in source._indexMap)
			_indexMap.Add(pair.Key, CreateValueList(pair.Value));
	}

	protected override void CopyToArray(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex)
	{
		MultiKVCollection kVCollection = new(Data, _indexMap, Owner.ValueComparer);
		kVCollection.CopyTo(array, arrayIndex);
	}

	protected override void RecomputeIndex()
	{
		// create new index
		IDictionary<TIndexType, IList<Int32>> newIndex = CreateIndexMap();
		EnsureIndexMapCapacity(_indexMap.Count);

		// compute the index
		foreach (KeyValuePair<TIndexType, IList<Int32>> pair in _indexMap)
		{
			IList<Int32> listIndexes = pair.Value;

			for (Int32 i = 0; i < listIndexes.Count; i++)
			{
				Int32 index = listIndexes[i];
				T value = Data[index];
				TIndexType key = IndexReader.GetIndexValue(value);

				IList<Int32>? listNewIndexes;
				if (!newIndex.TryGetValue(key, out listNewIndexes))
					newIndex.Add(key, listNewIndexes = CreateValueList());
				listNewIndexes.Add(index);
			}
		}
	}

	public override ICollection<TIndexType> Keys
	{
		get => new MultiKeyCollection(Data, _indexMap, Owner.ValueComparer);
	}

	public override ICollection<T> Values
	{
		get => new MultiValueCollection(Data, _indexMap, Owner.ValueComparer);
	}

	public override IEnumerator<KeyValuePair<TIndexType, T>> GetEnumerator()
	{
		MultiKVCollection kVCollection = new(Data, _indexMap, Owner.ValueComparer);
		return (IEnumerator<KeyValuePair<TIndexType, T>>)kVCollection.GetEnumerator();
	}

	protected override Int32 IndexOfKey(TIndexType key)
	{
		if (_indexMap.TryGetValue(key, out IList<Int32>? listIndexes))
			return listIndexes[0];

		return -1;
	}

	protected override IReadOnlyCollection<Int32> IndexesOfKey(TIndexType key)
	{
		if (_indexMap.TryGetValue(key, out IList<Int32>? listIndexes))
			return (IReadOnlyCollection<Int32>)listIndexes;

		return Array.Empty<Int32>();
	}

	#endregion // IIndex implementation

	#region IListChangeHandler implementation

	public override Object? OnBeginInsertValue(Int32 index, T value)
	{
		// add the value
		TIndexType key = IndexReader.GetIndexValue(value);

#pragma warning disable IDE0018 // Inline variable declaration
		IList<Int32>? listIndexes;
#pragma warning restore IDE0018 // Inline variable declaration
		if (!_indexMap.TryGetValue(key, out listIndexes))
			_indexMap.Add(key, listIndexes = CreateValueList());

		listIndexes.Add(index);

		return key;
	}

	public override void OnCommitInsertValue(Int32 index, T value, Object? state)
	{
		// nothing to do
	}

	public override void OnRollbackInsertValue(Int32 index, T value, Object? state)
	{
		// remove the added key
		TIndexType key = (TIndexType)state!;

		if (_indexMap.TryGetValue(key, out IList<Int32>? listIndexes))
		{
			if (listIndexes.Count == 1)
				_indexMap.Remove(key);
			else
				listIndexes.RemoveAt(listIndexes.Count - 1);
		}
	}

	public override Object? OnBeginRemoveValue(Int32 index, T prevValue)
	{
		// nothing to do
		return null;
	}

	public override void OnCommitRemoveValue(Int32 index, T prevValue, Object? state)
	{
		// remove it
		TIndexType key = IndexReader.GetIndexValue(prevValue);

		if (_indexMap.TryGetValue(key, out IList<Int32>? listIndexes))
		{
			if (listIndexes.Count == 1)
			{
				if (listIndexes[0] == index)
					_indexMap.Remove(key);
			}
			else
			{
				listIndexes.Remove(index);
			}
		}
	}

	public override void OnRollbackRemoveValue(Int32 index, T prevValue, Object? state)
	{
		// nothing to do
	}

	public override Object? OnBeginSetValue(Int32 index, T value, T prevValue)
	{
		// set the new key
		TIndexType key = IndexReader.GetIndexValue(value);
		TIndexType prevKey = IndexReader.GetIndexValue(prevValue);

		if (!KeyComparer.Equals(prevKey, key))
		{
			// remove the old value
			if (_indexMap.TryGetValue(prevKey, out IList<Int32>? listIndexesPrev))
			{
				if (listIndexesPrev.Count == 1)
				{
					if (listIndexesPrev[0] == index)
						_indexMap.Remove(key);
				}
				else
				{
					listIndexesPrev.Remove(index);
				}
			}

			// add the new value
#pragma warning disable IDE0018 // Inline variable declaration
			IList<Int32>? listIndexesNew;
#pragma warning restore IDE0018 // Inline variable declaration
			if (!_indexMap.TryGetValue(key, out listIndexesNew))
				_indexMap.Add(key, listIndexesNew = CreateValueList());
			listIndexesNew.Add(index);
		}

		return new Tuple<TIndexType, TIndexType>(key, prevKey);
	}

	public override void OnCommitSetValue(Int32 index, T value, T prevValue, Object? state)
	{
		// nothing to do
	}

	public override void OnRollbackSetValue(Int32 index, T value, T prevValue, Object? state)
	{
		Tuple<TIndexType, TIndexType> data = (Tuple<TIndexType, TIndexType>)state!;

		// restore previous index
		TIndexType key = data.Item1;
		TIndexType prevKey = data.Item2;

		// remove the old value
		if (!KeyComparer.Equals(prevKey, key))
		{
			if (_indexMap.TryGetValue(key, out IList<Int32>? listIndexes))
			{
#pragma warning disable IDE0056 // Use index operator
				if (listIndexes[listIndexes.Count - 1] == index)
#pragma warning restore IDE0056 // Use index operator
				{
					if (listIndexes.Count == 0)
						_indexMap.Remove(key);
					else
						listIndexes.RemoveAt(listIndexes.Count - 1);
				}
			}

			// add the new value
#pragma warning disable IDE0018 // Inline variable declaration
			IList<Int32>? listIndexesPrev;
#pragma warning restore IDE0018 // Inline variable declaration
			if (!_indexMap.TryGetValue(prevKey, out listIndexesPrev))
				_indexMap.Add(prevKey, listIndexesPrev = CreateValueList());
			listIndexesPrev.Add(index);
		}
	}

	public override Object? OnBeginClear(Int32 count)
	{
		// nothing to do
		return null;
	}

	public override void OnCommitClear(Int32 count, Object? state)
	{
		_indexMap.Clear();
	}

	public override void OnRollbackClear(Int32 count, Object? state)
	{
		// nothing to do
	}

	#endregion // IListChangeHandler implementation
}

public class MultiHashIndex<TIndexType, T> : MultiDictionaryIndex<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	public MultiHashIndex(IEqualityComparer<TIndexType>? keyComparer)
		: base(new Dictionary<TIndexType, IList<Int32>>(keyComparer), keyComparer)
	{
	}

	protected override IndexBase<TIndexType, T> CreateInstance()
	{
		return new MultiHashIndex<TIndexType, T>(KeyComparer);
	}

	protected override IDictionary<TIndexType, IList<Int32>> CreateIndexMap()
	{
		return new Dictionary<TIndexType, IList<Int32>>(KeyComparer);
	}

	protected override void EnsureIndexMapCapacity(Int32 capacity)
	{
		((Dictionary<TIndexType, IList<Int32>>)_indexMap).EnsureCapacity(capacity);
	}
}

public class MultiTreeIndex<TIndexType, T> : MultiDictionaryIndex<TIndexType, T>
	where T : notnull
	where TIndexType : notnull
{
	private readonly IComparer<TIndexType> _comparer;

	public MultiTreeIndex(IComparer<TIndexType>? comparer = null)
		: base(new SortedDictionary<TIndexType, IList<Int32>>(Comparer<TIndexType>.Default),
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
		return new MultiTreeIndex<TIndexType, T>(KeyComparer);
	}

	protected override IDictionary<TIndexType, IList<Int32>> CreateIndexMap()
	{
		return new SortedDictionary<TIndexType, IList<Int32>>(_comparer);
	}

	protected override void EnsureIndexMapCapacity(Int32 capacity)
	{
	}
}
