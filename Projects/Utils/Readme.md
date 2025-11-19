# Armat Utilities

[![NuGet](https://img.shields.io/nuget/v/armat.utils.svg)](https://www.nuget.org/packages/armat.utils/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

**Armat.Utils** is a comprehensive .NET 8.0 library providing reusable utilities for building robust .NET applications. It includes thread-safe counters, specialized collections, serialization helpers, and useful extension methods.

## Installation

```bash
dotnet add package armat.utils
```

Or via NuGet Package Manager:
```
Install-Package armat.utils
```

## Features

### 🔢 Counters (`Armat.Utils`)

Thread-safe counter implementations for synchronization and control flow:

- **`Counter`** - A thread-safe 64-bit counter with atomic increment/decrement operations. Fires pre and post modification events when the counter value changes.

- **`LockCounter`** - A thread-safe reentrant lock counter. Blocks operations while locked (value > 0) and fires events on lock/unlock. Includes `CreateLocker()` method to automatically manage lock scope with IDisposable pattern.

- **`ControlledActionInvoker`** - Extends `LockCounter` to control method invocations. Blocks action execution while locked and optionally triggers deferred execution upon unlock.

### 📚 Collections (`Armat.Collections`)

High-performance specialized collection types:

- **`ConcurrentList<T>`** - Thread-safe list implementation using `ReaderWriterLockSlim` for efficient concurrent access. Implements `IList<T>`, `IReadOnlyList<T>`, and `IDisposable`.

- **`IndigentList<T>`** - Memory-efficient list optimized for collections with fewer than 2 elements. Avoids array allocation when possible, reducing memory overhead for small collections.

- **`ListDictionary<TKey, TValue>`** - Ordered dictionary implementing both `IDictionary<TKey, TValue>` and `IList<TValue>` interfaces. Maintains insertion order while providing O(1) key-based lookups.

- **`IndexedList<T>`** - A list that can be indexed by multiple fields of the items simultaneously. Supports various indexing strategies:
  - **`DictionaryIndex`** - Hash-based single field indexing
  - **`MultiDictionaryIndex`** - Hash-based multi-field indexing
  - Additional index types available for different access patterns

- **`SegmentedStringDictionary`** - Dictionary with hierarchical segment-based keys. Keys are grouped by segments (e.g., `Key@Segment1|Segment2`), useful for storing categorized properties and settings. Implements `ISegmentedStringDictionary` interface for segment-based access patterns.

### 💾 Serialization (`Armat.Serialization`)

Simplified serialization utilities:

- **`IPackable` / `IPackage`** - Interfaces for custom serialization. Types implement `IPackable.Pack()` to return serializable packages, and `IPackage.Unpack()` to reconstruct objects. Includes extension methods for batch packing / unpacking collections.

- **`ITypeLocator`** - Interface for type resolution during deserialization based on assembly and type names. Useful for deserializing polymorphic types across assemblies.

- **`JsonSerializer`** - Static helper class for JSON serialization / deserialization. Supports serialization to string, file and stream, and deserialization from appropriate constructs.

- **`XmlSerializer`** - Static helper class for XML serialization / deserialization. Supports serialization to string, file, stream, `XmlDocument` and `XmlElement`, and deserialization from appropriate constructs.

- **`XmlFileElementReference`** - Utility for reading / writing specific XML elements within a file at a given XPath. Useful for targeted XML element manipulation without loading entire documents.

### 🔧 Extensions (`Armat.Utils.Extensions`)

Convenient extension methods:

- **`ByteArray`** - Extensions for `byte[]`:
  - `ContentsEquals()` - Compare byte array contents with optimized 8-byte chunk processing
  - Copy and bitwise operations

- **`ContentComparer`** - Compare collection contents for equality:
  - `ContentsEquals()` for `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`, `IReadOnlyCollection<T>`, and `IEnumerable<T>`
  - Supports custom `IEqualityComparer<T>` for value comparison

- **`ExceptionHelpers`** - Navigate exception hierarchies:
  - `As<T>()` and `Is<T>()` for `Exception` and `AggregateException`
  - Configurable lookup modes: `TheOnlyOne`, `AnyMatch`, `First`, `Last`, `FirstIfAllSameType`, `LastIfAllSameType`

- **`RWLockers`** - Simplified `ReaderWriterLockSlim` usage with IDisposable lock objects:
  - `CreateRLocker()` - Read lock
  - `CreateURLocker()` - Upgradeable read lock  
  - `CreateWLocker()` - Write lock
  - Automatic lock release on dispose

## Usage Examples

### Counter Example
```csharp
using Armat.Utils;

var counter = new Counter();
long value = counter.Increment(); // Thread-safe increment
```

### LockCounter Example
```csharp
using Armat.Utils;

var lockCounter = new LockCounter();
lockCounter.Lock();
try {
    // Critical section
}
finally {
    lockCounter.Unlock();
}

// Or use disposable pattern:
using (lockCounter.CreateLocker()) {
    // Automatically locked
} // Automatically unlocked
```

### IndexedList Example
```csharp
using Armat.Collections;

var list = new IndexedList<Person>();
var nameIndex = list.CreateHashIndex("NameIndex", p => p.Name, StringComparer.OrdinalIgnoreCase);

list.Add(new Person { Name = "John", Age = 30 });
var john = nameIndex["John"]; // O(1) lookup
```

### ReaderWriterLockSlim Extensions
```csharp
using Armat.Utils.Extensions;

var rwLock = new ReaderWriterLockSlim();

// Read lock
using (rwLock.CreateRLocker()) {
    // Read operations
}

// Write lock
using (rwLock.CreateWLocker()) {
    // Write operations
}
```

## Requirements

- .NET 8.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.


## Links

- [Project Website](http://armat.am/products/utilities)
- [GitHub Repository](https://github.com/ar-mat/Utilities)
- [NuGet Package](https://www.nuget.org/packages/armat.utils/)

## Version History

### Version 2.0.1
Current release targeting .NET 8.0

---

More utilities will come later...