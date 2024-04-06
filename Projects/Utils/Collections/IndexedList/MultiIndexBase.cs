using System;
using System.Collections;
using System.Collections.Generic;

namespace Armat.Collections
{
	public abstract class MultiIndexBase<TIndexType, T> : IndexBase<TIndexType, T>, IMultiIndex<TIndexType, T>
		where T : notnull
		where TIndexType : notnull
	{
		#region Constructors

		public MultiIndexBase(IEqualityComparer<TIndexType>? keyComparer)
			: base(keyComparer)
		{
		}

		#endregion // Constructors

		#region IIndex implementation

		protected override Int32 IndexOfKey(TIndexType key)
		{
			IReadOnlyCollection<Int32> listIndexes = IndexesOfKey(key);
			if (listIndexes == null)
				return -1;

			IEnumerator<Int32> listEnum = listIndexes.GetEnumerator();
			if (listEnum == null || !listEnum.MoveNext())
				return -1;

			return listEnum.Current;
		}

		protected abstract IReadOnlyCollection<Int32> IndexesOfKey(TIndexType key);

		public override Int32 IndexOfItem(KeyValuePair<TIndexType, T> item)
		{
			// find the index by key
			IReadOnlyCollection<Int32> listIndexes = IndexesOfKey(item.Key);
			if (listIndexes == null)
				return -1;

			// find values equal to the given one
			foreach (Int32 index in listIndexes)
			{
				if (Owner.ValueComparer.Equals(Data[index], item.Value))
					return index;	// item is found
			}

			return -1;
		}

		public IReadOnlyCollection<Int32> IndexesOfValue(T value)
		{
			TIndexType key = IndexReader.GetIndexValue(value);

			return IndexesOfItem(new KeyValuePair<TIndexType, T>(key, value));
		}

		protected virtual IReadOnlyCollection<Int32> IndexesOfItem(KeyValuePair<TIndexType, T> item)
		{
			// find the index by key
			IReadOnlyCollection<Int32> listIndexes = IndexesOfKey(item.Key);
			if (listIndexes == null)
				return Array.Empty<Int32>();

			IndigentList<Int32> result = new();

			// find values equal to the given one
			foreach (Int32 index in listIndexes)
			{
				if (Owner.ValueComparer.Equals(Data[index], item.Value))
					result.Add(index);	// item is found
			}

			return result;
		}

		public virtual Int32 GetCountByKey(TIndexType key)
		{
			IReadOnlyCollection<Int32> arrIndexes = IndexesOfKey(key);
			return arrIndexes.Count;
		}

		public virtual IReadOnlyCollection<Int32> IndexesOf(TIndexType key)
		{
			IReadOnlyCollection<Int32> arrIndexes = IndexesOfKey(key);
			if (arrIndexes == null)
				return Array.Empty<Int32>();

			IndigentList<Int32> listIndexes = new(arrIndexes.Count);
			foreach (Int32 index in arrIndexes)
			{
				// this is an interface method, must return external index
				Int32 externalIndex = Data.ToExternalIndex(index);
				if (externalIndex != -1)
					listIndexes.Add(externalIndex);
			}

			return listIndexes;
		}

		public virtual IReadOnlyCollection<T> GetValuesByKey(TIndexType key)
		{
			IReadOnlyCollection<Int32> arrIndexes = IndexesOfKey(key);
			if (arrIndexes == null)
				return Array.Empty<T>();

			List<T> listValues = new(arrIndexes.Count);
			foreach (Int32 index in arrIndexes)
				listValues.Add(Data[index]);

			return listValues;
		}

		#endregion // IIndex implementation

		#region IDictionary Implementation

		#region Helper collections for dictionary-based indexes

		protected class MultiKVCollection : ICollection<KeyValuePair<TIndexType, T>>
		{
			protected readonly IIndexedListAccessor<T> _data;
			protected readonly IDictionary<TIndexType, IList<Int32>> _indexMap;
			protected readonly IEqualityComparer<T> _valueComparer;

			public MultiKVCollection(IIndexedListAccessor<T> data, IDictionary<TIndexType, IList<Int32>> index, IEqualityComparer<T> valueComparer)
			{
				_data = data;
				_indexMap = index;
				_valueComparer = valueComparer;
			}

			public Int32 Count => _data.ExternalCount;

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
				IList<Int32>? listIndexes;

				// find indexes of items for lookup (by keys)
				if (!_indexMap.TryGetValue(item.Key, out listIndexes))
					return -1;

				for (Int32 i = 0; i < listIndexes.Count; i++)
				{
					// check if the corresponding value matches
					Int32 valueIndex = listIndexes[i];
					if (_valueComparer.Equals(item.Value, _data[valueIndex]))
						return valueIndex;
				}

				return -1;
			}

			public void CopyTo(KeyValuePair<TIndexType, T>[] array, Int32 arrayIndex)
			{
				if (array == null || array.Length <= arrayIndex)
					return;

				foreach (KeyValuePair<TIndexType, IList<Int32>> pair in _indexMap)
				{
					for (Int32 index = 0; index < pair.Value.Count; index++)
					{
						array[arrayIndex++] = new KeyValuePair<TIndexType, T>(pair.Key, _data[pair.Value[index]]);

						if (arrayIndex >= array.Length)
							return;
					}
				}
			}

			IEnumerator<KeyValuePair<TIndexType, T>> IEnumerable<KeyValuePair<TIndexType, T>>.GetEnumerator()
			{
				return new MultiKVCollectionEnumerator(_data, _indexMap);
			}

			public virtual IEnumerator GetEnumerator()
			{
				return ((IEnumerable<KeyValuePair<TIndexType, T>>)this).GetEnumerator();
			}

			protected class MultiKVCollectionEnumerator : IEnumerator, IEnumerator<KeyValuePair<TIndexType, T>>
			{
				protected readonly IIndexedListAccessor<T> _data;
				protected readonly IDictionary<TIndexType, IList<Int32>> _indexMap;

				protected IEnumerator<KeyValuePair<TIndexType, IList<Int32>>> _keyIt;
				protected Int32 _listIndex;

				public MultiKVCollectionEnumerator(IIndexedListAccessor<T> data, IDictionary<TIndexType, IList<Int32>> index)
				{
					_data = data;
					_indexMap = index;

					_keyIt = _indexMap.GetEnumerator();
					_listIndex = -1;
				}

				KeyValuePair<TIndexType, T> IEnumerator<KeyValuePair<TIndexType, T>>.Current
				{
					get => new(_keyIt.Current.Key, _data[_keyIt.Current.Value[_listIndex]]);
				}

				public virtual Object Current => ((IEnumerator<KeyValuePair<TIndexType, T>>)this).Current;

				public Boolean MoveNext()
				{
					if (_listIndex == -2)
						throw new ObjectDisposedException(nameof(MultiKVCollectionEnumerator));

					// check whether the current list still has items
					if (_listIndex != -1 && _listIndex < _keyIt.Current.Value.Count - 1)
					{
						_listIndex++;
						return true;
					}

					// move to the first item of next non-empty list
					Boolean result;

					while (result = _keyIt.MoveNext())
					{
						if (_keyIt.Current.Value.Count > 0)
							break;
					}

					if (result)
						_listIndex = 0;
					else
						_listIndex = -1;

					return result;
				}

				public void Reset()
				{
					_keyIt.Reset();
					_listIndex = -1;
				}

				public void Dispose()
				{
					_keyIt = (IEnumerator<KeyValuePair<TIndexType, IList<Int32>>>)Array.Empty<KeyValuePair<TIndexType, IList<Int32>>>().GetEnumerator();
					_listIndex = -2;
				}
			}
		}

