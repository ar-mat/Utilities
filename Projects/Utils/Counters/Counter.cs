using System;
using System.Collections.Generic;
using System.Threading;

namespace Armat.Utils
{
	// A thread-safe 64 bit counter class
	[Serializable]
	public class Counter : IEquatable<Counter>, IComparable<Counter>
	{
		private Int64 m_nValue = 0;

		public Counter()
		{
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

		private Int64 Adjust(Int32 amount)
		{
			Int64 prevValue = Interlocked.Read(ref m_nValue);
			if (amount == 0)
				return prevValue;

			OnModifying(prevValue + amount, prevValue);
			Int64 newValue = Interlocked.Add(ref m_nValue, amount);
			OnModified(newValue, newValue - amount);

			return newValue;
		}

		// gets / sets current value of the counter
		public Int64 Value64
		{
			get
			{
				return Interlocked.Read(ref m_nValue);
			}
			set
			{
				Int64 prevValue = Interlocked.Read(ref m_nValue);
				if (value == prevValue)
					return;

				OnModifying(prevValue, value);
				prevValue = Interlocked.Exchange(ref m_nValue, value);
				OnModified(value, prevValue);
			}
		}

		// gets / sets current value of the counter (32 bit version)
		public Int32 Value
		{
			get { return (Int32)Value64; }
			set { Value64 = value; }
		}

		// resets teh counter to 0
		public void Reset()
		{
			Value64 = 0;
		}

		// is called before the Counter value is modified
		// override to define own behavior in derived classes
		// ensure to call base class OnModifying method for the appropriate event to be triggered
		protected virtual void OnModifying(Int64 newValue, Int64 prevValue)
		{
			Modifying?.Invoke(this, new ModifyEventArgs(newValue, prevValue));
		}

		// is called after the Counter value is modified
		// override to define own behavior in derived classes
		// ensure to call base class OnModified method for the appropriate event to be triggered
		protected virtual void OnModified(Int64 newValue, Int64 prevValue)
		{
			Modified?.Invoke(this, new ModifyEventArgs(newValue, prevValue));
		}

		// Counter Modification event class.
		[Serializable]
		public class ModifyEventArgs
		{
			public ModifyEventArgs(Int64 newValue, Int64 prevValue)
			{
				NewValue = newValue;
				PrevValue = prevValue;
			}

			// new value of the counter
			public Int64 NewValue { get; }
			// previous value of the counter
			public Int64 PrevValue { get; }
		}
		//
		// Summary:
		//     Represents the method that will handle Counter modification event.
		//
		// Parameters:
		//   sender:
		//     The source of the event.
		//
		//   e:
		//     An object that contains counter modification data.
		public delegate void ModifyEventHandler(Object? sender, ModifyEventArgs e);

		// event triggered before modifying the Counter
		public event ModifyEventHandler? Modifying;
		// event triggered after modifying the Counter
		public event ModifyEventHandler? Modified;

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

		public Boolean Equals(Counter? other)
		{
			return other != null && Value64.Equals(other.Value64);
		}

		public Int32 CompareTo(Counter? other)
		{
			return other == null ? 1 : Value64.CompareTo(other.Value64);
		}
	}
}
