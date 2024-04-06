using Armat.Utils.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Armat.Collections
{
	public class ListDictionary<TKey, TValue> : IList<TValue>, IReadOnlyList<TValue>, 
		IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, 
		IListChangeEmitter<TValue>, INotifyCollectionChanged
		where TKey : notnull
		where TValue : notnull
	{
		#region Data Members

		private readonly IndexedList<TValue> _list;
		private readonly DictionaryIndex<TKey, TValue> _index;
		private StandardListChangeEmitter<TValue>? _changeEmitter = null;

		#endregion // Data Members

		#region Constructors

		public ListDictionary(IndexReaderDelegate<TKey, TValue> indexReader, IEqualityComparer<TKey> keyComparer, Boolean synchronized = false)
		{
			_list = new IndexedList<TValue>(synchronized);
			_index = _list.CreateHashIndex<TKey>("Index", indexReader, keyComparer);

			_list.RegisterChangeHandler(new ListChangeHandler(this));
		}

		public ListDictionary(IIndexReader<TKey, TValue> indexReader, IEqualityComparer<TKey> keyComparer, Boolean synchronized = false)
		{
			_list = new IndexedList<TValue>(synchronized);
			_index = _list.CreateHashIndex<TKey>("Index", indexReader, keyComparer);

			_list.RegisterChangeHandler(new ListChangeHandler(this));
		}

		public ListDictionary(Int32 capacity, IndexReaderDelegate<TKey, TValue> indexReader, IEqualityComparer<TKey> keyComparer, Boolean synchronized = false)
		{
			_list = new IndexedList<TValue>(capacity, synchronized);
			_index = _list.CreateHashIndex<TKey>("Index", indexReader, keyComparer);

			_list.RegisterChangeHandler(new ListChangeHandler(this));
		}

		public ListDictionary(Int32 capacity, IIndexReader<TKey, TValue> indexReader, IEqualityComparer<TKey> keyComparer, Boolean synchronized = false)
		{
			_list = new IndexedList<TValue>(capacity, synchronized);
			_index = _list.CreateHashIndex<TKey>("Index", indexReader, keyComparer);

			_list.RegisterChangeHandler(new ListChangeHandler(this));
		}

		public ListDictionary(IEnumerable<TValue> collection, IndexReaderDelegate<TKey, TValue> indexReader, IEqualityComparer<TKey> keyComparer, Boolean synchronized = false)
		{
			_list = new IndexedList<TValue>(collection, synchronized);
			_index = _list.CreateHashIndex<TKey>("Index", indexReader, keyComparer);

			_list.RegisterChangeHandler(new ListChangeHandler(this));
		}

		public ListDictionary(IEnumerable<TValue> collection, IIndexReader<TKey, TValue> indexReader, IEqualityComparer<TKey> keyComparer, Boolean synchronized = false)
		{
			_list = new IndexedList<TValue>(collection, synchronized);
			_index = _list.CreateHashIndex<TKey>("Index", indexReader, keyComparer);

			_list.RegisterChangeHandler(new ListChangeHandler(this));
		}

		#endregion // Constructors

		#region Thread safety

		public RLockerSlim CreateReadLock()
		{
			return _list.CreateReadLock();
		}

		public URLockerSlim CreateUpgradableReadLock()
		{
			return _list.CreateUpgradableReadLock();
		}

		public WLockerSlim CreateWriteLock()
		{
			return _list.CreateWriteLock();
		}

		#endregion // Thread safety

		#region Own API

		public IIndexReader<TKey, TValue> IndexReader
		{
			get { return _index.IndexReader; }
		}

		public IEqualityComparer<TKey> KeyComparer
		{
			get { return _index.KeyComparer; }
		}

		public TValue[] ToArray()
		{
			return _list.ToArray();
		}

		public void Reindex()
		{
			_list.ReIndex<TKey>(_index.Id);
		}

		#endregion // Own API

		#region IList, IDictionary implementation

		public TValue this[Int32 index]
		{
			get { return _list[index]; }
			set { _list[index] = value; }
		}

		public TValue this[TKey key]
		{
			get
			{
				if (_index.TryGetValue(key, out TValue? value))
					return value;

				throw new KeyNotFoundException();
			}
			set
			{
				_index[key] = value;
			}
		}

		public Int32 Count => _list.Count;

		public Boolean IsReadOnly => false;

		public ICollection<TKey> Keys => _index.Keys;

		public ICollection<TValue> Values => _list;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _index.Keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _list;

		public void Add(TValue item)
		{
			_list.Add(item);
		}

		public void Add(TKey key, TValue value)
		{
			_index.VerifyKeyValueConsistency(key, value);

			_list.Add(value);
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			_list.Clear();
		}

		public Boolean Contains(TValue item)
		{
			return _index.ContainsValue(item);
		}

		public Boolean Contains(KeyValuePair<TKey, TValue> item)
		{
			return _index.Contains(item);
		}

		public Boolean ContainsKey(TKey key)
		{
			return _index.ContainsKey(key);
		}

		public void CopyTo(TValue[] array, Int32 arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex)
		{
			_index.CopyTo(array, arrayIndex);
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public Int32 IndexOf(TValue item)
		{
			return _index.IndexOfValue(item);
		}

		public Int32 IndexOf(KeyValuePair<TKey, TValue> item)
		{
			return _index.IndexOfItem(item);
		}

		public Int32 IndexOfKey(TKey key)
		{
			return _index.IndexOf(key);
		}

		public void Insert(Int32 index, TValue item)
		{
			_list.Insert(index, item);
		}

		public Boolean Remove(TValue item)
		{
			Int32 index = IndexOf(item);
			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public Boolean Remove(KeyValuePair<TKey, TValue> item)
		{
			Int32 index = IndexOf(item);
			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public Boolean Remove(TKey key)
		{
			Int32 index = IndexOfKey(key);
			if (index == -1)
				return false;

			RemoveAt(index);
			return true;
		}

		public void RemoveAt(Int32 index)
		{
			_list.RemoveAt(index);
		}

		public Boolean RemoveFirst()
		{
			return _list.RemoveFirst();
		}

		public Boolean RemoveLast()
		{
			return _list.RemoveLast();
		}

		public void Move(Int32 prevIndex, Int32 newIndex)
		{
			_list.Move(prevIndex, newIndex);
		}

		public Boolean TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
		{
			return _index.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return _index.GetEnumerator();
		}

		#endregion // IList, IDictionary implementation

		#region Events

		private class ListChangeHandler : IListChangeHandler<TValue>
		{
			public ListChangeHandler(ListDictionary<TKey, TValue> owner)
			{
				Owner = owner;
			}

			public ListDictionary<TKey, TValue> Owner { get; }

			public Object? OnBeginInsertValue(Int32 index, TValue value)
			{
				Owner.Inserting?.Invoke(Owner, index, value);

				return Owner._changeEmitter?.OnBeginInsertValue(index, value);
			}

			public void OnCommitInsertValue(Int32 index, TValue value, Object? state)
			{
				Owner._changeEmitter?.OnCommitInsertValue(index, value, state);

				try { Owner.Inserted?.Invoke(Owner, index, value); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				try { Owner.CollectionChanged?.Invoke(Owner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index)); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
			}

			public void OnRollbackInsertValue(Int32 index, TValue value, Object? state)
			{
				Owner._changeEmitter?.OnRollbackInsertValue(index, value, state);
			}

			public Object? OnBeginRemoveValue(Int32 index, TValue prevValue)
			{
				Owner.Removing?.Invoke(Owner, index, prevValue);

				return Owner._changeEmitter?.OnBeginRemoveValue(index, prevValue);
			}

			public void OnCommitRemoveValue(Int32 index, TValue prevValue, Object? state)
			{
				Owner._changeEmitter?.OnCommitRemoveValue(index, prevValue, state);

				try { Owner.Removed?.Invoke(Owner, index, prevValue); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				try { Owner.CollectionChanged?.Invoke(Owner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, prevValue, index)); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
			}

			public void OnRollbackRemoveValue(Int32 index, TValue prevValue, Object? state)
			{
				Owner._changeEmitter?.OnRollbackRemoveValue(index, prevValue, state);
			}

			public Object? OnBeginSetValue(Int32 index, TValue value, TValue prevValue)
			{
				Owner.Updating?.Invoke(Owner, index, value, prevValue);

				return Owner._changeEmitter?.OnBeginSetValue(index, value, prevValue);
			}

			public void OnCommitSetValue(Int32 index, TValue value, TValue prevValue, Object? state)
			{
				Owner._changeEmitter?.OnCommitSetValue(index, value, prevValue, state);

				try { Owner.Updated?.Invoke(Owner, index, value, prevValue); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				try { Owner.CollectionChanged?.Invoke(Owner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, prevValue, index)); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
			}

			public void OnRollbackSetValue(Int32 index, TValue value, TValue prevValue, Object? state)
			{
				Owner._changeEmitter?.OnRollbackSetValue(index, value, prevValue, state);
			}

			public Object? OnBeginMoveValue(Int32 indexNew, Int32 indexPrev, TValue value)
			{
				Owner.Moving?.Invoke(Owner, indexNew, indexPrev, value);

				return Owner._changeEmitter?.OnBeginMoveValue(indexNew, indexPrev, value);
			}

			public void OnCommitMoveValue(Int32 indexNew, Int32 indexPrev, TValue value, Object? state)
			{
				Owner._changeEmitter?.OnCommitMoveValue(indexNew, indexPrev, value, state);

				try { Owner.Moved?.Invoke(Owner, indexNew, indexPrev, value); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				try { Owner.CollectionChanged?.Invoke(Owner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, value, indexNew, indexPrev)); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
			}

			public void OnRollbackMoveValue(Int32 indexNew, Int32 indexPrev, TValue value, Object? state)
			{
				Owner._changeEmitter?.OnRollbackMoveValue(indexNew, indexPrev, value, state);
			}

			public Object? OnBeginClear(Int32 count)
			{
				Owner.Clearing?.Invoke(Owner);

				return Owner._changeEmitter?.OnBeginClear(count);
			}

			public void OnCommitClear(Int32 count, Object? state)
			{
				Owner._changeEmitter?.OnCommitClear(count, state);

				try { Owner.Cleared?.Invoke(Owner); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				try { Owner.CollectionChanged?.Invoke(Owner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); }
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
			}

			public void OnRollbackClear(Int32 count, Object? state)
			{
				Owner._changeEmitter?.OnRollbackClear(count, state);
			}
		}

		public event InsertHandler<TValue>? Inserting;
		public event InsertHandler<TValue>? Inserted;
		public event RemoveHandler<TValue>? Removing;
		public event RemoveHandler<TValue>? Removed;
		public event UpdateHandler<TValue>? Updating;
		public event UpdateHandler<TValue>? Updated;
		public event MoveHandler<TValue>? Moving;
		public event MoveHandler<TValue>? Moved;
		public event ClearHandler<TValue>? Clearing;
		public event ClearHandler<TValue>? Cleared;
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public void RegisterChangeHandler(IListChangeHandler<TValue> handler)
		{
			if (_changeEmitter == null)
				_changeEmitter = new StandardListChangeEmitter<TValue>();

			_changeEmitter.RegisterChangeHandler(handler);
		}

		public Boolean UnregisterChangeHandler(IListChangeHandler<TValue> handler)
		{
			if (_changeEmitter == null)
				return false;

			return _changeEmitter.UnregisterChangeHandler(handler);
		}

		#endregion // Events
	}
}
