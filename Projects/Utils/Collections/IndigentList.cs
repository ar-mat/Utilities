using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Armat.Collections;

public class IndigentList<T> : IList<T>, IReadOnlyList<T>, IList, IEquatable<IndigentList<T>>
{
	private Int32 _count;
	private T? _singleItem;
	private T[]? _arrItems;

	public IndigentList()
	{
	}

	public IndigentList(Int32 capacity)
	{
		if (capacity > 0)
			SetCapacity(capacity);
	}

	public IndigentList(ICollection<T> other)
	{
		Int32 size = other.Count;
		if (size != 0)
		{
			IEnumerator<T> enumerator = other.GetEnumerator();
			if (size == 1)
			{
				if (!enumerator.MoveNext())
					throw new InvalidOperationException();

				_singleItem = enumerator.Current;
			}
			else
			{
				SetCapacity(size);
				System.Diagnostics.Debug.Assert(_arrItems != null && _arrItems.Length == size);

				for (Int32 index = 0; index < size; index++)
				{
					if (!enumerator.MoveNext())
						throw new InvalidOperationException();

					_arrItems[index] = enumerator.Current;
				}
			}
		}
		_count = size;
	}

	public IndigentList(IndigentList<T> other)
	{
		// clone it
		_count = other._count;
		_singleItem = other._singleItem;
		if (other._arrItems != null)
			_arrItems = (T[])other._arrItems.Clone();
	}

	public Int32 Count
	{
		get { return _count; }
	}

	public Int32 Capacity
	{
		get
		{
			if (_arrItems != null)
			{
				System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
				return _arrItems.Length;
			}

			return 1;
		}
		set
		{
			// update the capacity
			SetCapacity(value);
		}
	}

	public Boolean IsReadOnly => false;

	public Boolean IsFixedSize => false;

	public Boolean IsSynchronized => false;

	public Object SyncRoot => this;

	public T this[Int32 index]
	{
		get
		{
			if (index < 0 || index >= _count)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_arrItems != null)
			{
				System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
				return _arrItems[index];
			}
			else
			{
				System.Diagnostics.Debug.Assert(_singleItem != null && index == 0 && _count == 1);
				return _singleItem;
			}
		}
		set
		{
			if (index < 0 || index >= _count)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_arrItems != null)
			{
				System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
				_arrItems[index] = value;
			}
			else
			{
				System.Diagnostics.Debug.Assert(index == 0 && _count == 1);
				_singleItem = value;
			}
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

	public void Add(T item)
	{
		if (_arrItems != null)
		{
			// grow the capacity if necessary
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			if (_arrItems.Length == _count)
			{
				EnsureCapacity(_count + 1);
				System.Diagnostics.Debug.Assert(_arrItems.Length > _count);
			}

			// append the item
			_arrItems[_count] = item;
		}
		else if (_count == 0)
		{
			// add the only item
			_singleItem = item;
		}
		else
		{
			// add the second item
			System.Diagnostics.Debug.Assert(_count == 1);
			SetCapacity(2);

			System.Diagnostics.Debug.Assert(_arrItems != null && _arrItems.Length >= 2);
			_arrItems[1] = item;
		}

		_count++;
	}

	public Int32 Add(Object? value)
	{
		if (value is T item)
			Add(item);
		else
			throw new InvalidCastException();

		return _count - 1;
	}

	public void Clear()
	{
		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);

