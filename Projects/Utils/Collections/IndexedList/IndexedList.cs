using Armat.Utils;
using Armat.Utils.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Armat.Collections;
#pragma warning restore IDE0130 // Namespace does not match folder structure

// Represents a generic list of items
// which can be indexed by different fields
public class IndexedList<T> : IList<T>, IReadOnlyList<T>, IList, IEquatable<IndexedList<T>>, IListChangeEmitter<T>, INotifyCollectionChanged
	where T : notnull
{
	#region Data members

	private readonly List<T> _list;
	private List<Int32>? _mask;
	private List<Int32>? _zombies;

	private Dictionary<String, IIndexBase<T>>? _mapIndexesByName;
	private readonly IndexedListData _dataAccessorForIndex;

	private IndexedListChangeEmitter? _listChangeEmitter;

	private ReaderWriterLockSlim? _rwLock;

	#endregion // Data members

	#region Constructors

	public IndexedList()
		: this(EqualityComparer<T>.Default, false)
	{
	}

	public IndexedList(IEqualityComparer<T> valueComparer)
		: this(valueComparer, false)
	{
	}

	public IndexedList(Boolean synchronized)
		: this(EqualityComparer<T>.Default, synchronized)
	{
	}

	public IndexedList(IEqualityComparer<T> valueComparer, Boolean synchronized)
	{
		_list = new List<T>();
		_dataAccessorForIndex = new IndexedListData(this);

		ValueComparer = valueComparer;
		IsSynchronized = synchronized;
	}

	public IndexedList(Int32 capacity)
		: this(capacity, EqualityComparer<T>.Default, false)
	{
	}

	public IndexedList(Int32 capacity, IEqualityComparer<T> valueComparer)
		: this(capacity, valueComparer, false)
	{
	}

	public IndexedList(Int32 capacity, Boolean synchronized)
		: this(capacity, EqualityComparer<T>.Default, synchronized)
	{
	}

	public IndexedList(Int32 capacity, IEqualityComparer<T> valueComparer, Boolean synchronized)
	{
		_list = new List<T>(capacity);
		_dataAccessorForIndex = new IndexedListData(this);

		ValueComparer = valueComparer;
		IsSynchronized = synchronized;
	}

	public IndexedList(IEnumerable<T> collection)
		: this(collection, EqualityComparer <T>.Default, false)
	{
	}

	public IndexedList(IEnumerable<T> collection, IEqualityComparer<T> valueComparer)
		: this(collection, valueComparer, false)
	{
	}

	public IndexedList(IEnumerable<T> collection, Boolean synchronized)
		: this(collection, EqualityComparer<T>.Default, synchronized)
	{
	}

	public IndexedList(IEnumerable<T> collection, IEqualityComparer<T> valueComparer, Boolean synchronized)
	{
		_list = new List<T>(collection);
		_dataAccessorForIndex = new IndexedListData(this);

		ValueComparer = valueComparer;
		IsSynchronized = synchronized;
	}

	public IndexedList(IndexedList<T> other) : this(other, null, null)
	{
	}

	public IndexedList(IndexedList<T> other, IEqualityComparer<T>? valueComparer, Boolean? synchronized)
	{
		using var rLock = other.CreateReadLock();

		// data list
		_list = new List<T>(other._list);
		_dataAccessorForIndex = new IndexedListData(this);

		// the mask
		if (other._mask != null)
		{
			_mask = new List<Int32>(other._mask);
			_zombies = new List<Int32>(other._zombies!);
		}

		// indexes and other change handlers
		Dictionary<IListChangeHandler<T>, IListChangeHandler<T>>? mapChangeHandlers = null;

		if (other._mapIndexesByName != null)
		{
			_mapIndexesByName = new Dictionary<String, IIndexBase<T>>(other._mapIndexesByName!.Count);
			mapChangeHandlers = new Dictionary<IListChangeHandler<T>, IListChangeHandler<T>>();

			foreach (IIndexBase<T> index in other._mapIndexesByName.Values)
			{
				IndexedListData dataWrapper = new(this);
				IIndexBase<T> clonedIndex = ((IIndexCloner<T>)index).Clone(this, dataWrapper);

				//dataWrapper.Index = clonedIndex;
				_mapIndexesByName.Add(clonedIndex.Id, clonedIndex);

				if (clonedIndex is IListChangeHandler<T> clonedChangeHandler)
					mapChangeHandlers.Add((IListChangeHandler<T>)index, clonedChangeHandler);
			}
		}
		if (other._listChangeEmitter != null)
		{
			_listChangeEmitter = new IndexedListChangeEmitter();

			foreach (IndexedListChangeEmitter.HandlerItem changeHandlerItem in other._listChangeEmitter.ChangeHandlers)
			{
				if (mapChangeHandlers != null && 
					mapChangeHandlers.TryGetValue(changeHandlerItem.Callback, out IListChangeHandler<T>? clonedChangeHandler))
				{
					_listChangeEmitter.RegisterChangeHandler(clonedChangeHandler, true);
				}
				else if (changeHandlerItem.Callback is ICloneable /*cloneableHandler*/)
				{
					// Do not clone other change handlers!
					//clonedChangeHandler = (IListChangeHandler<T>)((ICloneable)changeHandlerItem.Callback).Clone();
					//_listChangeEmitter.RegisterChangeHandler(clonedChangeHandler, changeHandlerItem.InternalIndexing);
				}
				else
				{
					throw new InvalidOperationException("Cannot clone the change handler");
				}
			}
		}

		// synchronized
		IsSynchronized = synchronized ?? other.IsSynchronized;
		ValueComparer = valueComparer ?? EqualityComparer<T>.Default;
	}

	~IndexedList()
	{
		ReaderWriterLockSlim? rwLock = Interlocked.Exchange(ref _rwLock, null);
		rwLock?.Dispose();
	}

	#endregion // Constructors

	#region Thread safety

	public RLockerSlim CreateReadLock()
	{
		return new RLockerSlim(_rwLock);
	}

	public URLockerSlim CreateUpgradableReadLock()
	{
		return new URLockerSlim(_rwLock);
	}

	public WLockerSlim CreateWriteLock()
	{
		return new WLockerSlim(_rwLock);
	}

	#endregion // Thread safety

	#region Internal methods

	public virtual Boolean Equals(IndexedList<T>? list)
	{
		if (list == null)
			return false;

		using var rLockThis = CreateReadLock();
		using var rLockThat = list.CreateReadLock();

		if (list.Count != Count)
			return false;

		Int32 count = Count;
		for (Int32 index = 0; index < count; index++)
		{
			if (!ValueComparer.Equals(list[index], this[index]))
				return false;
		}

		return true;
	}

	public override sealed Boolean Equals(Object? obj)
	{
		return Equals(obj as IndexedList<T>);
	}

	public override Int32 GetHashCode()
	{
		const Int32 MaxCountForHashCode = 10;

		using var rLock = CreateReadLock();

		Int32 count = ExternalCount;
		Int32 hashCode = count.GetHashCode();

		Int32 step = 1;
		if (count > MaxCountForHashCode)
		{
			// calculate the rounded up step for iteration
			step = count / MaxCountForHashCode;
			if (step * MaxCountForHashCode != count)
				step++;
		}

		for (Int32 index = 0; index < count; index += step)
			hashCode = HashCode.Combine(hashCode, _list[ToInternalIndex(index)]);

		return hashCode;
	}

	public override String ToString()
	{
		const Int32 MaxCountForString = 10;

		return ToString(MaxCountForString);
	}

	public virtual String ToString(Int32 maxCount)
	{
		StringBuilder sb = new();
		Int32 listSize = 0;

		{
			using var rLock = CreateReadLock();

			listSize = Count;
			Int32 count = Math.Min(listSize, maxCount);
			for (Int32 index = 0; index < count; index++)
			{
				if (index > 0)
					sb.Append(", ");

				T value = this[index];
				sb.Append(value.ToString());
			}
		}
		if (listSize > maxCount)
			sb.Append(", ...");

		return sb.ToString();
	}

	// non thread-safe method
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private Int32 ToInternalIndex(Int32 externalIndex)
	{
		Int32 result = externalIndex;

		if (_mask != null)
		{
			result = _mask[externalIndex];
		}
		else
		{
			if (result < 0 || result >= _list.Count)
				throw new ArgumentOutOfRangeException(nameof(externalIndex));
		}

		return result;
	}

	// non thread-safe method
	// Try to avoid heavy reverse index operations because of notification
	// TODO: It would be good to improve the ToExternalIndex performance
	private Int32 ToExternalIndex(Int32 internalIndex)
	{
		Int32 result = internalIndex;

		if (_mask != null)
		{
			result = _mask.IndexOf(internalIndex);
		}
		else
		{
			if (result < 0 || result >= _list.Count)
				throw new ArgumentOutOfRangeException(nameof(internalIndex));
		}

		return result;
	}

	// non thread-safe
	private Int32 InternalCount
	{
		get { return _list.Count; }
	}
	// non thread-safe
	private Int32 ExternalCount
	{
		get { return _mask == null ? _list.Count : _mask.Count; }
	}

	private void SetValueByIndex(Int32 indexInt, Int32 indexExt, T value)
	{
		// begin transaction
		T prevValue = _list[indexInt];
		Object? state;

		// begin transaction
		state = BeginSetValue(indexInt, indexExt, value, prevValue);

		try
		{
			// apply the change
			_list[indexInt] = value;

			// commit must never fail!
			CommitSetValue(indexInt, indexExt, value, prevValue, state);
		}
		catch
		{
			//// revert the change
			//_list[indexInt] = prevValue;

			// roll back must never fail!
			RollbackSetValue(indexInt, indexExt, value, prevValue, state);

			// rethrow the exception
			throw;
		}
	}

	#endregion // Internal methods

	#region IList implementation

	public T this[Int32 index]
	{
		get
		{
			using var rLock = CreateReadLock();

			return _list[ToInternalIndex(index)];
		}
		set
		{
			using var wLock = CreateWriteLock();

			// validate the index
			Int32 indexInt = ToInternalIndex(index);

			SetValueByIndex(indexInt, index, value);
		}
	}

	Object? IList.this[Int32 index]
	{
		get
		{
			return this[index];
		}
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			this[index] = (T)value;
		}
	}

	public Int32 Count
	{
		get
		{
			using var rLock = CreateReadLock();

			return ExternalCount;
		}
	}

	public Int32 Capacity
	{
		get
		{
			using var rLock = CreateReadLock();

			// capacity of the list itself
			return _list.Capacity;
		}
		set
		{
			using var wLock = CreateWriteLock();

			// capacity of the list itself
			_list.Capacity = value;
		}
	}

	public Boolean IsReadOnly
	{
		get
		{
			return false;
		}
	}

	public Boolean IsFixedSize
	{
		get
		{
			return false;
		}
	}

	public IEqualityComparer<T> ValueComparer { get; }

	public Boolean IsSynchronized
	{
		get
		{
			return _rwLock != null;
		}
		private set
		{
			if (!value)
			{
				// dispose the read / write lock
				ReaderWriterLockSlim? rwLock = Interlocked.Exchange(ref _rwLock, null);
				rwLock?.Dispose();
			}
			else
			{
				// create read / write lock
				_rwLock ??= new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
			}
		}
	}

	public Object SyncRoot
	{
		get
		{
			return (Object?)_rwLock ?? this;
		}
	}

	public Int32 Add(T item)
	{
		return Insert(Int32.MaxValue, item);
	}

	void ICollection<T>.Add(T item)
	{
		this.Add(item);
	}

	Int32 IList.Add(Object? value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		return this.Add((T)value!);
	}

	public void Clear()
	{
		using var wLock = CreateWriteLock();

		Int32 count = ExternalCount;
		Object? state = null;

		// begin transaction
		state = BeginClear(count);

		try
		{
			// apply the change
			_list.Clear();
			if (_mask != null)
			{
				_mask.Clear();
				_zombies!.Clear();

				_mask = null;
				_zombies = null;
			}

			// commit must never fail!
			CommitClear(count, state);
		}
		catch
		{
			// roll back must never fail!
			RollbackClear(count, state);

			// rethrow the exception
			throw;
		}
	}

	public Boolean Contains(T item)
	{
		return IndexOf(item) != -1;
	}

	public Boolean Contains(Object? value)
	{
		if (value is T typedValue)
			return Contains(typedValue);

		return false;
	}

	public void CopyTo(T[] array, Int32 arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		using var rLock = CreateReadLock();

		if (_mask == null)
		{
			// case of a simple list
			Int32 count = Math.Min(_list.Count, array.Length - arrayIndex);
			_list.CopyTo(0, array, arrayIndex, count);
		}
		else
		{
			// case of a masked list
			Int32 externalCount = Math.Min(_mask.Count, array.Length - arrayIndex);

			for (Int32 externalIndex = 0; externalIndex < externalCount; externalIndex++)
			{
				Int32 internalIndex = _mask[externalIndex];
				array[externalIndex + arrayIndex] = _list[internalIndex];
			}
		}
	}

	public void CopyTo(Array array, Int32 arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		using var rLock = CreateReadLock();

		if (_mask == null)
		{
			// case of a simple list
			Int32 count = Math.Min(_list.Count, array.Length - arrayIndex);

			for (Int32 index = 0; index < count; index++)
			{
				array.SetValue(_list[index], index + arrayIndex);
			}
		}
		else
		{
			// case of a masked list
			Int32 externalCount = Math.Min(_mask.Count, array.Length - arrayIndex);

			for (Int32 externalIndex = 0; externalIndex < externalCount; externalIndex++)
			{
				Int32 internalIndex = _mask[externalIndex];
				array.SetValue(_list[internalIndex], externalIndex + arrayIndex);
			}
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new IndexedListEnumerator(this);
	}

	public Int32 IndexOf(T item)
	{
		using var rLock = CreateReadLock();

		if (_mask == null)
		{
			// case of a simple list
			Int32 count = _list.Count;

			for (Int32 index = 0; index < count; index++)
			{
				if (ValueComparer.Equals(_list[index], item))
					return index;
			}
		}
		else
		{
			// case of a masked list
			Int32 externalCount = _mask.Count;

			for (Int32 externalIndex = 0; externalIndex < externalCount; externalIndex++)
			{
				Int32 internalIndex = _mask[externalIndex];
				if (ValueComparer.Equals(_list[internalIndex], item))
					return externalIndex;
			}
		}

		return -1;
	}

	public Int32 IndexOf(Object? value)
	{
		if (value is T typedValue)
			return IndexOf(typedValue);

		return -1;
	}

	public Int32 Insert(Int32 index, T item)
	{
		using var urLock = CreateUpgradableReadLock();

		// validate the index
		Int32 zombieIndex = -1;
		Int32 internalIndex = -1;

		// case of reusing zombie index if any
		if (_zombies != null && _zombies.Count > 0)
		{
			// try reusing zombie indexes if it's not trying to add to the end of list
			zombieIndex = _zombies.Count - 1;
			internalIndex = _zombies[zombieIndex];
		}
		else
		{
			// will add the new element to the end
			internalIndex = _list.Count;
		}

		Int32 externalCount = ExternalCount;
		if (index == Int32.MaxValue)
		{
			// consider adding to the tail
			index = externalCount;
		}
		else
		{
			// validate the list index
			if (index < 0 || index > externalCount)
				throw new ArgumentOutOfRangeException(nameof(index));
		}

		// perform the insertion
		using var wLock = CreateWriteLock();

		Object? state = null;
		Boolean bListUpdated = false, bMaskUpdated = false, bMaskCreated = false;

		// begin transaction
		state = BeginInsertValue(internalIndex, index, item);

		try
		{
			// commit
			if (zombieIndex == -1)
			{
				// there's no index to reuse, just add a new item
				_list.Add(item);
			}
			else
			{
				// this will reuse an index previously marked as zombie
				_list[internalIndex] = item;
			}
			bListUpdated = true;

			// update the mask
			if (_mask == null)
			{
				// if there's no mask, then internal indexes correspond to the external ones
				// said above, we need to create the mask only if the newly added item should not appear at the end
				if (index != externalCount)
				{
					// generate a mask with all indexes including the added one
					List<Int32> mask = new(_list.Count);
					List<Int32> zombies = new();

					if (index > 0)
						mask.AddRange(System.Linq.Enumerable.Range(0, index));
					mask.Add(internalIndex);
					if (index < _list.Count - 1)
						mask.AddRange(System.Linq.Enumerable.Range(index, _list.Count - index - 1));

					_mask = mask;
					_zombies = zombies;

					bMaskCreated = true;
				}
			}
			else
			{
				// update the mask to point to the internal item index at the right position
				_mask.Insert(index, internalIndex);
				bMaskUpdated = true;
			}

			// release the zombie index considering all operations went successful
			if (zombieIndex != -1)
				_zombies!.RemoveAt(zombieIndex);

			// commit must never fail!
			CommitInsertValue(internalIndex, index, item, state);
		}
		catch
		{
			// reset the mask
			if (bMaskCreated)
			{
				// the mask has been just created
				_mask = null;
				_zombies = null;
			}
			else if (bMaskUpdated)
			{
				// roll back the mask
				_mask!.RemoveAt(index);
				if (zombieIndex != -1)
					_zombies!.Insert(zombieIndex, internalIndex);
			}

			// reset the list
			if (bListUpdated)
			{
				if (zombieIndex == -1)
				{
					// this will remove the last one
					_list.RemoveAt(internalIndex);
				}
				else
				{
					// this will reset the item to the default value (or null)
					// this value will never be returned out of the class, so null values should be acceptable.
					_list[internalIndex] = default!;
				}
			}

			// roll back must never fail!
			RollbackInsertValue(internalIndex, index, item, state);

			// rethrow the exception
			throw;
		}

		return index;
	}

	void IList<T>.Insert(Int32 index, T item)
	{
		this.Insert(index, item);
	}

	public void Insert(Int32 index, Object? value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		Insert(index, (T)value);
	}

	public Boolean Remove(T item)
	{
		using var urLock = CreateUpgradableReadLock();

		Int32 index = IndexOf(item);
		if (index == -1)
			return false;

		RemoveAt(index);
		return true;
	}

	public void Remove(Object? value)
	{
		if (value is T typedValue)
			Remove(typedValue);
	}

	public Boolean RemoveFirst()
	{
		using var urLock = CreateUpgradableReadLock();

		Int32 count = ExternalCount;
		if (count == 0)
			return false;

		RemoveAt(0);
		return true;
	}

	public Boolean RemoveLast()
	{
		using var urLock = CreateUpgradableReadLock();

		Int32 count = ExternalCount;
		if (count == 0)
			return false;

		RemoveAt(count - 1);
		return true;
	}

	public void RemoveAt(Int32 index)
	{
		using var urLock = CreateUpgradableReadLock();

		// validate the index
		Int32 internalIndex = ToInternalIndex(index);
		if (internalIndex < 0 || internalIndex >= _list.Count)
			throw new ArgumentOutOfRangeException(nameof(index));

		// perform the removal
		using var wLock = CreateWriteLock();

		Object? state = null;
		Boolean bListUpdated = false, bMaskUpdated = false, bMaskCreated = false;
		T prevValue = _list[internalIndex];

		// begin transaction
		state = BeginRemoveValue(internalIndex, index, prevValue);

		try
		{
			// commit
			// this will reset the item to the default value (or null)
			// this value will never be returned out of the class, so null values should be acceptable.
			_list[internalIndex] = default!;
			bListUpdated = true;

			// update the mask
			if (_mask == null)
			{
				// generate a mask with all indexes excluding the removed one
				List<Int32> mask = new(_list.Count - 1);
				List<Int32> zombies = new();

				if (internalIndex > 0)
					mask.AddRange(System.Linq.Enumerable.Range(0, internalIndex));
				if (internalIndex < _list.Count - 1)
					mask.AddRange(System.Linq.Enumerable.Range(internalIndex + 1, _list.Count - internalIndex - 1));

				_mask = mask;
				_zombies = zombies;

				bMaskCreated = true;
			}
			else
			{
				// remove index-th element from the mask
				_mask.RemoveAt(index);
				bMaskUpdated = true;
			}

			// remember the new zombie index to reuse it moving forward
			_zombies!.Add(internalIndex);

			// commit must never fail!
			CommitRemoveValue(internalIndex, index, prevValue, state);
		}
		catch
		{
			// reset the mask
			if (bMaskCreated)
			{
				// the mask has been just created
				_mask = null;
				_zombies = null;
			}
			else if (bMaskUpdated)
			{
				// roll back the mask
				_mask!.Insert(index, internalIndex);
				_zombies!.Remove(internalIndex);
			}

			// reset the list
			if (bListUpdated)
				_list[internalIndex] = prevValue;

			// roll back must never fail!
			RollbackRemoveValue(internalIndex, index, prevValue, state);

			// rethrow the exception
			throw;
		}
	}

	public void Move(Int32 prevIndex, Int32 newIndex)
	{
		if (prevIndex == newIndex)
			return;

		using var urLock = CreateUpgradableReadLock();

		// validate the old index
		Int32 internalIndex = ToInternalIndex(prevIndex);
		if (internalIndex < 0 || internalIndex >= _list.Count)
			throw new ArgumentOutOfRangeException(nameof(prevIndex));

		// validate the map index
		if (newIndex == Int32.MaxValue)
		{
			newIndex = ExternalCount;
		}
		else
		{
			if (newIndex < 0 || newIndex > ExternalCount)
				throw new ArgumentOutOfRangeException(nameof(newIndex));
		}

		// perform the move
		using var wLock = CreateWriteLock();

		Object? state = null;
		Boolean bMaskRemoved = false, bMaskAdded = false, bMaskCreated = false;
		T prevValue = _list[internalIndex];

		// begin transaction
		state = BeginMoveValue(internalIndex, newIndex, internalIndex, prevIndex, prevValue);

		try
		{
			// update the mask
			if (_mask == null)
			{
				// generate a mask with all indexes excluding the removed one
				List<Int32> mask = new(_list.Count - 1);
				List<Int32> zombies = new();

				if (internalIndex > 0)
					mask.AddRange(System.Linq.Enumerable.Range(0, internalIndex));
				if (internalIndex < _list.Count - 1)
					mask.AddRange(System.Linq.Enumerable.Range(internalIndex + 1, _list.Count - internalIndex - 1));

				_mask = mask;
				_zombies = zombies;

				bMaskCreated = true;
			}
			else
			{
				// remove index-th element from the mask
				_mask.RemoveAt(prevIndex);
				bMaskRemoved = true;
			}
			_mask.Insert(newIndex, internalIndex);
			bMaskAdded = true;

			// commit must never fail!
			CommitMoveValue(internalIndex, newIndex, internalIndex, prevIndex, prevValue, state);
		}
		catch
		{
			// reset the mask
			if (bMaskCreated)
			{
				// the mask has been just created
				_mask = null;
				_zombies = null;
			}
			else
			{
				if (bMaskAdded)
				{
					// remove the failed item from the mask
					_mask!.RemoveAt(newIndex);
				}
				if (bMaskRemoved)
				{
					// restore the original position in the mask
					_mask!.Insert(prevIndex, internalIndex);
				}
			}

			// roll back must never fail!
			RollbackMoveValue(internalIndex, newIndex, internalIndex, prevIndex, prevValue, state);

			// rethrow the exception
			throw;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	public T[] ToArray()
	{
		using var rLock = CreateReadLock();

		// get number of elements
		Int32 count = Count;
		if (count == 0)
			return Array.Empty<T>();

		// allocate an array
		T[] result = new T[count];
		CopyTo(result, 0);

		return result;
	}

	private class IndexedListEnumerator : IEnumerator<T>
	{
		private readonly IndexedList<T> _data;
		private Int32 _index = -1;

		public IndexedListEnumerator(IndexedList<T> data)
		{
			_data = data;
		}

		public T Current => _index != -2 ? _data[_index] : throw new ObjectDisposedException("IndexedListEnumerator");

		Object? IEnumerator.Current => this.Current;

		public void Dispose()
		{
			_index = -2;
		}

		public Boolean MoveNext()
		{
			if (_index == -2)
				throw new ObjectDisposedException("IndexedListEnumerator");

			_index++;
			return _index < _data.Count;
		}

		public void Reset()
		{
			_index = -1;
		}
	}

	#endregion // IList implementation

	#region Events

	public event InsertHandler<T>? Inserting;
	public event InsertHandler<T>? Inserted;

	public event RemoveHandler<T>? Removing;
	public event RemoveHandler<T>? Removed;

	public event UpdateHandler<T>? Updating;
	public event UpdateHandler<T>? Updated;

	public event MoveHandler<T>? Moving;
	public event MoveHandler<T>? Moved;

	public event ClearHandler<T>? Clearing;
	public event ClearHandler<T>? Cleared;

	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	public void RegisterChangeHandler(IListChangeHandler<T> changeHandler)
	{
		RegisterChangeHandler(changeHandler, false);
	}

	protected void RegisterChangeHandler(IListChangeHandler<T> changeHandler, Boolean useInternalIndexes)
	{
		_listChangeEmitter ??= new IndexedListChangeEmitter();

		_listChangeEmitter.RegisterChangeHandler(changeHandler, useInternalIndexes);
	}

	public Boolean UnregisterChangeHandler(IListChangeHandler<T> changeHandler)
	{
		if (_listChangeEmitter == null)
			return false;

		return _listChangeEmitter.UnregisterChangeHandler(changeHandler);
	}

	private class IndexedListChangeEmitter
	{
		public IndexedListChangeEmitter()
		{
			_listChangeHandlers = new List<HandlerItem>();
		}

		public struct HandlerItem
		{
			public IListChangeHandler<T> Callback { get; set; }
			public Boolean InternalIndexing { get; set; }
		}

		private readonly List<HandlerItem> _listChangeHandlers;
		public IReadOnlyCollection<HandlerItem> ChangeHandlers { get { return _listChangeHandlers.AsReadOnly(); } }

		public virtual void RegisterChangeHandler(IListChangeHandler<T> handler, Boolean useInternalIndexes)
		{
			_listChangeHandlers.Add(new HandlerItem() { Callback = handler, InternalIndexing = useInternalIndexes });
		}
		public virtual Boolean UnregisterChangeHandler(IListChangeHandler<T> handler)
		{
			for (Int32 index = _listChangeHandlers.Count - 1; index >= 0; index--)
			{
				if (_listChangeHandlers[index].Callback == handler)
				{
					_listChangeHandlers.RemoveAt(index);
					return true;
				}
			}

			return false;
		}

		public Object? OnBeginInsertValue(Int32 indexInt, Int32 indexExt, T value)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return null;

			Int32 index = 0, chCount = _listChangeHandlers.Count;
			Object? result;
			Object?[]? arrResults = null;

			try
			{
				// begin the operation
				// if there's an exception thrown, the operation won't be completed
				if (chCount > 1)
				{
					result = arrResults = new Object[chCount];
					for (index = 0; index < chCount; index++)
					{
						HandlerItem item = _listChangeHandlers[index];
						arrResults[index] = item.Callback.OnBeginInsertValue(item.InternalIndexing ? indexInt : indexExt, value);
					}
				}
				else
				{
					HandlerItem item = _listChangeHandlers[0];
					result = item.Callback.OnBeginInsertValue(item.InternalIndexing ? indexInt : indexExt, value);
				}
			}
			catch (Exception exc)
			{
				// roll back the ones that have succeeded
				if (chCount > 1)
				{
					for (index--; index >= 0; index--)
					{
						HandlerItem item = _listChangeHandlers[index];
						try { item.Callback.OnRollbackInsertValue(item.InternalIndexing ? indexInt : indexExt, value, arrResults![index]); }
						catch { }
					}
				}
				// There's nothing to roll back if the only begin has failed
				//else
				//{
				//	try { _listChangeHandlers[0].OnRollbackInsertValue(index, value, result); }
				//	catch { }
				//}

				// ensure not to wrap the OperationCanceledException in another one
				if (exc is OperationCanceledException)
					throw;

				throw new OperationCanceledException("Insertion of value has been canceled", exc);
			}

			return result;
		}

		public void OnCommitInsertValue(Int32 indexInt, Int32 indexExt, T value, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// commit the transaction
			// ensure that exception from one commit doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				//System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnCommitInsertValue(item.InternalIndexing ? indexInt : indexExt, value, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnCommitInsertValue(item.InternalIndexing ? indexInt : indexExt, value, state); }
				catch { }
			}
		}

		public void OnRollbackInsertValue(Int32 indexInt, Int32 indexExt, T value, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// rollback the transaction
			// ensure that exception from one rolling back doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnRollbackInsertValue(item.InternalIndexing ? indexInt : indexExt, value, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnRollbackInsertValue(item.InternalIndexing ? indexInt : indexExt, value, state); }
				catch { }
			}
		}

		public Object? OnBeginRemoveValue(Int32 indexInt, Int32 indexExt, T prevValue)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return null;

			Int32 index = 0, chCount = _listChangeHandlers.Count;
			Object? result;
			Object?[]? arrResults = null;

			try
			{
				// begin the operation
				// if there's an exception thrown, the operation won't be completed
				if (chCount > 1)
				{
					result = arrResults = new Object[chCount];
					for (index = 0; index < chCount; index++)
					{
						HandlerItem item = _listChangeHandlers[index];
						arrResults[index] = item.Callback.OnBeginRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue);
					}
				}
				else
				{
					HandlerItem item = _listChangeHandlers[0];
					result = item.Callback.OnBeginRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue);
				}
			}
			catch (Exception exc)
			{
				// roll back the ones that have succeeded
				if (chCount > 1)
				{
					for (index--; index >= 0; index--)
					{
						HandlerItem item = _listChangeHandlers[index];
						try { item.Callback.OnRollbackRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue, arrResults![index]); }
						catch { }
					}
				}
				// There's nothing to roll back if the only begin has failed
				//else
				//{
				//	try { _listChangeHandlers[0].OnRollbackRemoveValue(index, prevValue, result); }
				//	catch { }
				//}

				// ensure not to wrap the OperationCanceledException in another one
				if (exc is OperationCanceledException)
					throw;

				throw new OperationCanceledException("Removal of value has been canceled", exc);
			}

			return result;
		}

		public void OnCommitRemoveValue(Int32 indexInt, Int32 indexExt, T prevValue, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// commit the transaction
			// ensure that exception from one commit doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnCommitRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnCommitRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue, state); }
				catch { }
			}
		}

		public void OnRollbackRemoveValue(Int32 indexInt, Int32 indexExt, T prevValue, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// rollback the transaction
			// ensure that exception from one rolling back doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnRollbackRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnRollbackRemoveValue(item.InternalIndexing ? indexInt : indexExt, prevValue, state); }
				catch { }
			}
		}

		public Object? OnBeginSetValue(Int32 indexInt, Int32 indexExt, T value, T prevValue)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return null;

			Int32 index = 0, chCount = _listChangeHandlers.Count;
			Object? result;
			Object?[]? arrResults = null;

			try
			{
				// begin the operation
				// if there's an exception thrown, the operation won't be completed
				if (chCount > 1)
				{
					result = arrResults = new Object[chCount];
					for (index = 0; index < chCount; index++)
					{
						HandlerItem item = _listChangeHandlers[index];
						arrResults[index] = item.Callback.OnBeginSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue);
					}
				}
				else
				{
					HandlerItem item = _listChangeHandlers[0];
					result = item.Callback.OnBeginSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue);
				}
			}
			catch (Exception exc)
			{
				// roll back the ones that have succeeded
				if (chCount > 1)
				{
					for (index--; index >= 0; index--)
					{
						HandlerItem item = _listChangeHandlers[index];
						try { item.Callback.OnRollbackSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue, arrResults![index]); }
						catch { }
					}
				}
				// There's nothing to roll back if the only begin has failed
				//else
				//{
				//	try { _listChangeHandlers[0].OnRollbackSetValue(index, value, prevValue, result); }
				//	catch { }
				//}

				// ensure not to wrap the OperationCanceledException in another one
				if (exc is OperationCanceledException)
					throw;

				throw new OperationCanceledException("Setting of value has been canceled", exc);
			}

			return result;
		}

		public void OnCommitSetValue(Int32 indexInt, Int32 indexExt, T value, T prevValue, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// commit the transaction
			// ensure that exception from one commit doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnCommitSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnCommitSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue, state); }
				catch { }
			}
		}

		public void OnRollbackSetValue(Int32 indexInt, Int32 indexExt, T value, T prevValue, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// rollback the transaction
			// ensure that exception from one rolling back doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnRollbackSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnRollbackSetValue(item.InternalIndexing ? indexInt : indexExt, value, prevValue, state); }
				catch { }
			}
		}

		public Object? OnBeginMoveValue(Int32 indexIntNew, Int32 indexExtNew, Int32 indexIntPrev, Int32 indexExtPrev, T value)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return null;

			Int32 index = 0, chCount = _listChangeHandlers.Count;
			Object? result;
			Object?[]? arrResults = null;

			try
			{
				// begin the operation
				// if there's an exception thrown, the operation won't be completed
				if (chCount > 1)
				{
					result = arrResults = new Object[chCount];
					for (index = 0; index < chCount; index++)
					{
						HandlerItem item = _listChangeHandlers[index];
						arrResults[index] = item.Callback.OnBeginMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value);
					}
				}
				else
				{
					HandlerItem item = _listChangeHandlers[0];
					result = item.Callback.OnBeginMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value);
				}
			}
			catch (Exception exc)
			{
				// roll back the ones that have succeeded
				if (chCount > 1)
				{
					for (index--; index >= 0; index--)
					{
						HandlerItem item = _listChangeHandlers[index];
						try { item.Callback.OnRollbackMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value, arrResults![index]); }
						catch { }
					}
				}
				// There's nothing to roll back if the only begin has failed
				//else
				//{
				//	try { _listChangeHandlers[0].OnRollbackSetValue(index, value, prevValue, result); }
				//	catch { }
				//}

				// ensure not to wrap the OperationCanceledException in another one
				if (exc is OperationCanceledException)
					throw;

				throw new OperationCanceledException("Setting of value has been canceled", exc);
			}

			return result;
		}

		public void OnCommitMoveValue(Int32 indexIntNew, Int32 indexExtNew, Int32 indexIntPrev, Int32 indexExtPrev, T value, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// commit the transaction
			// ensure that exception from one commit doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnCommitMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnCommitMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value, state); }
				catch { }
			}
		}

		public void OnRollbackMoveValue(Int32 indexIntNew, Int32 indexExtNew, Int32 indexIntPrev, Int32 indexExtPrev, T value, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// rollback the transaction
			// ensure that exception from one rolling back doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					HandlerItem item = _listChangeHandlers[index];
					try { item.Callback.OnRollbackMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				HandlerItem item = _listChangeHandlers[0];
				try { item.Callback.OnRollbackMoveValue(item.InternalIndexing ? indexIntNew : indexExtNew, item.InternalIndexing ? indexIntPrev : indexExtPrev, value, state); }
				catch { }
			}
		}

		public Object? OnBeginClear(Int32 count)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return null;

			Int32 index = 0, chCount = _listChangeHandlers.Count;
			Object? result;
			Object?[]? arrResults = null;

			try
			{
				// begin the operation
				// if there's an exception thrown, the operation won't be completed
				if (chCount > 1)
				{
					result = arrResults = new Object[chCount];
					for (index = 0; index < chCount; index++)
						arrResults[index] = _listChangeHandlers[index].Callback.OnBeginClear(count);
				}
				else
				{
					result = _listChangeHandlers[0].Callback.OnBeginClear(count);
				}
			}
			catch (Exception exc)
			{
				// roll back the ones that have succeeded
				if (chCount > 1)
				{
					for (index--; index >= 0; index--)
					{
						try { _listChangeHandlers[index].Callback.OnRollbackClear(count, arrResults![index]); }
						catch { }
					}
				}
				// There's nothing to roll back if the only begin has failed
				//else
				//{
				//	try { _listChangeHandlers[0].OnRollbackClear(result); }
				//	catch { }
				//}

				// ensure not to wrap the OperationCanceledException in another one
				if (exc is OperationCanceledException)
					throw;

				throw new OperationCanceledException("Clearing of list has been canceled", exc);
			}

			return result;
		}

		public void OnCommitClear(Int32 count, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// commit the transaction
			// ensure that exception from one commit doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					try { _listChangeHandlers[index].Callback.OnCommitClear(count, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				try { _listChangeHandlers[0].Callback.OnCommitClear(count, state); }
				catch { }
			}
		}

		public void OnRollbackClear(Int32 count, Object? state)
		{
			if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
				return;

			Int32 chCount = _listChangeHandlers.Count;

			// rollback the transaction
			// ensure that exception from one rolling back doesn't affect the others
			if (chCount > 1)
			{
				Object[] arrStates = (Object[])state!;
				System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == chCount);

				for (Int32 index = 0; index < chCount; index++)
				{
					try { _listChangeHandlers[index].Callback.OnRollbackClear(count, arrStates[index]); }
					catch { }
				}
			}
			else
			{
				try { _listChangeHandlers[0].Callback.OnRollbackClear(count, state); }
				catch { }
			}
		}
	}

	#endregion // Events

	#region Support of Indexes

	public HashIndex<TIndexType, T> CreateHashIndex<TIndexType>(String id, 
		IndexReaderDelegate<TIndexType, T> indexReader, IEqualityComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		return CreateHashIndex(id, new StandardIndexReader<TIndexType>(indexReader), comparer);
	}

	public HashIndex<TIndexType, T> CreateHashIndex<TIndexType>(String id, 
		IIndexReader<TIndexType, T> indexReader, IEqualityComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		HashIndex<TIndexType, T> index = new(comparer);

		CreateIndex(index, id, indexReader);

		return index;
	}

	public TreeIndex<TIndexType, T> CreateTreeIndex<TIndexType>(String id,
		IndexReaderDelegate<TIndexType, T> indexReader, IComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		return CreateTreeIndex(id, new StandardIndexReader<TIndexType>(indexReader), comparer);
	}

	public TreeIndex<TIndexType, T> CreateTreeIndex<TIndexType>(String id, 
		IIndexReader<TIndexType, T> indexReader, IComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		TreeIndex<TIndexType, T> index = new(comparer);

		CreateIndex(index, id, indexReader);

		return index;
	}

	public MultiHashIndex<TIndexType, T> CreateMultiHashIndex<TIndexType>(String id, 
		IndexReaderDelegate<TIndexType, T> indexReader, IEqualityComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		return CreateMultiHashIndex(id, new StandardIndexReader<TIndexType>(indexReader), comparer);
	}

	public MultiHashIndex<TIndexType, T> CreateMultiHashIndex<TIndexType>(String id, 
		IIndexReader<TIndexType, T> indexReader, IEqualityComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		MultiHashIndex<TIndexType, T> index = new(comparer);

		CreateIndex(index, id, indexReader);

		return index;
	}

	public MultiTreeIndex<TIndexType, T> CreateMultiTreeIndex<TIndexType>(String id, 
		IndexReaderDelegate<TIndexType, T> indexReader, IComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		return CreateMultiTreeIndex(id, new StandardIndexReader<TIndexType>(indexReader), comparer);
	}

	public MultiTreeIndex<TIndexType, T> CreateMultiTreeIndex<TIndexType>(String id, 
		IIndexReader<TIndexType, T> indexReader, IComparer<TIndexType>? comparer = null)
		where TIndexType : notnull
	{
		MultiTreeIndex<TIndexType, T> index = new(comparer);

		CreateIndex(index, id, indexReader);

		return index;
	}

	public IIndex<TIndexType, T> CreateIndex<TIndexType>(Type indexClassType, String id, IndexReaderDelegate<TIndexType, T> indexReader)
		where TIndexType : notnull
	{
		return CreateIndex<TIndexType>(indexClassType, id, new StandardIndexReader<TIndexType>(indexReader));
	}

	public IIndex<TIndexType, T> CreateIndex<TIndexType>(Type indexClassType, String id, 
		IIndexReader<TIndexType, T> indexReader)
		where TIndexType : notnull
	{
		if (indexClassType.FullName == null || !typeof(IIndex<TIndexType, T>).IsAssignableFrom(indexClassType))
			throw new ArgumentException($"Type {indexClassType} must implement \"IIndex<TIndexType, T>\" interface", nameof(indexClassType));

		// create the index instance
		Object objInstance = indexClassType.Assembly.CreateInstance(indexClassType.FullName)
			?? throw new ArgumentException($"Creation of an index of type {indexClassType} failed");

		IIndex<TIndexType, T> index = (IIndex<TIndexType, T>)objInstance;
		CreateIndex<TIndexType>(index, id, indexReader);

		return index;
	}

	public void CreateIndex<TIndexType>(IIndex<TIndexType, T> index, String id, IIndexReader<TIndexType, T> indexReader)
		where TIndexType : notnull
	{
		IIndexInitializer<TIndexType, T> initializer = index as IIndexInitializer<TIndexType, T>
			?? throw new NotImplementedException("interface \"IIndexInitializer\" is not implemented for the index");

		// create the index instance
		IndexedListData dataWrapper = _dataAccessorForIndex;
		initializer.Initialize(id, this, dataWrapper, indexReader);

		// register it
		RegisterIndex(index);
	}

	protected virtual void RegisterIndex(IIndexBase<T> index)
	{
		if (index.Id.Length == 0)
			throw new ArgumentException("Invalid index id");
		if (index.Count != 0)
			throw new ArgumentException("Index is not empty");

		if (_mapIndexesByName == null)
		{
			_mapIndexesByName = new Dictionary<String, IIndexBase<T>>();
		}
		else if (_mapIndexesByName.ContainsKey(index.Id))
		{
			throw new ArgumentException("Index is already registered");
		}

		if (index is IListChangeHandler<T> changeHandler)
		{
			// initialize the index by filling in all current list items
			if (_mask == null)
			{
				// register list elements in the new index
				for (Int32 i = 0; i < _list.Count; i++)
				{
					T value = _list[i];
					Object? state = changeHandler.OnBeginInsertValue(i, value);
					changeHandler.OnCommitInsertValue(i, value, state);
				}
			}
			else
			{
				// register list elements in the new index
				foreach (Int32 i in _mask)
				{
					T value = _list[i];
					Object? state = changeHandler.OnBeginInsertValue(i, value);
					changeHandler.OnCommitInsertValue(i, value, state);
				}
			}

			// register the change handler, so the index is notified about modifications in the list
			RegisterChangeHandler(changeHandler, true);
		}

		// register it
		_mapIndexesByName.Add(index.Id, index);
	}

	public Boolean DestroyIndex(IIndexBase<T> index)
	{
		if (index == null)
			return false;

		return UnregisterIndex(index.Id);
	}

	protected virtual Boolean UnregisterIndex(String id)
	{
		if (_mapIndexesByName == null)
			return false;

#pragma warning disable IDE0018 // Inline variable declaration
		IIndexBase<T>? index;
		if (!_mapIndexesByName.Remove(id, out index) || index == null)
			return false;
#pragma warning restore IDE0018 // Inline variable declaration

		if (index is IListChangeHandler<T> changeHandler)
		{
			// unregister the change handler of index
			UnregisterChangeHandler(changeHandler);

			// reset index contents
			Int32 count = index.Count;
			Object? state = changeHandler.OnBeginClear(count);
			changeHandler.OnCommitClear(count, state);
		}

		return true;
	}

	public IReadOnlyCollection<IIndexBase<T>> Indexes
	{
		get { return _mapIndexesByName != null ? _mapIndexesByName.Values : Array.Empty<IIndexBase<T>>(); }
	}

	public IIndexBase<T>? GetIndex(String name)
	{
		if (_mapIndexesByName == null)
			return null;

		return _mapIndexesByName.GetValueOrDefault(name);
	}

	public IIndex<TIndexType, T>? GetIndex<TIndexType>(String name)
		where TIndexType : notnull
	{
		return GetIndex(name) as IIndex<TIndexType, T>;
	}

	public void ReIndex<TIndexType>()
	{
		if (_mapIndexesByName == null)
			return;

		// recompute all indexes
		foreach (IIndexBase<T> index in _mapIndexesByName.Values)
			index.ReIndex();
	}

	public void ReIndex<TIndexType>(String id)
	{
		if (_mapIndexesByName == null)
			throw new KeyNotFoundException();

		// get the index
		IIndexBase<T> index = _mapIndexesByName.GetValueOrDefault(id)
			?? throw new KeyNotFoundException();

		// reindex
		index.ReIndex();
	}

	protected class StandardIndexReader<TIndexType> : IIndexReader<TIndexType, T>
		where TIndexType : notnull
	{
		private readonly IndexReaderDelegate<TIndexType, T> _indexReader;

		public StandardIndexReader(IndexReaderDelegate<TIndexType, T> indexReader)
		{
			_indexReader = indexReader;
		}

		public TIndexType GetIndexValue(T item)
		{
			return _indexReader(item);
		}
	}

	#endregion // Support of Indexes

	#region Index Data List Class

	// this class is used to enable direct access to the internal list of IndexesList for indexes
	// indexes of this list are internal (matching the ones in IndexedList._list)
	// thread safety working with IndexedListData methods is guaranteed
	private class IndexedListData : IIndexedListAccessor<T>
	{
		private readonly IndexedList<T> _owner;

		public IndexedListData(IndexedList<T> owner)
		{
			_owner = owner;
			//_index = index;
		}

		public Int32 InternalCount
		{
			get
			{
				using var rLock = _owner.CreateReadLock();

				return _owner.InternalCount;
			}
		}
		public Int32 ExternalCount
		{
			get
			{
				using var rLock = _owner.CreateReadLock();

				return _owner.ExternalCount;
			}
		}

		public T this[Int32 internalIndex]
		{
			get
			{
				using var rLock = _owner.CreateReadLock();

				return _owner._list[internalIndex];
			}
			set
			{
				using var wLock = _owner.CreateWriteLock();

				Int32 externalIndex = ToExternalIndex(internalIndex);
				_owner.SetValueByIndex(internalIndex, externalIndex, value);
			}
		}

		public void Add(T item)
		{
			_owner.Add(item);
		}

		public void RemoveAt(Int32 internalIndex)
		{
			using var urLock = _owner.CreateUpgradableReadLock();

			Int32 externalIndex = ToExternalIndex(internalIndex);
			if (externalIndex == -1)
				throw new ArgumentOutOfRangeException(nameof(internalIndex));

			_owner.RemoveAt(externalIndex);
		}

		public void Clear()
		{
			_owner.Clear();
		}

		public Int32 ToInternalIndex(Int32 externalIndex)
		{
			return _owner.ToInternalIndex(externalIndex);
		}

		public Int32 ToExternalIndex(Int32 internalIndex)
		{
			return _owner.ToExternalIndex(internalIndex);
		}
	}

	#endregion // Index Data List Class

	#region List modification handlers

