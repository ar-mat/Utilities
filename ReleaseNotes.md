# Armat.Utils — Release Notes

Release history of the [`armat.utils`](https://www.nuget.org/packages/armat.utils/) NuGet package
([GitHub repository](https://github.com/ar-mat/Utilities)).

> **How to maintain this file:** for every new release, add a `## Version x.y.z (date)` section
> **above** the existing ones (newest release first), using the same subsection layout —
> *Breaking changes / Fixed / Added / Changed / Security* (omit empty subsections).
> This file is packed into the NuGet package and linked from the package metadata,
> so keep it self-contained.

---

## Version 3.0.0-beta (unreleased)

Stability release based on a full source audit, plus a retarget to .NET 10. It fixes a number of
long-standing correctness and thread-safety bugs and cleans up several ambiguous or
contract-violating APIs. Several fixes change public API or observable behavior — review the
breaking changes below when upgrading. First published as the `3.0.0-beta` prerelease.

### Breaking changes

- **Retargeted to .NET 10.0** (from .NET 8.0). The package now builds a single `net10.0` target;
  projects on .NET 8 or 9 can no longer reference this package version — stay on the `2.0.x`
  line, or upgrade your project to .NET 10.
- **Index lookups return public list positions.** `IIndex<K,T>.IndexOfValue`, `IndexOfItem`, and
  `IMultiIndex<K,T>.IndexesOfValue` used to return internal storage slots that were meaningless
  (or dangerous) to callers once the list had removals or mid-list inserts; they now return the
  same external indexes the `IndexedList<T>` public API uses.
- **Vetoed list changes rethrow the original exception.** Adding or updating an item whose key
  already exists in a unique index now throws `ArgumentException` (matching the `IDictionary`
  contract) instead of wrapping it in `OperationCanceledException`.
- **`ListDictionary<TKey,TValue>`**: key-based removal is now `RemoveByKey(TKey)`
  (`IDictionary<TKey,TValue>.Remove(TKey)` remains available through the interface), so that
  `Remove(TValue)` no longer collides with it in `TKey == TValue` instantiations; `Values`
  returns a read-only view instead of the internal list; new `GetAt` / `SetAt` positional
  accessors avoid the indexer ambiguity when `TKey` is `Int32`.
- **`ContentComparer.ContentsEquals`**: the `IDictionary<K,V>` overloads were removed — calling
  them with a `Dictionary<K,V>` did not compile (ambiguous, CS0121); use the
  `IReadOnlyDictionary<K,V>` overloads, which all BCL dictionaries implement.
- **`ITypeLocator`** is now a single-method interface: `GetType(className, assemblyName)`.
- **`IndexedList<T>` / `ListDictionary<TKey,TValue>` implement `IDisposable`** (the `IndexedList`
  finalizer was removed) — dispose instances created in `synchronized` mode to release the
  internal lock.
- **`IndexedList<T>.ReIndex`** is non-generic; the old `ReIndex<TIndexType>()` / `ReIndex<TIndexType>(String)`
  overloads were removed (the type parameter was never used). Drop the type argument at call sites.
- **Collection contract fixes** that change observable behavior: `CopyTo` on `IndexedList<T>`
  and on index key/value collections throws `ArgumentException` when the target array is too
  small (previously it silently truncated); `IndigentList<T>.Insert/RemoveAt` throw
  `ArgumentOutOfRangeException` (previously `IndexOutOfRangeException`); `Counter` comparison
  operators against `Int32` compare the full 64-bit value (previously the counter value was
  truncated to 32 bits before comparing).

### Fixed

- `Counter.operator !=(Counter, Counter)` returned the **inverted** result.
- Wrong-element removal and lookups after removals or mid-list inserts, caused by the
  internal/external index mixup in `IIndex.Remove(KeyValuePair)`, value-based lookups, and
  `ListDictionary` value-based `Remove` / `IndexOf`.
- A rejected duplicate-key update no longer corrupts the unique index (the previous key used to
  be lost while the item stayed in the list, with no way to repair it).
- Index keys are captured in the transaction *Begin* phase: an index-reader delegate that throws
  now cancels the operation instead of silently desynchronizing the index from the list.
- `ExceptionHelpers.As` / `Is` honor the requested `ExceptionLookupMode`
  (`TheOnlyOne` / `First` / `Last` / …) when recursing into inner exceptions.
- `ByteArray.ContentsEquals(a, b)` returns `false` for arrays of different lengths.
- `ConcurrentList<T>.Equals(itself)` no longer throws `LockRecursionException`; the non-generic
  `IList` members follow the BCL contract instead of throwing `InvalidCastException` for
  incompatible values.
- `ControlledActionInvoker`: closed a race where an action invoked concurrently with unlocking
  could be lost; the pending-invocation flag is now interlocked and consumed exactly once.
- `JsonSerializer.ToFile` truncates existing files (previously stale bytes of a longer old file
  corrupted shorter payloads).
- `JsonSerializer`: deserializing with `JsonCommentHandling.Allow` no longer throws
  (downgraded to `Skip` for the serializer options).
- `XmlFileElementReference.SaveXmlElement`: the create path always crashed with
  `IndexOutOfRangeException`; it now creates missing files, roots, and intermediate elements.
- `XmlSerializer` serializes with the same type it records in the `TypeName` attribute, and no
  longer mutates a caller-provided `XmlWriterSettings` instance.
- `PackAll` / `UnpackAll` enumerate their source sequence exactly once.
- Enumerators throw `InvalidOperationException` when `Current` is accessed before `MoveNext()`
  or after the end of the sequence.

### Added

- `ListDictionary<TKey,TValue>.RemoveByKey`, `GetAt`, `SetAt`.
- Parameterless constructors for `HashIndex`, `TreeIndex`, `MultiHashIndex`, `MultiTreeIndex`;
  the `IndexedList<T>.CreateIndex(Type, ...)` overload now actually works with the built-in
  index types.
- This release notes document, packed into the NuGet package and referenced from its metadata.

### Changed

- Thread safety: index creation / destruction / lookup, `ReIndex`, and change-handler
  registration now honor the `synchronized` mode; multi-index query methods take read locks.
- Performance: `ByteArray` operations use span-based comparison and block copies;
  `JsonSerializer` caches `JsonSerializerOptions` instances (restoring System.Text.Json
  metadata caching); index `Keys` / `Values` views are cached instead of allocated per access.
- Documentation: security guidance for the serializers, locking/eventing behavior notes on
  `IndexedList<T>`, "do not copy" notes on the disposable locker structs, refreshed readmes.

---

## Version 2.0.3 (baseline)

Last release of the 2.x line, targeting .NET 8.0. Provided thread-safe counters
(`Counter`, `LockCounter`, `ControlledActionInvoker`), specialized collections
(`IndexedList`, `ConcurrentList`, `IndigentList`, `ListDictionary`, `SegmentedStringDictionary`),
serialization helpers (`JsonSerializer`, `XmlSerializer`, `IPackable`/`IPackage`), and extension
methods (`ByteArray`, `ContentComparer`, `ExceptionHelpers`, `RWLockers`).
This section is the baseline of this document; earlier 1.x/2.x history predates it.
