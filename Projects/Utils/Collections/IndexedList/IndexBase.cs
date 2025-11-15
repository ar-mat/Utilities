using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Armat.Collections;

public abstract class IndexBase<TIndexType, T> : 
	IIndex<TIndexType, T>, IListChangeHandler<T>, 
	IIndexInitializer<TIndexType, T>, IIndexCloner<T>
	where T : notnull
	where TIndexType : notnull
{
	#region Constructors

	public IndexBase(IEqualityComparer<TIndexType>? keyComparer)
	{
		// set the key comparer
		KeyComparer = keyComparer ?? EqualityComparer<TIndexType>.Default;

		// use the uninitialized index to set non-null values to the data members
		IndexBase<TIndexType, T> index = (IndexBase<TIndexType, T>)_uninitialized.GetIndex<TIndexType>(_uninitializedIndexId)!;
		Id = String.Empty;
		Owner = index.Owner;
		Data = index.Data;
		IndexReader = index.IndexReader;
	}

	void IIndexInitializer<TIndexType, T>.Initialize(String id, IndexedList<T> owner, 
		IIndexedListAccessor<T> data, IIndexReader<TIndexType, T> indexReader)
	{
		Id = id;
		Owner = owner;
		Data = data;
		IndexReader = indexReader;

		OnInitialized();
	}

	protected virtual void OnInitialized()
	{
	}

	#region Uninitialized index - to be used for initializing non-nullable data members

	private static readonly IndexedList<T> _uninitialized = CreateUninitializedIndexedList();
	private const String _uninitializedIndexId = "_";

	private static IndexedList<T> CreateUninitializedIndexedList()
	{
		IIndex<TIndexType, T> uninitializedIndex = new EmptyIndex();
		IIndexReader<TIndexType, T> uninitializedReader = new EmptyIndexReader();

		IndexedList<T> result = new();
		result.CreateIndex<TIndexType>(uninitializedIndex,
			_uninitializedIndexId, uninitializedReader);
		return result;
	}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	// this is a special constructor to be used for creating 
	// the singleton instance of EmptyIndex class
	// without having regular IndexBase construction code

	// Note: The only instance will be created in CreateUninitializedIndexedList method
	// And all nun-nullable fields will be initialized inside CreateIndex<TIndexType>(...)
	private IndexBase(Boolean _)
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	private class EmptyIndex : IndexBase<TIndexType, T>
	{
		public EmptyIndex() : base(false) { }

		public override ICollection<TIndexType> Keys => Array.Empty<TIndexType>();
		public override ICollection<T> Values => Array.Empty<T>();

		protected override Int32 IndexOfKey(TIndexType key)
		{
			return -1;
		}

		protected override void RecomputeIndex()
		{
			// do nothing
		}

		protected override IndexBase<TIndexType, T> CreateInstance()
		{
			throw new NotSupportedException();
		}

		protected override void CopyFrom(IndexBase<TIndexType, T> sourceIndex)
		{
			throw new NotSupportedException();
		}

		protected override void CopyToArray(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex)
		{
			// nothing to copy
		}

		public override IEnumerator<KeyValuePair<TIndexType, T>> GetEnumerator()
		{
			return (IEnumerator<KeyValuePair<TIndexType, T>>)Array.Empty<KeyValuePair<TIndexType, T>>().GetEnumerator();
		}
	}

	private class EmptyIndexReader : IIndexReader<TIndexType, T>
	{
		public TIndexType GetIndexValue(T item)
		{
			throw new NotSupportedException();
		}
	}

	#endregion // Uninitialized index - to be used for initializing non-nullable data members

	#endregion // Constructors

	#region IIndex implementation

	public String Id { get; private set; }

	public IndexedList<T> Owner { get; private set; }

	public IIndexReader<TIndexType, T> IndexReader { get; private set; }

	public IEqualityComparer<TIndexType> KeyComparer { get; private set; }

	protected IIndexedListAccessor<T> Data { get; private set; }

	IIndexReaderBase<T> IIndexBase<T>.IndexReader { get { return this.IndexReader; } }

	public Boolean IsEmpty
	{
		get
		{
			return Data.ExternalCount == 0;
		}
	}

	public void VerifyKeyValueConsistency(TIndexType key, T value)
	{
		if (!KeyComparer.Equals(key, IndexReader.GetIndexValue(value)))
			throw new ArgumentException("Inconsistent key specified", nameof(key));
	}

	protected abstract Int32 IndexOfKey(TIndexType key);

	public Int32 IndexOfValue(T value)
	{
		TIndexType key = IndexReader.GetIndexValue(value);

		return IndexOfItem(new KeyValuePair<TIndexType, T>(key, value));
	}

	public virtual Int32 IndexOfItem(KeyValuePair<TIndexType, T> item)
	{
		// find the index by key
		Int32 index = IndexOfKey(item.Key);
		if (index == -1)
			return -1;

		// check whether values are equal
		if (Owner.ValueComparer.Equals(Data[index], item.Value))
			return index;	// item is found

		return -1;
	}

	IIndexBase<T> IIndexCloner<T>.Clone(IndexedList<T> owner, IIndexedListAccessor<T> data)
	{
		// create an empty clone
		IndexBase<TIndexType, T> clone = CreateInstance();

		// initialize that
		((IIndexInitializer<TIndexType, T>)clone).Initialize(Id, owner, data, IndexReader);

		// copy data into the new instance
		using var rLock = Owner.CreateReadLock();

		clone.CopyFrom(this);

		return clone;
	}

	// by default it will create another instance of the same type using parameterless constructor
	// override if this method doesn't work for the derived Index class
	protected abstract IndexBase<TIndexType, T> CreateInstance();

	protected abstract void CopyFrom(IndexBase<TIndexType, T> sourceIndex);

	protected abstract void CopyToArray(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex);

	public void ReIndex()
	{
		// copy data into the new instance
		using var rLock = Owner.CreateReadLock();

		RecomputeIndex();
	}

	protected abstract void RecomputeIndex();

	public Int32 IndexOf(TIndexType key)
	{
		using var rLock = Owner.CreateReadLock();

		Int32 internalIndex = IndexOfKey(key);
		if (internalIndex == -1)
			return -1;

		// this is an interface method, must return external index
		return Data.ToExternalIndex(internalIndex);
	}

	public Boolean ContainsValue(T value)
	{
		return IndexOfValue(value) != -1;
	}

	#endregion // IIndex implementation

	#region IDictionary Implementation

	#region Helper collections for dictionary-based indexes

	protected class KVCollection : ICollection<KeyValuePair<TIndexType, T>>
	{
		protected readonly IIndexedListAccessor<T> _data;
		protected readonly IDictionary<TIndexType, Int32> _indexMap;
		protected readonly IEqualityComparer<T> _valueComparer;

		public KVCollection(IIndexedListAccessor<T> data, IDictionary<TIndexType, Int32> indexMap, IEqualityComparer<T> valueComparer)
		{
			_data = data;
			_indexMap = indexMap;
			_valueComparer = valueComparer;
		}

		public Int32 Count
		{
			get
			{
				// in case of empty _indexMap (not initialized yet) the count must be 0
				// otherwise the counts must be equal
				// it returns _data.ExternalCount to be consistent with MultiIndexBase.MultiKVCollection.Count implementation
				return _indexMap.Count == 0 ? 0 : _data.ExternalCount;
			}
		}

		public Boolean IsReadOnly => true;

		public void Add(KeyValuePair<TIndexType, T> item)
		{
			throw new NotSupportedException();
		}

		public Boolean Remove(KeyValuePair<TIndexType, T> item)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public Boolean Contains(KeyValuePair<TIndexType, T> item)
		{
			return IndexOf(item) != -1;
		}

		public Int32 IndexOf(KeyValuePair<TIndexType, T> item)
		{
#pragma warning disable IDE0018 // Inline variable declaration
			Int32 valueIndex;
#pragma warning restore IDE0018 // Inline variable declaration

			// find indexes of items for lookup (by keys)
			if (!_indexMap.TryGetValue(item.Key, out valueIndex))
				return -1;

			// check if the corresponding value matches
			if (!_valueComparer.Equals(item.Value, _data[valueIndex]))
				return -1;

			return valueIndex;

		}

		public void CopyTo(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex)
		{
			if (array == null || array.Length <= arrayIndex)
				return;

			foreach (KeyValuePair<TIndexType, Int32> pair in _indexMap)
			{
				array[arrayIndex++] = new KeyValuePair<TIndexType, T>(pair.Key, _data[pair.Value]);

				if (arrayIndex >= array.Length)
					return;
			}
		}

		IEnumerator<KeyValuePair<TIndexType, T>> IEnumerable<KeyValuePair<TIndexType, T>>.GetEnumerator()
		{
			return new KVCollectionEnumerator(_data, _indexMap);
		}

		public virtual IEnumerator GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<TIndexType, T>>)this).GetEnumerator();
		}

		protected class KVCollectionEnumerator : IEnumerator, IEnumerator<KeyValuePair<TIndexType, T>>
		{
			protected readonly IIndexedListAccessor<T> _data;
			protected readonly IDictionary<TIndexType, Int32> _indexMap;

			protected IEnumerator<KeyValuePair<TIndexType, Int32>> _keyIt;

			public KVCollectionEnumerator(IIndexedListAccessor<T> data, IDictionary<TIndexType, Int32> index)
			{
				_data = data;
				_indexMap = index;

				_keyIt = _indexMap.GetEnumerator();
			}

			KeyValuePair<TIndexType, T> IEnumerator<KeyValuePair<TIndexType, T>>.Current
			{
				get => new(_keyIt.Current.Key, _data[_keyIt.Current.Value]);
			}

			public virtual Object Current => ((IEnumerator<KeyValuePair<TIndexType, T>>)this).Current;

			public Boolean MoveNext()
			{
				return _keyIt.MoveNext();
			}

			public void Reset()
			{
				_keyIt.Reset();
			}

			public void Dispose()
			{
				_keyIt.Dispose();
			}
		}
	}

	protected class KeyCollection : KVCollection, ICollection<TIndexType>
	{
		public KeyCollection(IIndexedListAccessor<T> data, IDictionary<TIndexType, Int32> index, IEqualityComparer<T> valueComparer)
			: base(data, index, valueComparer)
		{
		}

		public void Add(TIndexType item)
		{
			throw new NotImplementedException();
		}

		public Boolean Remove(TIndexType item)
		{
			throw new NotImplementedException();
		}

		public Boolean Contains(TIndexType item)
		{
			return _indexMap.ContainsKey(item);
		}

		public Int32 IndexOf(TIndexType item)
		{
			// find indexes of items for lookup (by keys)
			if (_indexMap.TryGetValue(item, out Int32 index))
				return index;

			return -1;
		}

		public void CopyTo(TIndexType[] array, Int32 arrayIndex)
		{
			_indexMap.Keys.CopyTo(array, arrayIndex);
		}

		IEnumerator<TIndexType> IEnumerable<TIndexType>.GetEnumerator()
		{
			return new KeyCollectionEnumerator(_data, _indexMap);
		}

		public override IEnumerator GetEnumerator()
		{
			return ((IEnumerable<TIndexType>)this).GetEnumerator();
		}

		private class KeyCollectionEnumerator : KVCollectionEnumerator, IEnumerator<TIndexType>
		{
			public KeyCollectionEnumerator(IIndexedListAccessor<T> data, IDictionary<TIndexType, Int32> index)
				: base(data, index)
			{
			}

			TIndexType IEnumerator<TIndexType>.Current
			{
				get => _keyIt.Current.Key;
			}

			public override Object Current => ((IEnumerator<TIndexType>)this).Current;
		}
	}

	protected class ValueCollection : KVCollection, ICollection<T>
	{
		public ValueCollection(IIndexedListAccessor<T> data, IDictionary<TIndexType, Int32> index, IEqualityComparer<T> valueComparer)
			: base(data, index, valueComparer)
		{
		}

		public void Add(T item)
		{
			throw new NotImplementedException();
		}

		public Boolean Remove(T item)
		{
			throw new NotImplementedException();
		}

		public Boolean Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		public Int32 IndexOf(T item)
		{
			foreach (KeyValuePair<TIndexType, Int32> pair in _indexMap)
			{
				// check if the corresponding value matches
				if (_valueComparer.Equals(item, _data[pair.Value]))
					return pair.Value;
			}

			return -1;
		}

		public void CopyTo(T[] array, Int32 arrayIndex)
		{
			if (array == null || array.Length <= arrayIndex)
				return;

			foreach (KeyValuePair<TIndexType, Int32> pair in _indexMap)
			{
				array[arrayIndex++] = _data[pair.Value];

				if (arrayIndex >= array.Length)
					return;
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new ValueCollectionEnumerator(_data, _indexMap);
		}

		public override IEnumerator GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}

		private class ValueCollectionEnumerator : KVCollectionEnumerator, IEnumerator<T>
		{
			public ValueCollectionEnumerator(IIndexedListAccessor<T> data, IDictionary<TIndexType, Int32> index)
				: base(data, index)
			{
			}

			T IEnumerator<T>.Current
			{
				get => _data[_keyIt.Current.Value];
			}

			public override Object Current => ((IEnumerator<T>)this).Current;
		}
	}

	#endregion // Helper collections for dictionary-based indexes

	public abstract ICollection<TIndexType> Keys
	{
		get;
	}

	public abstract ICollection<T> Values
	{
		get;
	}

	public abstract IEnumerator<KeyValuePair<TIndexType, T>> GetEnumerator();

	IEnumerable<TIndexType> IReadOnlyDictionary<TIndexType, T>.Keys
	{
		get { return this.Keys; }
	}

	IEnumerable<T> IReadOnlyDictionary<TIndexType, T>.Values
	{
		get { return this.Values; }
	}

	public Int32 Count
	{
		get
		{
			return Keys.Count;
		}
	}

	public Boolean IsReadOnly
	{
		get
		{
			return Owner.IsReadOnly;
		}
	}

	public T this[TIndexType key]
	{
		get
		{
			using var rLock = Owner.CreateReadLock();

			Int32 index = IndexOfKey(key);
			if (index == -1)
				throw new KeyNotFoundException();

			return Data[index];
		}
		set
		{
			VerifyKeyValueConsistency(key, value);

			using var urLock = Owner.CreateUpgradableReadLock();

			Int32 index = IndexOfKey(key);
			if (index != -1)
				Data[index] = value;
			else
				Data.Add(value);
		}
	}

	public void Add(TIndexType key, T value)
	{
		VerifyKeyValueConsistency(key, value);

		Data.Add(value);
	}

	public Boolean ContainsKey(TIndexType key)
	{
		using var rLock = Owner.CreateReadLock();

		return IndexOfKey(key) != -1;
	}

	public Boolean Remove(TIndexType key)
	{
		using var urLock = Owner.CreateUpgradableReadLock();

		Int32 index = IndexOfKey(key);
		if (index == -1)
			return false;

		Data.RemoveAt(index);
		return true;
	}

	public Boolean TryGetValue(TIndexType key, [MaybeNullWhen(false)] out T value)
	{
		using var rLock = Owner.CreateReadLock();

		Int32 index = IndexOfKey(key);
		if (index != -1)
		{
			value = Data[index];
			return true;
		}

		value = default;
		return false;
	}

	public void Add(KeyValuePair<TIndexType, T> item)
	{
		Add(item.Key, item.Value);
	}

	public void Clear()
	{
		Data.Clear();
	}

	public Boolean Contains(KeyValuePair<TIndexType, T> item)
	{
		using var rLock = Owner.CreateReadLock();

		return IndexOfItem(item) != -1;
	}

	public void CopyTo(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex)
	{
		if (array == null || array.Length <= arrayIndex)
			return;

		using var rLock = Owner.CreateReadLock();

		CopyToArray(array, arrayIndex);
	}

	public Boolean Remove(KeyValuePair<TIndexType, T> item)
	{
		using var urLock = Owner.CreateUpgradableReadLock();

		Int32 index = IndexOfItem(item);
		if (index == -1)
			return false;

		Owner.RemoveAt(index);
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	#endregion // IDictionary Implementation

	#region IListChangeHandler implementation

	public virtual Object? OnBeginInsertValue(Int32 index, T value)
	{
		return null;
	}

	public virtual void OnCommitInsertValue(Int32 index, T value, Object? state)
	{
	}

	public virtual void OnRollbackInsertValue(Int32 index, T value, Object? state)
	{
	}

	public virtual Object? OnBeginRemoveValue(Int32 index, T prevValue)
	{
		return null;
	}

	public virtual void OnCommitRemoveValue(Int32 index, T prevValue, Object? state)
	{
	}

	public virtual void OnRollbackRemoveValue(Int32 index, T prevValue, Object? state)
	{
	}

	public virtual Object? OnBeginSetValue(Int32 index, T value, T prevValue)
	{
		return null;
	}

	public virtual void OnCommitSetValue(Int32 index, T value, T prevValue, Object? state)
	{
	}

	public virtual void OnRollbackSetValue(Int32 index, T value, T prevValue, Object? state)
	{
	}

	/* *Move* methods are not marked as virtual because moving items within IndexedList 
	 * does reshuffling of external indexes but not internal ones.
	 * Indexes always work based on internal indexes, and therefore Moves are irrelevant in this context */
	public /*virtual*/ Object? OnBeginMoveValue(Int32 indexNew, Int32 IndexPrev, T value)
	{
		return null;
	}

	public /*virtual*/ void OnCommitMoveValue(Int32 indexNew, Int32 IndexPrev, T value, Object? state)
	{
	}

	public /*virtual*/ void OnRollbackMoveValue(Int32 indexNew, Int32 IndexPrev, T value, Object? state)
	{
	}
	/* *Move* methods are not marked as virtual because moving items within IndexedList 
	 * does reshuffling of external indexes but not internal ones.
	 * Indexes always work based on internal indexes, and therefore Moves are irrelevant in this context */

	public virtual Object? OnBeginClear(Int32 count)
	{
		return null;
	}

	public virtual void OnCommitClear(Int32 count, Object? state)
	{
	}

	public virtual void OnRollbackClear(Int32 count, Object? state)
	{
	}

	#endregion // IListChangeHandler implementation
}

#region Comparer -> EqualityComparer

// Helper class to convert IComparer<T> into IEqualityComparer<T>
// Note: GetHashCode method is not supported
public class EqualityComparerHelper<T> : IEqualityComparer<T>
{
	public EqualityComparerHelper(IComparer<T>? comparer)
	{
		Comparer = comparer ?? Comparer<T>.Default;
	}

	public IComparer<T> Comparer { get; }

	public Boolean Equals(T? x, T? y)
	{
		return Comparer.Compare(x, y) == 0;
	}

	public Int32 GetHashCode([DisallowNull] T obj)
	{
		throw new NotSupportedException();
	}
}

#endregion // Comparer -> EqualityComparer
