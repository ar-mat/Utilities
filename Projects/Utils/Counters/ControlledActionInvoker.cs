using System;
using System.Threading;

namespace Armat.Utils
{
	// Provides means to control / defer action invocation to a later time - once the counter is unlocked
	public class ControlledActionInvoker : LockCounter
	{
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
		public Boolean HasPendingInvocation { get; private set; }

		// invokes an action if the counter is not locked
		// otherwise sets the HasPendingInvocation = true indicating that the an action invocation has been blocked
		// This method should be called manually after unlocking the counter if InvokesPendingActionOnUnlock = false
		public void Invoke()
		{
			if (IsLocked)
			{
				HasPendingInvocation = true;
			}
			else
			{
				HasPendingInvocation = false;
				Action();
			}
		}

		// overridden to invoke pending actions once teh Counter is unlocked
		protected override void OnUnlocked()
		{
			base.OnUnlocked();

			if (InvokesPendingActionOnUnlock && HasPendingInvocation)
			{
				HasPendingInvocation = false;
				Action();
			}
		}
	}
}
