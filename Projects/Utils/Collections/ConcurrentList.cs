using Armat.Utils.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Armat.Collections;

public sealed class ConcurrentList<T> : IList<T>, IReadOnlyList<T>, IList, IEquatable<ConcurrentList<T>>, IReadOnlyCollection<T>, IDisposable
{
	#region Data Members

	private readonly List<T> _list;
	private readonly ReaderWriterLockSlim _lock;

	#endregion // Data Members

	#region Constructors

	public ConcurrentList()
	{
		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
		_list = new List<T>();
	}

	public ConcurrentList(Int32 capacity)
	{
		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
		_list = new List<T>(capacity);
	}

	public ConcurrentList(IEnumerable<T> items)
	{
		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
		_list = new List<T>(items);
	}

	public ConcurrentList(ConcurrentList<T> items)
	{
		using var locker = items._lock.CreateRLocker();

		_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
		_list = new List<T>(items._list);
	}

	public void Dispose()
	{
		_lock.Dispose();
	}

	#endregion // Constructors

	#region IList implementation

	public void Add(T item)
	{
		using var locker = _lock.CreateWLocker();

		_list.Add(item);
	}

	public Int32 Add(Object? value)
	{
		if (value is T item)
		{
			using var locker = _lock.CreateWLocker();

			Int32 result = _list.Count;
			_list.Add(item);

			return result;
		}
		else
		{
			throw new InvalidCastException();
		}
	}

	public T? AddIf(T value, Func<Boolean> conditionEvaluator)
	{
		return AddIf(value, conditionEvaluator, out _);
	}

	public T? AddIf(Func<T> itemProvider, Func<Boolean> conditionEvaluator)
	{
		return AddIf(itemProvider, conditionEvaluator, out _);
	}

	public T? AddIf(T value, Func<Boolean> conditionEvaluator, out Int32 addedIndex)
	{
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
		{
			addedIndex = -1;
			return default;
		}

		using var locker = _lock.CreateWLocker();

		_list.Add(value);
		addedIndex = _list.Count;

		return value;
	}

	public T? AddIf(Func<T> itemProvider, Func<Boolean> conditionEvaluator, out Int32 addedIndex)
	{
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
		{
			addedIndex = -1;
			return default;
		}

		using var locker = _lock.CreateWLocker();

		T result = itemProvider();
		_list.Add(result);
		addedIndex = _list.Count;

		return result;
	}

	public void Insert(Int32 index, T item)
	{
		using var locker = _lock.CreateWLocker();

		_list.Insert(index, item);
	}

	public void Insert(Int32 index, Object? value)
	{
		if (value is T item)
			Insert(index, item);
		else
			throw new InvalidCastException();
	}

	public T? InsertIf(Int32 index, T value, Func<Boolean> conditionEvaluator)
	{
		return InsertIf(index, value, conditionEvaluator, out _);
	}

	public T? InsertIf(Int32 index, Func<T> itemProvider, Func<Boolean> conditionEvaluator)
	{
		return InsertIf(index, itemProvider, conditionEvaluator, out _);
	}

	public T? InsertIf(Int32 index, T value, Func<Boolean> conditionEvaluator, out Boolean inserted)
	{
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
		{
			inserted = false;
			return default;
		}

		using var locker = _lock.CreateWLocker();

		_list.Insert(index, value);
		inserted = true;

		return value;
	}

	public T? InsertIf(Int32 index, Func<T> itemProvider, Func<Boolean> conditionEvaluator, out Boolean inserted)
	{
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
		{
			inserted = false;
			return default;
		}

		using var locker = _lock.CreateWLocker();

		T result = itemProvider();
		_list.Insert(index, result);
		inserted = true;

		return result;
	}

	public Boolean Remove(T item)
	{
		using var locker = _lock.CreateWLocker();

		return _list.Remove(item);
	}

	public void Remove(Object? value)
	{
		if (value is T item)
			Remove(item);
		else
			throw new InvalidCastException();
	}

	public Boolean RemoveIf(T value, Func<Boolean> conditionEvaluator)
	{
		Boolean result = false;
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
			return result;

		using var locker = _lock.CreateWLocker();

		result = _list.Remove(value);

		return result;
	}

	public Boolean RemoveIf(Func<T> itemProvider, Func<Boolean> conditionEvaluator)
	{
		Boolean result = false;
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
			return result;

		using var locker = _lock.CreateWLocker();

		result = _list.Remove(itemProvider());

		return result;
	}

	public void RemoveAt(Int32 index)
	{
		using var locker = _lock.CreateWLocker();

		_list.RemoveAt(index);
	}

	public Boolean RemoveAtIf(Int32 index, Func<Boolean> conditionEvaluator)
	{
		using var urLocker = _lock.CreateURLocker();

		if (!conditionEvaluator())
			return false;

		using var locker = _lock.CreateWLocker();

		_list.RemoveAt(index);

		return true;
	}

	public void Clear()
	{
		using var urLocker = _lock.CreateURLocker();

		if (_list.Count == 0)
			return;

		using var locker = _lock.CreateWLocker();

		_list.Clear();
	}

