using System;
using System.Threading;

namespace Armat.Utils;

// Provides means to control / defer action invocation to a later time - once the counter is unlocked
public class ControlledActionInvoker : LockCounter
{
	// 0 = no pending invocation, 1 = pending invocation; accessed with Interlocked only
	private Int32 _pendingInvocation;

	public ControlledActionInvoker(Action action)
		: this(action, true)
	{
	}
	public ControlledActionInvoker(Action action, Boolean invokePendingActionOnUnlock)
	{
		Action = action;
		InvokesPendingActionOnUnlock = invokePendingActionOnUnlock;
	}

	// action to be invoked
	public Action Action { get; }

	// flag indicating if Action should be invoked automatically once the Counter is unlocked
	public Boolean InvokesPendingActionOnUnlock { get; private set; }

	// checks whether there are action invocations while the Counter was locked
	public Boolean HasPendingInvocation
	{
		get { return Interlocked.CompareExchange(ref _pendingInvocation, 0, 0) != 0; }
	}

	// invokes an action if the counter is not locked
	// otherwise sets the HasPendingInvocation = true indicating that an action invocation has been blocked
	// This method should be called manually after unlocking the counter if InvokesPendingActionOnUnlock = false
	public void Invoke()
	{
		if (IsUnlocked)
		{
			Interlocked.Exchange(ref _pendingInvocation, 0);
			Action();
			return;
		}

		// the counter is locked - defer the invocation
		Interlocked.Exchange(ref _pendingInvocation, 1);

		// re-check: the counter may have been unlocked between the IsUnlocked check and
		// the flag assignment above; consume the flag atomically so the action runs exactly once
		if (IsUnlocked && InvokesPendingActionOnUnlock &&
			Interlocked.Exchange(ref _pendingInvocation, 0) == 1)
		{
			Action();
		}
	}

	// overridden to invoke pending actions once the Counter is unlocked
	protected override void OnUnlocked()
	{
		base.OnUnlocked();

		if (InvokesPendingActionOnUnlock &&
			Interlocked.Exchange(ref _pendingInvocation, 0) == 1)
		{
			Action();
		}
	}
}
