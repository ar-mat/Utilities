# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Armat.Utils (`armat.utils` on NuGet) — a .NET 10.0 C# utility library: thread-safe counters, specialized collections, serialization helpers, and extension methods. Two projects: `Projects/Utils` (the library) and `Projects/UtilsTest` (xUnit tests).

## Commands

Always build and test through the solution file, from the repo root:

```powershell
dotnet build Solution/Armat.Utilities/Armat.Utilities.sln

dotnet test Solution/Armat.Utilities/Armat.Utilities.sln

# Single test class / method
dotnet test Solution/Armat.Utilities/Armat.Utilities.sln --filter "FullyQualifiedName~IndexedList_Indexing"
dotnet test Solution/Armat.Utilities/Armat.Utilities.sln --filter "FullyQualifiedName~IndexedList_Indexing.TestAppend"
```

**Do not build or test a `.csproj` directly.** The projects set `OutputPath` from `$(SolutionDir)`, which is undefined outside a solution build — output then resolves to the drive root (e.g. `D:\bin\Debug`) and `dotnet test` fails to find the assembly. All build output for both projects goes to `bin/<Configuration>/` at the repo root (no per-project `bin/`, no target-framework subfolder).

NuGet packaging and release publishing (scripts use relative paths — run from inside `BuildScripts/`; they override the output path with `-o`, which is why they can build the csproj directly):

```powershell
cd BuildScripts
./Pack.ps1      # dotnet build + pack → bin/Release/pack/Utils
./Publish.ps1   # dotnet build + publish + zip → bin/Release/publish
```

## Code style

`EnforceCodeStyleInBuild` is enabled — style analyzer violations can fail the build. Conventions that differ from typical C# defaults:

- BCL type names instead of C# keywords: `String`, `Int32`, `Boolean`, `Object` — not `string`, `int`, `bool`, `object`.
- `ImplicitUsings` is disabled — every file lists its `using` directives explicitly.
- Nullable reference types enabled; tabs for indentation; file-scoped namespaces.

## Architecture

The library is organized into four areas, each with its own namespace. The root namespace is `Armat.Utils`, but folders deliberately use different namespaces (IDE0130 is suppressed where needed):

- `Counters/` → `Armat.Utils` — `Counter` → `LockCounter` → `ControlledActionInvoker` form an inheritance chain: atomic counter, then a reentrant lock built on it, then deferred/blocked action invocation built on that.
- `Collections/` → `Armat.Collections` — `ConcurrentList`, `IndigentList`, `ListDictionary`, `SegmentedStringDictionary`, and the IndexedList subsystem.
- `Serialization/` → `Armat.Serialization` — `IPackable`/`IPackage` custom-serialization contracts, `ITypeLocator` for polymorphic type resolution, static `JsonSerializer`/`XmlSerializer` helpers.
- `Extensions/` → `Armat.Utils.Extensions` — extension methods, including `RWLockers` (`IDisposable` wrappers over `ReaderWriterLockSlim`) which the collections use internally for their `synchronized` mode.

### IndexedList subsystem (`Collections/IndexedList/`)

The one multi-file design in the repo: `IndexedList<T>` is a list whose items can be looked up O(1) by any number of item fields simultaneously.

- `Index.cs` defines the contracts: `IIndexBase<T>` (key-type-agnostic, lets the list hold heterogeneous indexes), `IIndex<TIndexType, T>` (dictionary-like unique index), `IMultiIndex<TIndexType, T>` (multiple items per key), and `IIndexReader`/`IndexReaderDelegate` (extract the key from an item).
- `IndexBase.cs`/`MultiIndexBase.cs` are abstract bases; `DictionaryIndex.cs`/`MultiDictionaryIndex.cs` are the hash-based implementations created via `list.CreateHashIndex(...)` / `CreateMultiHashIndex(...)`.
- `ListChangeEmitter.cs` (`IListChangeEmitter<T>`) is the glue: the list emits element change events and each index subscribes to stay consistent; it also backs `INotifyCollectionChanged`.

## Packaging notes

- The package version is `<Version>` in `Projects/Utils/Utils.csproj`; uncomment/set `_NugetVersionPostfix` for prerelease suffixes (e.g. `-beta`).
- There are two readmes: the root `README.md` (GitHub) and `Projects/Utils/Readme.md` (packed into the NuGet package). They largely duplicate each other — when documenting features, update both.
