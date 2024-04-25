using System;
using System.Threading;

namespace Armat.Utils;

// A thread-safe locks counter. There are events notifying of locking and unlocking the underlying counter.
// The Counter is considered to be locked if it's Value > 0, and unlocked otherwise.
[Serializable]
public class LockCounter
{
	// internal counter
	private readonly Counter _counter;

	public LockCounter()
	{
		_counter = new InternalCounter(this);
	}
	public LockCounter(Int32 numberOfLocks)
	{
		_counter = new InternalCounter(this, numberOfLocks);
	}

	// creates a locker object which
	// locks the Counter upon Construction and unlocks the Counter upon Disposal
	// use this method to automatically lock / unlock the counter a scope of a function
	public LockCounterLocker CreateLocker()
	{
		return new LockCounterLocker(this);
	}

	// Locks and returns the new lock counter
	public Int32 Lock()
	{
		return (Int32)_counter.Increment(1);
	}

	// Locks and returns the new lock counter
	public Int32 Lock(Int32 numberOfLocks)
	{
		return (Int32)_counter.Increment(numberOfLocks);
	}

	// Unlocks and returns the new lock counter
	public Int32 Unlock()
	{
		return (Int32)_counter.Decrement(1);
	}

	// Unlocks and returns the new lock counter
	public Int32 Unlock(Int32 numberOfUnlocks)
	{
		return (Int32)_counter.Decrement(numberOfUnlocks);
	}

	// gets / sets number of applied locks
	public Int32 LockCount
	{
		get { return _counter.Value; }
		set { _counter.Value = value; }
	}
	// checks whether the counter is locked
	public Boolean IsLocked
	{
		get { return _counter.Value64 > 0; }
	}
	// checks whether the counter is unlocked
	public Boolean IsUnlocked
	{
		get { return _counter.Value64 <= 0; }
	}

	// used to trigger Locked & Unlocked events upon counter modification
	protected virtual void OnCounterModified(Int64 newValue, Int64 prevValue)
	{
		// notify of locking / unlocking
		if (prevValue > 0 && newValue <= 0)
		{
			OnUnlocked();
		}
		else if (prevValue <= 0 && newValue >= 1)
		{
			OnLocked();
		}
	}

	// is called once the Counter is locked
	// override to define own behavior in derived classes
	// ensure to call base class OnLocked method for the appropriate event to be triggered
	protected virtual void OnLocked()
	{
		Locked?.Invoke(this, EventArgs.Empty);
	}
	// is called once the Counter is unlocked
	// override to define own behavior in derived classes
	// ensure to call base class OnUnlocked method for the appropriate event to be triggered
	protected virtual void OnUnlocked()
	{
		Unlocked?.Invoke(this, EventArgs.Empty);
	}

	// event triggered after Locking the Counter
	public event EventHandler? Locked;
	// event triggered after Unlocking the Counter
	public event EventHandler? Unlocked;

	#region InternalCounter : Counter class

	// derived class from Counter makes possible overriding the OnModified method
	// instead of listening to Modified event
	private class InternalCounter : Counter
	{
		public InternalCounter(LockCounter owner) : base()
		{
			Owner = owner;
		}
		public InternalCounter(LockCounter owner, Int32 value) : base(value)
		{
			Owner = owner;
		}

		public LockCounter Owner { get; }

		protected override void OnModified(Int64 newValue, Int64 prevValue)
		{
			base.OnModified(newValue, prevValue);

			// call the appropriate LockCounter method to notify about the change
			Owner.OnCounterModified(newValue, prevValue);
		}
	}

	#endregion // InternalCounter : Counter class
}

public struct LockCounterLocker : IDisposable
{
	public LockCounterLocker(LockCounter counter)
	{
		Counter = counter;
		Counter.Lock();
	}
	public void Dispose()
	{
		Int32 wasDisposed = Interlocked.Exchange(ref _disposed, 1);
		if (wasDisposed != 0)
			throw new ObjectDisposedException(nameof(LockCounterLocker));

		Counter.Unlock();
	}

	public LockCounter Counter { get; private set; }
	private Int32 _disposed = 0;
}
