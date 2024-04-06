using System;
using System.Threading;

namespace Armat.Utils.Extensions
{
	public static class RWLockers
	{
		public static RLockerSlim CreateRLocker(this ReaderWriterLockSlim rwLock)
		{
			return new RLockerSlim(rwLock);
		}
		public static URLockerSlim CreateURLocker(this ReaderWriterLockSlim rwLock)
		{
			return new URLockerSlim(rwLock);
		}
		public static WLockerSlim CreateWLocker(this ReaderWriterLockSlim rwLock)
		{
			return new WLockerSlim(rwLock);
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This is an object scoped to the stack")]
	public struct RLockerSlim : IDisposable
	{
		public RLockerSlim(ReaderWriterLockSlim? rwLock)
		{
			Lock = rwLock;

			if (Lock != null)
				Lock.EnterReadLock();
		}
		public void Dispose()
		{
			if (Lock != null)
			{
				Lock.ExitReadLock();
				Lock = null;
			}
		}

		public ReaderWriterLockSlim? Lock { get; private set; }
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This is an object scoped to the stack")]
	public struct URLockerSlim : IDisposable
	{
		public URLockerSlim(ReaderWriterLockSlim? rwLock)
		{
			Lock = rwLock;

			if (Lock != null)
				Lock.EnterUpgradeableReadLock();
		}
		public void Dispose()
		{
			if (Lock != null)
			{
				Lock.ExitUpgradeableReadLock();
				Lock = null;
			}
		}

		public ReaderWriterLockSlim? Lock { get; private set; }
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This is an object scoped to the stack")]
	public struct WLockerSlim : IDisposable
	{
		public WLockerSlim(ReaderWriterLockSlim? rwLock)
		{
			Lock = rwLock;

			if (Lock != null)
				Lock.EnterWriteLock();
		}
		public void Dispose()
		{
			if (Lock != null)
			{
				Lock.ExitWriteLock();
				Lock = null;
			}
		}

		public ReaderWriterLockSlim? Lock { get; private set; }
	}
}
