using System;
using System.Collections.Generic;

namespace Armat.Collections;

public delegate void InsertHandler<T>(IList<T> sender, Int32 index, T value);
public delegate void RemoveHandler<T>(IList<T> sender, Int32 index, T prevValue);
public delegate void UpdateHandler<T>(IList<T> sender, Int32 index, T value, T prevValue);
public delegate void MoveHandler<T>(IList<T> sender, Int32 indexNew, Int32 indexPrev, T value);
public delegate void ClearHandler<T>(IList<T> sender);

public interface IListChangeEmitter<T>
{
	event InsertHandler<T> Inserting;
	event InsertHandler<T> Inserted;

	event RemoveHandler<T> Removing;
	event RemoveHandler<T> Removed;

	event UpdateHandler<T> Updating;
	event UpdateHandler<T> Updated;

	event MoveHandler<T> Moving;
	event MoveHandler<T> Moved;

	event ClearHandler<T> Clearing;
	event ClearHandler<T> Cleared;

	void RegisterChangeHandler(IListChangeHandler<T> handler);
	Boolean UnregisterChangeHandler(IListChangeHandler<T> handler);
}

public interface IListChangeHandler<T>
{
	Object? OnBeginInsertValue(Int32 index, T value);
	void OnCommitInsertValue(Int32 index, T value, Object? state);
	void OnRollbackInsertValue(Int32 index, T value, Object? state);

	Object? OnBeginRemoveValue(Int32 index, T prevValue);
	void OnCommitRemoveValue(Int32 index, T prevValue, Object? state);
	void OnRollbackRemoveValue(Int32 index, T prevValue, Object? state);

	Object? OnBeginSetValue(Int32 index, T value, T prevValue);
	void OnCommitSetValue(Int32 index, T value, T prevValue, Object? state);
	void OnRollbackSetValue(Int32 index, T value, T prevValue, Object? state);

	Object? OnBeginMoveValue(Int32 indexNew, Int32 indexPrev, T value);
	void OnCommitMoveValue(Int32 indexNew, Int32 indexPrev, T value, Object? state);
	void OnRollbackMoveValue(Int32 indexNew, Int32 indexPrev, T value, Object? state);

	Object? OnBeginClear(Int32 count);
	void OnCommitClear(Int32 count, Object? state);
	void OnRollbackClear(Int32 count, Object? state);
}

public class StandardListChangeEmitter<T> : IListChangeHandler<T>
{
	public StandardListChangeEmitter()
	{
		_listChangeHandlers = new List<IListChangeHandler<T>>();
		_roChangeHandlers = _listChangeHandlers.AsReadOnly();
	}

	private readonly List<IListChangeHandler<T>> _listChangeHandlers;
	private readonly IReadOnlyCollection<IListChangeHandler<T>> _roChangeHandlers;
	public IReadOnlyCollection<IListChangeHandler<T>> ChangeHandlers { get { return _roChangeHandlers; } }
	public virtual void RegisterChangeHandler(IListChangeHandler<T> handler)
	{
		_listChangeHandlers.Add(handler);
	}
	public virtual Boolean UnregisterChangeHandler(IListChangeHandler<T> handler)
	{
		return _listChangeHandlers.Remove(handler);
	}

