using System;
using System.Collections.Generic;
using System.Threading;

namespace Armat.Utils;

// A thread-safe 64 bit counter class
[Serializable]
public struct Counter : IEquatable<Counter>, IComparable<Counter>
{
	private Int64 m_nValue;

	public Counter()
	{
		m_nValue = 0;
	}
	public Counter(Int32 value)
	{
		m_nValue = value;
	}
	public Counter(Int64 value)
	{
		m_nValue = value;
	}

	// Increments the counter and returns the new value
	public Int64 Increment()
	{
		return Adjust(1);
	}
	// Increments the counter by the given amount and returns the new value
	public Int64 Increment(Int32 amount)
	{
		return Adjust(amount);
	}

	// Decrements the counter and returns the new value
	public Int64 Decrement()
	{
		return Adjust(-1);
	}
	// Decrements the counter by the given amount and returns the new value
	public Int64 Decrement(Int32 amount)
	{
		return Adjust(-amount);
	}

	// gets / sets current value of the counter
	public Int64 Value64
	{
		get
		{
			return Get();
		}
		set
		{
			Set(value);
		}
	}

	// gets / sets current value of the counter (32 bit version)
	public Int32 Value
	{
		get { return (Int32)Get(); }
		set { Set(value); }
	}

	public Int64 Get()
	{
		return Interlocked.Read(ref m_nValue);
	}
	// sets the given value to the counter and returns the previous one
	public Int64 Set(Int64 value)
	{
		return Interlocked.Exchange(ref m_nValue, value);
	}
	private Int64 Adjust(Int32 amount)
	{
		return Interlocked.Add(ref m_nValue, amount);
	}

	// resets the counter to 0
	public Int64 Reset()
	{
		return Set(0);
	}

	public override String ToString()
	{
		return Value64.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
	}

	public override Int32 GetHashCode()
	{
		return Value64.GetHashCode();
	}

	public override Boolean Equals(Object? obj)
	{
		if (obj == null)
			return false;

		if (obj is Counter counter)
			return Value64 == counter.Value64;
		if (obj is Int32 int32)
			return Value == int32;
		if (obj is Int64 int64)
			return Value64 == int64;

		return false;
	}

	public Boolean Equals(Counter other)
	{
		return Value64 == other.Value64;
	}

	public Int32 CompareTo(Counter other)
	{
		return Value64.CompareTo(other.Value64);
	}

	#region Comparison operators

	public static Boolean operator ==(Counter left, Counter right)
	{
		return left.Value64 == right.Value64;
	}
	public static Boolean operator ==(Int32 left, Counter right)
	{
		return left == right.Value;
	}
	public static Boolean operator ==(Int64 left, Counter right)
	{
		return left == right.Value64;
	}
	public static Boolean operator ==(Counter left, Int32 right)
	{
		return left.Value == right;
	}
	public static Boolean operator ==(Counter left, Int64 right)
	{
		return left.Value64 == right;
	}

	public static Boolean operator !=(Counter left, Counter right)
	{
		return left.Equals(right);
	}
	public static Boolean operator !=(Int32 left, Counter right)
	{
		return left != right.Value;
	}
	public static Boolean operator !=(Int64 left, Counter right)
	{
		return left != right.Value64;
	}
	public static Boolean operator !=(Counter left, Int32 right)
	{
		return left.Value != right;
	}
	public static Boolean operator !=(Counter left, Int64 right)
	{
		return left.Value64 != right;
	}

	public static Boolean operator <(Counter left, Counter right)
	{
		return left.CompareTo(right) < 0;
	}
	public static Boolean operator <(Int32 left, Counter right)
	{
		return left < right.Value;
	}
	public static Boolean operator <(Int64 left, Counter right)
	{
		return left < right.Value64;
	}
	public static Boolean operator <(Counter left, Int32 right)
	{
		return left.Value < right;
	}
	public static Boolean operator <(Counter left, Int64 right)
	{
		return left.Value64 < right;
	}

	public static Boolean operator <=(Counter left, Counter right)
	{
		return left.CompareTo(right) <= 0;
	}
	public static Boolean operator <=(Int32 left, Counter right)
	{
		return left <= right.Value;
	}
	public static Boolean operator <=(Int64 left, Counter right)
	{
		return left <= right.Value64;
	}
	public static Boolean operator <=(Counter left, Int32 right)
	{
		return left.Value <= right;
	}
	public static Boolean operator <=(Counter left, Int64 right)
	{
		return left.Value64 <= right;
	}

	public static Boolean operator >(Counter left, Counter right)
	{
		return left.CompareTo(right) > 0;
	}
	public static Boolean operator >(Int32 left, Counter right)
	{
		return left > right.Value;
	}
	public static Boolean operator >(Int64 left, Counter right)
	{
		return left > right.Value64;
	}
	public static Boolean operator >(Counter left, Int32 right)
	{
		return left.Value > right;
	}
	public static Boolean operator >(Counter left, Int64 right)
	{
		return left.Value64 > right;
	}

	public static Boolean operator >=(Counter left, Counter right)
	{
		return left.CompareTo(right) >= 0;
	}
	public static Boolean operator >=(Int32 left, Counter right)
	{
		return left >= right.Value;
	}
	public static Boolean operator >=(Int64 left, Counter right)
	{
		return left >= right.Value64;
	}
	public static Boolean operator >=(Counter left, Int32 right)
	{
		return left.Value >= right;
	}
	public static Boolean operator >=(Counter left, Int64 right)
	{
		return left.Value64 >= right;
	}

	#endregion // Comparison operators
}