#pragma warning disable IDE0220 // Add explicit cast

	private Object? BeginInsertValue(Int32 indexInt, Int32 indexExt, T value)
	{
		// invoke the event
		Inserting?.Invoke(this, indexExt, value);

		return _listChangeEmitter?.OnBeginInsertValue(indexInt, indexExt, value);
	}

	private void CommitInsertValue(Int32 indexInt, Int32 indexExt, T value, Object? state)
	{
		try { _listChangeEmitter?.OnCommitInsertValue(indexInt, indexExt, value, state); }
		catch { }

		// invoke the event
		if (Inserted != null)
		{
			foreach (InsertHandler<T> handler in Inserted.GetInvocationList())
			{
				try { handler.Invoke(this, indexExt, value); }
				catch { }
			}
		}
		if (CollectionChanged != null)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, indexExt);
			foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
			{
				try { handler.Invoke(this, args); }
				catch { }
			}
		}
	}

	private void RollbackInsertValue(Int32 indexInt, Int32 indexExt, T value, Object? state)
	{
		try { _listChangeEmitter?.OnRollbackInsertValue(indexInt, indexExt, value, state); }
		catch { }
	}

	private Object? BeginRemoveValue(Int32 indexInt, Int32 indexExt, T prevValue)
	{
		// invoke the event
		Removing?.Invoke(this, indexExt, prevValue);

		return _listChangeEmitter?.OnBeginRemoveValue(indexInt, indexExt, prevValue);
	}

	private void CommitRemoveValue(Int32 indexInt, Int32 indexExt, T prevValue, Object? state)
	{
		try { _listChangeEmitter?.OnCommitRemoveValue(indexInt, indexExt, prevValue, state); }
		catch { }

		// invoke the event
		if (Removed != null)
		{
			foreach (RemoveHandler<T> handler in Removed.GetInvocationList())
			{
				try { handler.Invoke(this, indexExt, prevValue); }
				catch { }
			}
		}
		if (CollectionChanged != null)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, prevValue, indexExt);
			foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
			{
				try { handler.Invoke(this, args); }
				catch { }
			}
		}
	}

	private void RollbackRemoveValue(Int32 indexInt, Int32 indexExt, T prevValue, Object? state)
	{
		try { _listChangeEmitter?.OnRollbackRemoveValue(indexInt, indexExt, prevValue, state); }
		catch { }
	}

	private Object? BeginSetValue(Int32 indexInt, Int32 indexExt, T value, T prevValue)
	{
		// invoke the event
		Updating?.Invoke(this, indexExt, value, prevValue);

		return _listChangeEmitter?.OnBeginSetValue(indexInt, indexExt, value, prevValue);
	}

	private void CommitSetValue(Int32 indexInt, Int32 indexExt, T value, T prevValue, Object? state)
	{
		try { _listChangeEmitter?.OnCommitSetValue(indexInt, indexExt, value, prevValue, state); }
		catch { }

		// invoke the event
		if (Updated != null)
		{
			foreach (UpdateHandler<T> handler in Updated.GetInvocationList())
			{
				try { handler.Invoke(this, indexExt, value, prevValue); }
				catch { }
			}
		}
		if (CollectionChanged != null)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, prevValue, indexExt);
			foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
			{
				try { handler.Invoke(this, args); }
				catch { }
			}
		}
	}

	private void RollbackSetValue(Int32 indexInt, Int32 indexExt, T value, T prevValue, Object? state)
	{
		try { _listChangeEmitter?.OnRollbackSetValue(indexInt, indexExt, value, prevValue, state); }
		catch { }
	}

	private Object? BeginMoveValue(Int32 indexIntNew, Int32 indexExtNew, Int32 indexIntPrev, Int32 indexExtPrev, T value)
	{
		// invoke the event
		Moving?.Invoke(this, indexExtNew, indexExtPrev, value);

		return _listChangeEmitter?.OnBeginMoveValue(indexIntNew, indexExtNew, indexIntPrev, indexExtPrev, value);
	}

	private void CommitMoveValue(Int32 indexIntNew, Int32 indexExtNew, Int32 indexIntPrev, Int32 indexExtPrev, T value, Object? state)
	{
		try { _listChangeEmitter?.OnCommitMoveValue(indexIntNew, indexExtNew, indexIntPrev, indexExtPrev, value, state); }
		catch { }

		// invoke the event
		if (Moved != null)
		{
			foreach (MoveHandler<T> handler in Moved.GetInvocationList())
			{
				try { handler.Invoke(this, indexExtNew, indexExtPrev, value); }
				catch { }
			}
		}
		if (CollectionChanged != null)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, value, indexExtNew, indexExtPrev);
			foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
			{
				try { handler.Invoke(this, args); }
				catch { }
			}
		}
	}

	private void RollbackMoveValue(Int32 indexIntNew, Int32 indexExtNew, Int32 indexIntPrev, Int32 indexExtPrev, T value, Object? state)
	{
		try { _listChangeEmitter?.OnRollbackMoveValue(indexIntNew, indexExtNew, indexIntPrev, indexExtPrev, value, state); }
		catch { }
	}

	private Object? BeginClear(Int32 count)
	{
		// invoke the event
		Clearing?.Invoke(this);

		return _listChangeEmitter?.OnBeginClear(count);
	}

	private void CommitClear(Int32 count, Object? state)
	{
		try { _listChangeEmitter?.OnCommitClear(count, state); }
		catch { }

		// invoke the event
		if (Cleared != null)
		{
			foreach (ClearHandler<T> handler in Cleared.GetInvocationList())
			{
				try { handler.Invoke(this); }
				catch { }
			}
		}
		if (CollectionChanged != null)
		{
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
			{
				try { handler.Invoke(this, args); }
				catch { }
			}
		}
	}

	private void RollbackClear(Int32 count, Object? state)
	{
		try { _listChangeEmitter?.OnRollbackClear(count, state); }
		catch { }
	}
#pragma warning restore IDE0220 // Add explicit cast

	#endregion // List modification handlers
}