	public Object? OnBeginInsertValue(Int32 index, T value)
	{
		if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
			return null;

		Int32 chIndex = 0, chCount = _listChangeHandlers.Count;
		Object? result;
		Object?[]? arrResults = null;

		try
		{
			// begin the operation
			// if there's an exception thrown, the operation won't be completed
			if (chCount > 1)
			{
				result = arrResults = new Object[chCount];
				for (chIndex = 0; chIndex < chCount; chIndex++)
					arrResults[chIndex] = _listChangeHandlers[chIndex].OnBeginInsertValue(index, value);
			}
			else
			{
				result = _listChangeHandlers[0].OnBeginInsertValue(index, value);
			}
		}
		catch (Exception exc)
		{
			// roll back the ones that have succeeded
			if (chCount > 1)
			{
				for (chIndex--; chIndex >= 0; chIndex--)
				{
					try { _listChangeHandlers[chIndex].OnRollbackInsertValue(index, value, arrResults![chIndex]); }
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

	public void OnCommitInsertValue(Int32 index, T value, Object? state)
	{
		if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
			return;

		Int32 chCount = _listChangeHandlers.Count;

		// commit the transaction
		// ensure that exception from one commit doesn't affect the others
		if (chCount > 1)
		{
			Object?[] arrStates = (Object?[])state!;
			//System.Diagnostics.Debug.Assert(arrStates != null && arrStates.Length == count);

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnCommitInsertValue(index, value, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnCommitInsertValue(index, value, state); }
			catch { }
		}
	}

	public void OnRollbackInsertValue(Int32 index, T value, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnRollbackInsertValue(index, value, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnRollbackInsertValue(index, value, state); }
			catch { }
		}
	}

	public Object? OnBeginRemoveValue(Int32 index, T prevValue)
	{
		if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
			return null;

		Int32 chIndex = 0, chCount = _listChangeHandlers.Count;
		Object? result;
		Object?[]? arrResults = null;

		try
		{
			// begin the operation
			// if there's an exception thrown, the operation won't be completed
			if (chCount > 1)
			{
				result = arrResults = new Object[chCount];
				for (chIndex = 0; chIndex < chCount; chIndex++)
					arrResults[chIndex] = _listChangeHandlers[chIndex].OnBeginRemoveValue(index, prevValue);
			}
			else
			{
				result = _listChangeHandlers[0].OnBeginRemoveValue(index, prevValue);
			}
		}
		catch (Exception exc)
		{
			// roll back the ones that have succeeded
			if (chCount > 1)
			{
				for (chIndex--; chIndex >= 0; chIndex--)
				{
					try { _listChangeHandlers[chIndex].OnRollbackRemoveValue(index, prevValue, arrResults![chIndex]); }
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

	public void OnCommitRemoveValue(Int32 index, T prevValue, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnCommitRemoveValue(index, prevValue, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnCommitRemoveValue(index, prevValue, state); }
			catch { }
		}
	}

	public void OnRollbackRemoveValue(Int32 index, T prevValue, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnRollbackRemoveValue(index, prevValue, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnRollbackRemoveValue(index, prevValue, state); }
			catch { }
		}
	}

	public Object? OnBeginSetValue(Int32 index, T value, T prevValue)
	{
		if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
			return null;

		Int32 chIndex = 0, chCount = _listChangeHandlers.Count;
		Object? result;
		Object?[]? arrResults = null;

		try
		{
			// begin the operation
			// if there's an exception thrown, the operation won't be completed
			if (chCount > 1)
			{
				result = arrResults = new Object[chCount];
				for (chIndex = 0; chIndex < chCount; chIndex++)
					arrResults[chIndex] = _listChangeHandlers[chIndex].OnBeginSetValue(index, value, prevValue);
			}
			else
			{
				result = _listChangeHandlers[0].OnBeginSetValue(index, value, prevValue);
			}
		}
		catch (Exception exc)
		{
			// roll back the ones that have succeeded
			if (chCount > 1)
			{
				for (chIndex--; chIndex >= 0; chIndex--)
				{
					try { _listChangeHandlers[chIndex].OnRollbackSetValue(index, value, prevValue, arrResults![chIndex]); }
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

	public void OnCommitSetValue(Int32 index, T value, T prevValue, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnCommitSetValue(index, value, prevValue, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnCommitSetValue(index, value, prevValue, state); }
			catch { }
		}
	}

	public void OnRollbackSetValue(Int32 index, T value, T prevValue, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnRollbackSetValue(index, value, prevValue, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnRollbackSetValue(index, value, prevValue, state); }
			catch { }
		}
	}

	public Object? OnBeginMoveValue(Int32 indexNew, Int32 indexPrev, T value)
	{
		if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
			return null;

		Int32 chIndex = 0, chCount = _listChangeHandlers.Count;
		Object? result;
		Object?[]? arrResults = null;

		try
		{
			// begin the operation
			// if there's an exception thrown, the operation won't be completed
			if (chCount > 1)
			{
				result = arrResults = new Object[chCount];
				for (chIndex = 0; chIndex < chCount; chIndex++)
					arrResults[chIndex] = _listChangeHandlers[chIndex].OnBeginMoveValue(indexNew, indexPrev, value);
			}
			else
			{
				result = _listChangeHandlers[0].OnBeginMoveValue(indexNew, indexPrev, value);
			}
		}
		catch (Exception exc)
		{
			// roll back the ones that have succeeded
			if (chCount > 1)
			{
				for (chIndex--; chIndex >= 0; chIndex--)
				{
					try { _listChangeHandlers[chIndex].OnRollbackMoveValue(indexNew, indexPrev, value, arrResults![chIndex]); }
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

	public void OnCommitMoveValue(Int32 indexNew, Int32 indexPrev, T value, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnCommitMoveValue(indexNew, indexPrev, value, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnCommitMoveValue(indexNew, indexPrev, value, state); }
			catch { }
		}
	}

	public void OnRollbackMoveValue(Int32 indexNew, Int32 indexPrev, T value, Object? state)
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnRollbackMoveValue(indexNew, indexPrev, value, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnRollbackMoveValue(indexNew, indexPrev, value, state); }
			catch { }
		}
	}

	public Object? OnBeginClear(Int32 count)
	{
		if (_listChangeHandlers == null || _listChangeHandlers.Count == 0)
			return null;

		Int32 chIndex = 0, chCount = _listChangeHandlers.Count;
		Object? result;
		Object?[]? arrResults = null;

		try
		{
			// begin the operation
			// if there's an exception thrown, the operation won't be completed
			if (chCount > 1)
			{
				result = arrResults = new Object[chCount];
				for (chIndex = 0; chIndex < chCount; chIndex++)
					arrResults[chIndex] = _listChangeHandlers[chIndex].OnBeginClear(count);
			}
			else
			{
				result = _listChangeHandlers[0].OnBeginClear(count);
			}
		}
		catch (Exception exc)
		{
			// roll back the ones that have succeeded
			if (chCount > 1)
			{
				for (chIndex--; chIndex >= 0; chIndex--)
				{
					try { _listChangeHandlers[chIndex].OnRollbackClear(count, arrResults![chIndex]); }
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnCommitClear(count, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnCommitClear(count, state); }
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

			for (Int32 chIndex = 0; chIndex < chCount; chIndex++)
			{
				try { _listChangeHandlers[chIndex].OnRollbackClear(count, arrStates[chIndex]); }
				catch { }
			}
		}
		else
		{
			try { _listChangeHandlers[0].OnRollbackClear(count, state); }
			catch { }
		}
	}
}