			// do not preserve the capacity
			// Array.Clear(_arrItems, 0, _count);
			_arrItems = null;
			_count = 0;
		}
		else if (_count == 1)
		{
			_singleItem = default;
			_count = 0;
		}
		else
		{
			System.Diagnostics.Debug.Assert(_count == 0);
		}
	}

	public Boolean Contains(T item)
	{
		return IndexOf(item) != -1;
	}

	public Boolean Contains(Object? value)
	{
		if (value is T item)
			return Contains(item);

		return false;
	}

	public void CopyTo(T[] array, Int32 arrayIndex)
	{
		if (_count == 0)
		{
			return;
		}
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			Array.Copy(_arrItems, 0, array, arrayIndex, _count);
		}
		else
		{
			System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
			array[arrayIndex] = _singleItem;
		}
	}

	public void CopyTo(Int32 index, T[] array, Int32 arrayIndex, Int32 count)
	{
		if (_count == 0)
		{
			return;
		}
		if (array == null)
			throw new ArgumentNullException(nameof(array));
		if (count == 0)
			return;

		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			Array.Copy(_arrItems, index, array, arrayIndex, count);
		}
		else
		{
			if (index != 0 || count != 1)
				throw new IndexOutOfRangeException();

			System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
			array[arrayIndex] = _singleItem;
		}
	}

	public void CopyTo(Array array, Int32 index)
	{
		if (_count == 0)
		{
			return;
		}
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			Array.Copy(_arrItems, 0, array, index, _count);
		}
		else
		{
			System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
			array.SetValue(_singleItem, index);
		}
	}

	public T[] ToArray()
	{
		if (_count == 0)
		{
			return Array.Empty<T>();
		}

		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			T[] result = new T[_count];
			Array.Copy(_arrItems, result, _count);
			return result;

		}

		System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
		return new T[] { _singleItem };
	}

	public IEnumerator<T> GetEnumerator()
	{
		if (_count == 0)
			return EmptyEnumerator.GetInstance();

		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			return ((IEnumerable<T>)_arrItems).GetEnumerator();
		}

		System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
		return SingleItemEnumerator.GetInstance(_singleItem);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	public Int32 IndexOf(T item)
	{
		if (_count == 0)
		{
			return -1;
		}

		if (_arrItems != null)
		{
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			return Array.IndexOf<T>(_arrItems, item, 0, _count);
		}

		System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
		return EqualityComparer<T>.Default.Equals(_singleItem, item) ? 0 : -1;
	}

	public Int32 IndexOf(Object? value)
	{
		if (value is T item)
			return IndexOf(item);

		return -1;
	}

	public void Insert(Int32 index, T item)
	{
		if (index < 0 || index > _count)
			throw new IndexOutOfRangeException();

		if (_arrItems != null)
		{
			// grow the capacity if necessary
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			if (_arrItems.Length == _count)
			{
				EnsureCapacity(_count + 1);
				System.Diagnostics.Debug.Assert(_arrItems.Length > _count);
			}

			// shift array elements
			if (_count > index)
				Array.Copy(_arrItems, index, _arrItems, index + 1, _count - index);

			// insert the item
			_arrItems[index] = item;
		}
		else if (_count == 0)
		{
			// insert the only item
			System.Diagnostics.Debug.Assert(index == 0);
			_singleItem = item;
		}
		else if (_count == 1)
		{
			// insert the second item
			System.Diagnostics.Debug.Assert(index == 0 || index == 1);
			SetCapacity(2);

			System.Diagnostics.Debug.Assert(_arrItems != null && _arrItems.Length >= 2);
			if (index == 0)
			{
				_arrItems[1] = _arrItems[0];
				_arrItems[0] = item;
			}
			else
			{
				System.Diagnostics.Debug.Assert(index == 1);
				_arrItems[1] = item;
			}
		}

		_count++;
	}

	public void Insert(Int32 index, Object? value)
	{
		if (value is T item)
			Insert(index, item);
		else
			throw new InvalidCastException();
	}

	public Boolean Remove(T item)
	{
		Int32 index = IndexOf(item);
		if (index >= 0)
		{
			RemoveAt(index);
			return true;
		}

		return false;
	}

	public void Remove(Object? value)
	{
		if (value is T item)
			Remove(item);
	}

	public void RemoveAt(Int32 index)
	{
		if (index < 0 || index >= _count)
			throw new IndexOutOfRangeException();

		if (_arrItems != null)
		{
			// shift array elements
			System.Diagnostics.Debug.Assert(_arrItems.Length >= _count);
			if (_count > index + 1)
				Array.Copy(_arrItems, index + 1, _arrItems, index, _count - index - 1);
		}
		else
		{
			// remove the only item
			System.Diagnostics.Debug.Assert(_count == 1 && _singleItem != null);
			_singleItem = default;
		}

		_count--;

		// deallocate some memory if necessary
		PurgeCapacity();
	}

	public override String ToString()
	{
		return ToString(10);
	}

	public virtual String ToString(Int32 maxCount)
	{
		StringBuilder sb = new();

		Int32 listSize = Count;
		Int32 count = Math.Min(listSize, maxCount);
		for (Int32 index = 0; index < count; index++)
		{
			if (index > 0)
				sb.Append(", ");

			T value = this[index];
			if (value != null)
				sb.Append(value.ToString());
			else
				sb.Append('?');
		}
		if (listSize > maxCount)
			sb.Append(", ...");

		return sb.ToString();
	}

	public virtual Boolean Equals(IndigentList<T>? list)
	{
		if (list == null || list.Count != Count)
			return false;

		Int32 count = Count;
		for (Int32 index = 0; index < count; index++)
		{
			if (!EqualityComparer<T>.Default.Equals(list[index], this[index]))
				return false;
		}

		return true;
	}

	public override Boolean Equals(Object? obj)
	{
		return Equals(obj as IndigentList<T>);
	}

	public override Int32 GetHashCode()
	{
		if (_count == 0)
		{
			// empty list
			return Array.Empty<T>().GetHashCode();
		}
		else if (_count == 1)
		{
			// return hash code of the only item
			System.Diagnostics.Debug.Assert(_singleItem != null);
			return _singleItem.GetHashCode();
		}

		// compute hash code for the array
		System.Diagnostics.Debug.Assert(_arrItems != null && _arrItems.Length >= 2);

		const Int32 MaxCountForHashCode = 10;

		Int32 hashCode = Count.GetHashCode();

		Int32 count = Count;
		Int32 step = (count > MaxCountForHashCode ? count / MaxCountForHashCode : 1);
		if (count > MaxCountForHashCode)
		{
			// calculate the rounded up step for iteration
			step = count / MaxCountForHashCode;
			if (step * MaxCountForHashCode != count)
				step++;
		}

		for (Int32 index = 0; index < count; index += step)
			hashCode = HashCode.Combine(hashCode, _arrItems[index]);

		return hashCode;
	}

	private void EnsureCapacity(Int32 newCapacity)
	{
		if (newCapacity <= Capacity)
			return;

		newCapacity = CalculateGrowCapacity(newCapacity);
		SetCapacity(newCapacity);
	}

	private void PurgeCapacity()
	{
		Int32 shrinkCapacity = CalculateShrinkCapacity();
		if (shrinkCapacity != -1)
			SetCapacity(shrinkCapacity);
	}

	private Int32 CalculateGrowCapacity(Int32 proposedCapacity)
	{
		Int32 currCapacity = _arrItems != null ? _arrItems.Length : 1;
		if (proposedCapacity <= currCapacity)
			return currCapacity;

		// do not allow growing more then 100 elements
		const Int32 minGrowStep = 3;
		const Int32 maxGrowStep = 100;
		const Double preferredGrowRatio = 0.5;

		// compute the preferred capacity grow step
		Int32 preferredIncrease = (Int32)Math.Ceiling(currCapacity * preferredGrowRatio);
		preferredIncrease = Math.Clamp(preferredIncrease, minGrowStep, maxGrowStep);

		// compute the capacity delta 
		Int32 delta = proposedCapacity - currCapacity;
		delta = Math.Max(preferredIncrease, delta);

		// return the new computed capacity
		return currCapacity + delta;
	}

	private Int32 CalculateShrinkCapacity()
	{
		if (_arrItems == null)
			return -1;

		// do not allow shrinking less then 10 elements
		const Int32 minShrinkStep = 10;
		if (_arrItems.Length < _count + minShrinkStep)
			return -1;

		// do not allow shrinking less then half of the array
		const Double preferredShrinkRatio = 1.5;
		if (_arrItems.Length < _count * preferredShrinkRatio)
			return -1;

		// shrink to the actual count
		return Math.Max(_count, 1);
	}

	private void SetCapacity(Int32 capacity)
	{
		if (capacity <= 0 || capacity < _count)
			throw new ArgumentException("Capacity must be a positive number greater then the count", nameof(capacity));

		if (_arrItems == null)
		{
			if (capacity > 1)
			{
				System.Diagnostics.Debug.Assert(_count <= 1);

				// allocate the array
				_arrItems = new T[capacity];

				// move the _singleItem into array
				if (_count > 0)
				{
					System.Diagnostics.Debug.Assert(_singleItem != null);
					_arrItems[0] = _singleItem;
				}
				_singleItem = default;
			}
		}
		else if (_arrItems.Length != capacity)
		{
			if (capacity <= 1)
			{
				// move the only item in array into _singleItem
				if (_count == 1)
					_singleItem = _arrItems[0];

				// reset the array
				// Array.Clear(_arrItems, 0, _count);
				_arrItems = null;
			}
			else
			{
				// reallocate the array
				Array.Resize<T>(ref _arrItems, capacity);
			}
		}
	}


	private readonly struct EmptyEnumerator : IEnumerator<T>
	{
		public T Current => throw new InvalidOperationException();
		Object IEnumerator.Current => throw new InvalidOperationException();

		public void Dispose() { }
		public Boolean MoveNext() { return false; }
		public void Reset() { }

		private static readonly EmptyEnumerator _instance = new();
		public static IEnumerator<T> GetInstance() { return _instance; }
	}

	private struct SingleItemEnumerator : IEnumerator<T>
	{
		public SingleItemEnumerator(T value)
		{
			_value = value;
			_pos = -1;
		}

		private readonly T _value;
		private Int32 _pos;

		public T Current => _value;
		Object? IEnumerator.Current => _value;

		public void Dispose()
		{
			_pos = -2;
		}
		public Boolean MoveNext()
		{
			if (_pos == -2)
			{
				throw new ObjectDisposedException(nameof(SingleItemEnumerator));
			}
			if (_pos == -1)
			{
				_pos++;
				return true;
			}
			return false;
		}
		public void Reset()
		{
			_pos = -1;
		}

		public static IEnumerator<T> GetInstance(T value) { return new SingleItemEnumerator(value); }
	}
}