		protected class MultiKeyCollection : MultiKVCollection, ICollection<TIndexType>
		{
			public MultiKeyCollection(IIndexedListAccessor<T> data, IDictionary<TIndexType, IList<Int32>> index, IEqualityComparer<T> valueComparer)
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

			public IList<Int32>? IndexesOf(TIndexType item)
			{
				// find indexes of items for lookup (by keys)
				if (_indexMap.TryGetValue(item, out IList<Int32>? listIndexes))
					return listIndexes;

				return null;
			}

			public void CopyTo(TIndexType[] array, Int32 arrayIndex)
			{
				if (array == null || array.Length <= arrayIndex)
					return;

				foreach (KeyValuePair<TIndexType, IList<Int32>> pair in _indexMap)
				{
					for (Int32 index = 0; index < pair.Value.Count; index++)
					{
						array[arrayIndex++] = pair.Key;

						if (arrayIndex >= array.Length)
							return;
					}
				}
			}

			IEnumerator<TIndexType> IEnumerable<TIndexType>.GetEnumerator()
			{
				return new MultiKeyCollectionEnumerator(_data, _indexMap);
			}

			public override IEnumerator GetEnumerator()
			{
				return ((IEnumerable<TIndexType>)this).GetEnumerator();
			}

			private class MultiKeyCollectionEnumerator : MultiKVCollectionEnumerator, IEnumerator<TIndexType>
			{
				public MultiKeyCollectionEnumerator(IIndexedListAccessor<T> data, IDictionary<TIndexType, IList<Int32>> index)
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

		protected class MultiValueCollection : MultiKVCollection, ICollection<T>
		{
			public MultiValueCollection(IIndexedListAccessor<T> data, IDictionary<TIndexType, IList<Int32>> index, IEqualityComparer<T> valueComparer)
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
				foreach (KeyValuePair<TIndexType, IList<Int32>> pair in _indexMap)
				{
					IList<Int32> listIndexes = pair.Value;
					for (Int32 i = 0; i < listIndexes.Count; i++)
					{
						// check if the corresponding value matches
						Int32 valueIndex = listIndexes[i];
						if (_valueComparer.Equals(item, _data[valueIndex]))
							return valueIndex;
					}
				}

				return -1;
			}

			public void CopyTo(T[] array, Int32 arrayIndex)
			{
				if (array == null || array.Length <= arrayIndex)
					return;

				foreach (KeyValuePair<TIndexType, IList<Int32>> pair in _indexMap)
				{
					for (Int32 index = 0; index < pair.Value.Count; index++)
					{
						array[arrayIndex++] = _data[pair.Value[index]];

						if (arrayIndex >= array.Length)
							return;
					}
				}
			}

			IEnumerator<T> IEnumerable<T>.GetEnumerator()
			{
				return new MultiValueCollectionEnumerator(_data, _indexMap);
			}

			public override IEnumerator GetEnumerator()
			{
				return ((IEnumerable<T>)this).GetEnumerator();
			}

			private class MultiValueCollectionEnumerator : MultiKVCollectionEnumerator, IEnumerator<T>
			{
				public MultiValueCollectionEnumerator(IIndexedListAccessor<T> data, IDictionary<TIndexType, IList<Int32>> index)
					: base(data, index)
				{
				}

				T IEnumerator<T>.Current
				{
					get => _data[_keyIt.Current.Value[_listIndex]];
				}

				public override Object Current => ((IEnumerator<T>)this).Current;
			}
		}

		#endregion // Helper collections for dictionary-based indexes

		#endregion // IDictionary Implementation
	}
}
