# Armat Utilities

The document describes `Armat.Utils` .Net library usage. It represents a set of reusable utilities for .Net applications. It contains of the following classes:
- Counters
	- `Armat.Utils.Counter` class is a thread-safe counter. It also fires pre and post modification events when the counter value is changed.
	- `Armat.Utils.LockCounter` class is a thread-safe reentrant lock. It could be used to block certain operations while the program is running a job. It also fires events when the counter is locked or unlocked.
	- `Armat.Utils.ControlledActionInvoker` is derived from a `LockCounter` and allows blocking method invocations while the counter is locked. If configured accordingly, method invocation triggers upon unlocking the counter.
- Collections
	- `Armat.Collections.ConcurrentList` is an implementation of list with thread safety in mind.
	- `Armat.Collections.IndigentList` is an implementation of list which mostly contains less then 2 elements. It avoids allocation of arrays wherever possible.
	- `Armat.Collections.ListDictionary` is an ordered dictionary implementing both - `IDictionary` and `IList` interfaces.
	- `Armat.Collections.SegmentedStringDictionary` is an implementation of a dictionary with multiple segments of keys. Interface `ISegmentedStringDictionary` defines the interface to access dictionary elements by segment keys.
	- `Armat.Collections.IndexedList` represents a list of elements which can be indexed by any field(s). It could be used as in-memory table of rows indexed by different columns. There are several indexing methods like hash-tables or binary trees.
- Extensions
	- Extension of `Byte[]` to compare, copy and perform bitwise operations on byte arrays. See `Armat.Utils.Extensions.ByteArray` class for details.
	- Extensions of `IDictionary<Key,Value>`, `IReadOnlyDictionary<Key,Value>`, `IReadOnlyCollection<T>` and `IEnumerable<T>` to compare contents of collections. See `Armat.Utils.Extensions.ContentComparer` class for details.
	- Extensions of `Exception` and `AggregateException` classes to retrieve inner exception(s) of a given type. See `Armat.Utils.Extensions.ExceptionHelpers` class for details.
	- Extension of `ReaderWriterLockSlim` to create a disposable *ReadLocker*, *UpgradableReadLocker* or *WriteLocker* objects - to acquire a lock in a given scope.

More utilities will come later...