	public Boolean Contains(T item)
	{
		using var locker = _lock.CreateRLocker();

		return _list.Contains(item);
	}

	public Boolean Contains(Object? value)
	{
		if (value is T item)
			return Contains(item);
		else
			throw new InvalidCastException();
	}

	public Int32 IndexOf(T item)
	{
		using var locker = _lock.CreateRLocker();

		return _list.IndexOf(item);
	}

	public Int32 IndexOf(Object? value)
	{
		if (value is T item)
			return IndexOf(item);
		else
			throw new InvalidCastException();
	}

	public void CopyTo(T[] array, Int32 arrayIndex)
	{
		using var locker = _lock.CreateRLocker();

		_list.CopyTo(array, arrayIndex);
	}

	public void CopyTo(Array array, Int32 index)
	{
		using var locker = _lock.CreateRLocker();

		((IList)_list).CopyTo(array, index);
	}

	// Note: The enumerator doesn't consider that items could be removed during the iteration
	// if so, it might skip some of those
	// to have consistent iteration use ToArray().GetENumerator() instead
	public IEnumerator<T> GetEnumerator()
	{
		return new ConcurrentListEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	public T[] ToArray()
	{
		using var locker = _lock.CreateRLocker();

		return _list.ToArray();
	}

	private sealed class ConcurrentListEnumerator : IEnumerator<T>
	{
		private readonly ConcurrentList<T> _owner;
		private Int32 _index;

		public ConcurrentListEnumerator(ConcurrentList<T> list)
		{
			_owner = list;
			_index = -1;
		}

		public Boolean MoveNext()
		{
			if (_index == -2)
				throw new ObjectDisposedException("this");

			using var locker = _owner._lock.CreateRLocker();

			_index++;
			return _index < _owner._list.Count;
		}

		public void Reset()
		{
			_index = -1;
		}

		public void Dispose()
		{
			_index = -2;
		}

		public T Current
		{
			get
			{
				using var locker = _owner._lock.CreateRLocker();

				if (_index >= _owner._list.Count)
					throw new InvalidOperationException();

				return _owner._list[_index];
			}
		}

		Object? IEnumerator.Current
		{
			get { return this.Current; }
		}
	}

	#endregion // IList implementation

	#region Properties

	public T this[Int32 index]
	{
		get
		{
			using var locker = _lock.CreateRLocker();

			return _list[index];
		}
		set
		{
			using var locker = _lock.CreateWLocker();

			_list[index] = value;
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
			if (value is T item)
				this[index] = item;
			else
				throw new InvalidCastException();
		}
	}

	public Int32 Capacity
	{
		get
		{
			using var locker = _lock.CreateRLocker();

			return _list.Capacity;
		}
		set
		{
			using var locker = _lock.CreateWLocker();

			_list.Capacity = value;
		}
	}

	public Int32 Count
	{
		get
		{
			using var locker = _lock.CreateRLocker();

			return _list.Count;
		}
	}

	public Boolean IsReadOnly
	{
		get { return false; }
	}

	public Boolean IsFixedSize
	{
		get { return false; }
	}

	public Boolean IsSynchronized
	{
		get { return true; }
	}

	public Object SyncRoot
	{
		get { return _lock; }
	}

	#endregion // Properties

	#region Internal methods

	public Boolean Equals(ConcurrentList<T>? list)
	{
		if (list == null || list.Count != Count)
			return false;

		using var thisLocker = _lock.CreateRLocker();
		using var thatLocker = list._lock.CreateRLocker();

		if (list._list.Count != _list.Count)
			return false;

		Int32 count = _list.Count;
		for (Int32 index = 0; index < count; index++)
		{
			if (!EqualityComparer<T>.Default.Equals(list._list[index], _list[index]))
				return false;
		}

		return true;
	}

	public override Boolean Equals(Object? obj)
	{
		return Equals(obj as ConcurrentList<T>);
	}

	public override Int32 GetHashCode()
	{
		const Int32 MaxCountForHashCode = 10;

		using var locker = _lock.CreateRLocker();

		Int32 hashCode = _list.Count.GetHashCode();

		Int32 count = _list.Count;
		Int32 step = 1;
		if (count > MaxCountForHashCode)
		{
			// calculate the rounded up step for iteration
			step = count / MaxCountForHashCode;
			if (step * MaxCountForHashCode != count)
				step++;
		}

		for (Int32 index = 0; index < count; index += step)
			hashCode = HashCode.Combine(hashCode, _list[index]);

		return hashCode;
	}

	public override String ToString()
	{
		const Int32 MaxCountForString = 10;

		return ToString(MaxCountForString);
	}

	public String ToString(Int32 maxCount)
	{
		StringBuilder sb = new();

		using var locker = _lock.CreateRLocker();

		Int32 listSize = _list.Count;
		Int32 count = Math.Min(listSize, maxCount);
		for (Int32 index = 0; index < count; index++)
		{
			if (index > 0)
				sb.Append(", ");

			T value = _list[index];
			if (value != null)
				sb.Append(value.ToString());
			else
				sb.Append('?');
		}
		if (listSize > maxCount)
			sb.Append(", ...");

		return sb.ToString();
	}

	#endregion // Internal methods
}
