using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace Armat.Collections
{
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

		#endregion // Properties

		#region IIndex implementation

		protected abstract IDictionary<TIndexType, Int32> CreateIndexMap();
		protected abstract void EnsureIndexMapCapacity(Int32 capacity);

		protected override void CopyFrom(IndexBase<TIndexType, T> sourceIndex)
		{
			if (_indexMap.Count > 0)
				throw new NotSupportedException("Cannot copy into non-empty index");

			DictionaryIndex<TIndexType, T> source = (DictionaryIndex<TIndexType, T>)sourceIndex;

			// copy current class data
			EnsureIndexMapCapacity(source._indexMap.Count);
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
			EnsureIndexMapCapacity(_indexMap.Count);

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
		}

		public override ICollection<TIndexType> Keys
		{
			get => new KeyCollection(Data, _indexMap, Owner.ValueComparer);
		}

		public override ICollection<T> Values
		{
			get => new ValueCollection(Data, _indexMap, Owner.ValueComparer);
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

		public override Object? OnBeginInsertValue(Int32 index, T value)
		{
			// add the value
			TIndexType key = IndexReader.GetIndexValue(value);
			_indexMap.Add(key, index);

			return key;
		}

		public override void OnCommitInsertValue(Int32 index, T value, Object? state)
		{
			// nothing to do
		}

		public override void OnRollbackInsertValue(Int32 index, T value, Object? state)
		{
			// remove the added key
			_indexMap.Remove((TIndexType)state!);
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
			_indexMap.Remove(key);
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

			Int32 prevIndex = -1;
			if (!KeyComparer.Equals(prevKey, key))
			{
				if (_indexMap.TryGetValue(prevKey, out prevIndex))
					_indexMap.Remove(prevKey);
				_indexMap.Add(key, index);
			}

			return new Tuple<TIndexType, TIndexType, Int32>(key, prevKey, prevIndex);
		}

		public override void OnCommitSetValue(Int32 index, T value, T prevValue, Object? state)
		{
			// nothing to do
		}

		public override void OnRollbackSetValue(Int32 index, T value, T prevValue, Object? state)
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

	public class HashIndex<TIndexType, T> : DictionaryIndex<TIndexType, T>
		where T : notnull
		where TIndexType : notnull
	{
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

		protected override void EnsureIndexMapCapacity(Int32 capacity)
		{
			((Dictionary<TIndexType, Int32>)_indexMap).EnsureCapacity(capacity);
		}
	}

	public class TreeIndex<TIndexType, T> : DictionaryIndex<TIndexType, T>
		where T : notnull
		where TIndexType : notnull
	{
		private readonly IComparer<TIndexType> _comparer;

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

		protected override void EnsureIndexMapCapacity(Int32 capacity)
		{
		}
	}
}
