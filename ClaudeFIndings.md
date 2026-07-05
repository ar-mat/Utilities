# Claude Code Review Findings — Armat.Utils

Full code review of the `Projects/Utils` library, performed 2026-07-05 at commit `008605f`.
Findings **F01–F15 were confirmed by actually running them** (a repro program against a Release
build of `armat.utils.dll`; F07/F08 confirmed by compiler errors). Findings F16+ were verified by
careful code inspection only.

Line numbers refer to the files at the commit above and may drift — every finding also quotes a
unique code snippet you can search for with `grep`/IDE search.

---

## How to work with this document

Each finding contains:

- **Where** — file, member, and a searchable code snippet.
- **Severity** — Critical / High / Medium / Low. **Confidence** — Confirmed (reproduced) or High (inspection).
- **Effort** — XS (one line), S (< 1 hour), M (a few hours), L (design work).
- **Breaking?** — whether the fix changes public API or observable behavior of the published NuGet package (`armat.utils`). Breaking fixes should be batched into a major version bump.
- **Problem / Reproduction / Fix / Verify** sections. When several fixes are possible, Option A is the recommended one.

### Rules you MUST follow when fixing (project conventions)

1. Build and test **only via the solution** file, from the repo root:
   ```powershell
   dotnet build Solution/Armat.Utilities/Armat.Utilities.sln
   dotnet test  Solution/Armat.Utilities/Armat.Utilities.sln
   ```
   Never build/test a `.csproj` directly (output path breaks — see CLAUDE.md).
2. Code style (`EnforceCodeStyleInBuild` is on — violations can fail the build):
   - BCL type names, not keywords: `String`, `Int32`, `Boolean`, `Int64`, `Object` — not `string`, `int`, `bool`, `long`, `object`.
   - Tabs for indentation; file-scoped namespaces; explicit `using` directives (`ImplicitUsings` disabled); nullable reference types enabled.
3. Start by adding the regression tests from **F38** — they fail before the fixes and pass after; keep them green as you fix.
4. After all fixes: bump `<Version>` in `Projects/Utils/Utils.csproj` and update **both** readmes (`README.md` and `Projects/Utils/Readme.md`) if any public API changed.

### Suggested fix order

1. **F38** — add the regression test file (tests will initially fail; that is expected).
2. Small isolated bugs: **F01, F05, F06, F09, F10, F11, F12, F13, F14** — each is one file.
3. IndexedList core correctness: **F02, F03, F04, F15, F16** (read the *Background* section first).
4. Thread-safety / lifecycle: **F17, F18, F19, F20, F21**.
5. API-breaking cleanups (batch for a major release): **F07, F08, F22–F30**.
6. Improvements: **F31–F37**.

---

## Summary table

| ID  | Title                                                              | Severity | Confidence | Effort | Breaking? |
|-----|--------------------------------------------------------------------|----------|-----------|--------|-----------|
| F01 | `Counter.operator !=` is inverted                                  | Critical | Confirmed | XS     | Behavior (bug fix) |
| F02 | Index APIs leak internal indexes; `Remove(KeyValuePair)` removes wrong element | Critical | Confirmed | M | Behavior (bug fix) |
| F03 | `ListDictionary.Remove/IndexOf(value)` broken after any removal    | Critical | Confirmed | S (after F02) | Behavior (bug fix) |
| F04 | Rejected duplicate-key update permanently corrupts unique index    | Critical | Confirmed | S      | No |
| F05 | `XmlFileElementReference.SaveXmlElement` create path always throws | High     | Confirmed | M      | Behavior (bug fix) |
| F06 | `JsonSerializer.ToFile` does not truncate existing files           | High     | Confirmed | XS     | No |
| F07 | `ContentsEquals` uncallable on `Dictionary<K,V>` (CS0121)          | High     | Confirmed | S      | Yes |
| F08 | `ListDictionary<K,V>` ambiguous members when `TKey == TValue`      | High     | Confirmed | S      | Yes |
| F09 | `ExceptionLookupMode` semantics violated by fallback loop          | Medium   | Confirmed | S      | Behavior (bug fix) |
| F10 | `Counter` ↔ `Int32` comparison operators truncate                  | Medium   | Confirmed | S      | Behavior (bug fix) |
| F11 | `ConcurrentList.Equals(itself)` throws `LockRecursionException`    | Medium   | Confirmed | XS     | No |
| F12 | `ByteArray.ContentsEquals` ignores length difference               | Medium   | Confirmed | S      | Behavior (bug fix) |
| F13 | `IndigentList` breaks on null items (`GetHashCode` NRE, asserts)   | Medium   | Confirmed | S      | No |
| F14 | `PackAll`/`UnpackAll` enumerate the source twice                   | Medium   | Confirmed | S      | No |
| F15 | `IndexedList.CopyTo` silently truncates                            | Medium   | Confirmed | S      | Behavior (bug fix) |
| F16 | Commit-phase exceptions swallowed → silent index divergence        | High risk| High      | M      | No |
| F17 | Finalizer disposes lock; locker structs not copy/double-dispose safe | Medium | High      | S      | Yes (adds IDisposable) |
| F18 | `ControlledActionInvoker` lost-action race                         | Medium   | High      | S      | No |
| F19 | Index management APIs ignore `synchronized` mode                   | Medium   | High      | S      | No |
| F20 | Events raised under write lock; vetoes surface as `OperationCanceledException` | Medium | High | M | Yes (exception type) |
| F21 | `CreateIndex(Type, ...)` cannot create built-in index types        | Low-Med  | High      | S      | No |
| F22 | Type-name-based deserialization is unsafe on untrusted input       | Advisory | High      | S      | No (docs) |
| F23 | `XmlSerializer.ToElement` mutates the caller's `XmlWriterSettings` | Low      | High      | XS     | No |
| F24 | `XmlSerializer.ToElement` type attributes vs serializer type mismatch | Low   | High      | XS     | Behavior |
| F25 | `ConcurrentList` `IList` members throw `InvalidCastException`      | Low      | High      | S      | Behavior |
| F26 | `IndigentList` throws `IndexOutOfRangeException` instead of `ArgumentOutOfRangeException` | Low | High | XS | Behavior |
| F27 | Enumerator `Current` misbehaves before `MoveNext` / after end      | Low      | High      | S      | No |
| F28 | `ListDictionary.Values` leaks the internal mutable list            | Low      | High      | XS     | Yes |
| F29 | `ExceptionHelpers` throws `NullReferenceException` explicitly      | Low      | High      | XS     | Behavior |
| F30 | `ReIndex<TIndexType>()` type parameter is unused                   | Low      | High      | XS     | Yes |
| F31 | New `JsonSerializerOptions` per call kills STJ caching (+ `Allow` comment-handling crash) | Perf/Bug | High | S | No |
| F32 | `IndexBase.Count/Keys/Values` allocate on every access             | Perf     | High      | M      | No |
| F33 | `ToExternalIndex` is O(n)                                          | Perf     | High      | M/L    | No |
| F34 | `ByteArray` should use `Span`-based comparison/copy                | Perf     | High      | S      | No |
| F35 | Duplicated change-emitter implementations (~500 lines × 2)         | Maint    | High      | M      | No |
| F36 | Hygiene: unused usings, typos, dead code, style inconsistencies    | Trivial  | High      | S      | No |
| F37 | No CI — `.github/workflows` is empty                               | Process  | High      | S      | No |
| F38 | Regression test suite for all confirmed bugs                       | Tests    | —         | S      | No |

---

## Background: IndexedList internal vs external indexes

Required reading for F02, F03, F04, F15, F16, F19.

`IndexedList<T>` (in `Projects/Utils/Collections/IndexedList/IndexedList.cs`) stores items in a
plain `List<T> _list`. Positions in `_list` are called **internal indexes**. Positions as seen by
the public list API (`this[i]`, `RemoveAt(i)`, `Insert(i, x)`, `IndexOf`, `Count`) are called
**external indexes**.

- Initially there is no difference. On the **first `RemoveAt`** (or an `Insert` that is not an
  append), the list creates `_mask` (`List<Int32>`): `_mask[externalIndex] == internalIndex`.
  Removed slots are not deleted from `_list`; their internal indexes go to `_zombies` and get
  reused by later inserts. From then on internal ≠ external.
- `ToInternalIndex(ext)` = `_mask[ext]` (O(1)). `ToExternalIndex(int)` = `_mask.IndexOf(int)`
  (O(n), returns -1 if not present).
- Indexes (`HashIndex`, `TreeIndex`, `MultiHashIndex`, `MultiTreeIndex`) store **internal**
  indexes in their `_indexMap`, because `IndexedList.RegisterIndex` registers them with
  `useInternalIndexes: true`. They access items through `IIndexedListAccessor<T>` (`Data`
  property), whose indexer, `RemoveAt`, `ToExternalIndex` etc. all speak **internal** indexes and
  convert internally.
- Mutations run a Begin/Commit/Rollback protocol: `OnBegin*` may throw to veto the change
  (surfaced as `OperationCanceledException`); `OnCommit*`/`OnRollback*` must never fail —
  exceptions there are swallowed with `catch { }`.

**The rule every fix must respect:** anything returned to or accepted from the *public* API must
be an external index; anything stored in `_indexMap` or passed to `Data` members must be internal.

---

# Part 1 — Confirmed bugs

## F01. `Counter.operator !=` is inverted

- **Where:** `Projects/Utils/Counters/Counter.cs`, ~line 146. Search for:
  ```csharp
  public static Boolean operator !=(Counter left, Counter right)
  {
      return left.Equals(right);
  }
  ```
- **Severity:** Critical. **Confidence:** Confirmed. **Effort:** XS. **Breaking?:** bug fix.
- **Problem:** The operator returns `Equals`, so `a != b` is `true` when the counters are **equal**
  and `false` when they differ. Exactly backwards.
- **Reproduction:** `new Counter(5) != new Counter(5)` → `true`; `new Counter(5) != new Counter(7)` → `false`.
- **Fix:**
  ```csharp
  public static Boolean operator !=(Counter left, Counter right)
  {
      return !left.Equals(right);
  }
  ```
- **Verify:** regression test `Counter_InequalityOperator` (F38).

## F02. Index APIs leak internal indexes; `IIndex.Remove(KeyValuePair)` removes the wrong element

- **Where:** `Projects/Utils/Collections/IndexedList/IndexBase.cs` and `MultiIndexBase.cs`.
  The offenders:
  1. `IndexBase.Remove(KeyValuePair<TIndexType, T> item)` (~line 631) — search for:
     ```csharp
     Int32 index = IndexOfItem(item);
     if (index == -1)
         return false;

     Owner.RemoveAt(index);
     ```
     `IndexOfItem` returns an **internal** index; `Owner.RemoveAt` expects an **external** one.
  2. `IndexBase.IndexOfValue(T value)` (~line 156) and `IndexBase.IndexOfItem(...)` (~line 163) —
     public methods returning internal indexes.
  3. `MultiIndexBase.IndexesOfValue(T value)` (~line 54) and `MultiIndexBase.IndexesOfItem(...)`
     (~line 61) — public, return internal indexes.
  Note the *correct* pattern already exists in the same files: `IndexBase.IndexOf(TIndexType key)`
  (~line 211) converts via `Data.ToExternalIndex(internalIndex)`, and `MultiIndexBase.IndexesOf`
  (~line 86) converts each element and skips `-1`.
- **Severity:** Critical (silent removal of the wrong element). **Confidence:** Confirmed.
  **Effort:** M. **Breaking?:** behavior bug fix (return values change once a mask exists).
- **Reproduction (ran and confirmed):**
  ```csharp
  var list = new IndexedList<String>();
  var idx = list.CreateHashIndex("k", s => s[..1]);
  list.Add("a1"); list.Add("b1"); list.Add("c1");
  list.Insert(0, "d1");                                    // creates mask [3,0,1,2]
  idx.Remove(new KeyValuePair<String, String>("b", "b1")); // returns true...
  // ...but "a1" was removed and "b1" is still in the list.
  ```
- **Fix (Option A — recommended, full consistency):** make every public index method speak
  external indexes; keep internal indexes private.
  1. In `IndexBase`, extract the current body of `IndexOfItem` into a private helper that returns
     the internal index:
     ```csharp
     private Int32 IndexOfItemInternal(KeyValuePair<TIndexType, T> item)
     {
         // find the index by key
         Int32 index = IndexOfKey(item.Key);
         if (index == -1)
             return -1;

         // check whether values are equal
         if (Owner.ValueComparer.Equals(Data[index], item.Value))
             return index;    // item is found

         return -1;
     }
     ```
     Then:
     ```csharp
     public virtual Int32 IndexOfItem(KeyValuePair<TIndexType, T> item)
     {
         Int32 internalIndex = IndexOfItemInternal(item);
         if (internalIndex == -1)
             return -1;

         // this is a public method, must return an external index
         return Data.ToExternalIndex(internalIndex);
     }
     ```
     `IndexOfValue` already delegates to `IndexOfItem`, so it becomes correct automatically.
  2. Fix `Remove(KeyValuePair<TIndexType, T>)` to use the internal helper and the internal-index
     `Data.RemoveAt` (which converts itself — same as `Remove(TIndexType key)` at ~line 577 does):
     ```csharp
     public Boolean Remove(KeyValuePair<TIndexType, T> item)
     {
         using var urLock = Owner.CreateUpgradableReadLock();

         Int32 index = IndexOfItemInternal(item);
         if (index == -1)
             return false;

         Data.RemoveAt(index);
         return true;
     }
     ```
  3. `Contains(KeyValuePair...)` should call `IndexOfItemInternal` (boolean result — conversion
     is wasted work).
  4. In `MultiIndexBase`: `IndexOfItem` is overridden — apply the same split (`IndexesOfKey`-based
     body goes to an internal helper; the public override converts). `IndexesOfItem` /
     `IndexesOfValue` must convert every element exactly like `IndexesOf` does:
     ```csharp
     IndigentList<Int32> result = new();
     foreach (Int32 index in listIndexes)
     {
         if (Owner.ValueComparer.Equals(Data[index], item.Value))
         {
             Int32 externalIndex = Data.ToExternalIndex(index);
             if (externalIndex != -1)
                 result.Add(externalIndex);
         }
     }
     ```
- **Fix (Option B — minimal patch, only stops wrong-element removal):** change just
  `Owner.RemoveAt(index)` → `Data.RemoveAt(index)` in `IndexBase.Remove(KeyValuePair...)`.
  This leaves `IndexOfValue`/`IndexOfItem`/`IndexesOfValue` returning useless internal indexes and
  leaves F03 broken — Option A is strongly preferred.
- **Verify:** regression tests `Index_RemoveKeyValuePair_RemovesCorrectElement` and
  `ListDictionary_RemoveByValue…` (F38). Also run the full existing suite.

## F03. `ListDictionary.Remove(value)` / `IndexOf(value)` broken after any removal

- **Where:** `Projects/Utils/Collections/ListDictionary.cs` — search for:
  ```csharp
  public Int32 IndexOf(TValue item)
  {
      return _index.IndexOfValue(item);
  }
  ```
  and the `Remove(TValue item)` / `Remove(KeyValuePair<TKey, TValue> item)` methods that call
  `IndexOf(...)` then `RemoveAt(index)`.
- **Severity:** Critical. **Confidence:** Confirmed. **Effort:** S (falls out of F02 Option A).
- **Problem:** `_index.IndexOfValue`/`IndexOfItem` return **internal** indexes (F02);
  `RemoveAt` expects **external**. After the first successful removal the dictionary can no longer
  remove some values.
- **Reproduction (ran and confirmed):**
  ```csharp
  var ld = new ListDictionary<Char, String>(s => s[0], EqualityComparer<Char>.Default);
  ld.Add("a1"); ld.Add("b1"); ld.Add("c1");
  ld.Remove("b1");   // OK
  ld.Remove("c1");   // throws ArgumentOutOfRangeException although "c1" is present
  ```
- **Fix:** apply F02 Option A. `ListDictionary.IndexOf` then returns external indexes and
  `Remove`/`RemoveAt` compose correctly — no change needed in `ListDictionary` itself.
  If F02 Option B was chosen instead, rewrite `ListDictionary.Remove(TValue)` and
  `Remove(KeyValuePair...)` to delegate to `_index.Remove(...)` and rewrite `IndexOf(TValue)` /
  `IndexOf(KeyValuePair...)` to convert (not recommended).
- **Verify:** regression test `ListDictionary_RemoveByValue_AfterEarlierRemoval` (F38).

## F04. Rejected duplicate-key update permanently corrupts a unique index

- **Where:** `Projects/Utils/Collections/IndexedList/DictionaryIndex.cs`, `OnBeginSetValue`
  (~line 139). Search for:
  ```csharp
  if (!KeyComparer.Equals(prevKey, key))
  {
      if (_indexMap.TryGetValue(prevKey, out prevIndex))
          _indexMap.Remove(prevKey);
      _indexMap.Add(key, index);
  }
  ```
- **Severity:** Critical (silent index corruption). **Confidence:** Confirmed. **Effort:** S.
- **Problem:** When the new key already exists in the unique index, `_indexMap.Add(key, index)`
  throws `ArgumentException` — but `prevKey` has **already been removed**. The change-emitter
  protocol only rolls back handlers that *succeeded* their Begin phase (see the commented-out
  block "There's nothing to roll back if the only begin has failed" in
  `IndexedList.cs`/`ListChangeEmitter.cs`), so the failing handler's partial mutation is never
  undone. The list update is correctly cancelled, but the index has lost `prevKey` forever.
  `ReIndex()` cannot repair it because `RecomputeIndex` iterates the (now incomplete) old map.
- **Reproduction (ran and confirmed):**
  ```csharp
  var list = new IndexedList<String>();
  var idx = list.CreateHashIndex("k", s => s[..1]);
  list.Add("a1"); list.Add("b1");
  try { list[0] = "b9"; } catch (OperationCanceledException) { }  // update correctly rejected
  // list[0] is still "a1", BUT idx.ContainsKey("a") is now false — entry lost.
  ```
- **Fix:** validate **before** mutating, so a failing Begin leaves no partial state:
  ```csharp
  public override Object? OnBeginSetValue(Int32 index, T value, T prevValue)
  {
      // set the new key
      TIndexType key = IndexReader.GetIndexValue(value);
      TIndexType prevKey = IndexReader.GetIndexValue(prevValue);

      Int32 prevIndex = -1;
      if (!KeyComparer.Equals(prevKey, key))
      {
          // validate BEFORE mutating - a duplicate key must not leave the index half-updated
          if (_indexMap.ContainsKey(key))
              throw new ArgumentException($"An item with the same key already exists in index '{Id}'", nameof(value));

          if (_indexMap.TryGetValue(prevKey, out prevIndex))
              _indexMap.Remove(prevKey);
          _indexMap.Add(key, index);
      }

      return new Tuple<TIndexType, TIndexType, Int32>(key, prevKey, prevIndex);
  }
  ```
  Review `MultiDictionaryIndex.OnBeginSetValue` too: multi-indexes allow duplicates so no
  `ContainsKey` guard is needed there, but keep the "validate/allocate before mutate" principle
  in mind (its only post-mutation failure mode is OOM).
- **Verify:** regression test `HashIndex_RejectedDuplicateUpdate_KeepsIndexIntact` (F38).

## F05. `XmlFileElementReference.SaveXmlElement` create path always throws

- **Where:** `Projects/Utils/Serialization/XmlFileElementReference.cs`, ~line 101. Search for:
  ```csharp
  if (xmlElement != null && xmlElement.Name != xmlLevels[xmlLevels.Length])
  ```
- **Severity:** High — the "create element" half of the class has never worked.
  **Confidence:** Confirmed. **Effort:** M. **Breaking?:** bug fix.
- **Problem (three defects):**
  1. `xmlLevels[xmlLevels.Length]` is out of bounds **by definition** → guaranteed
     `IndexOutOfRangeException` whenever the element doesn't already exist in the document.
  2. Line ~97 `xmlDoc.DocumentElement!` null-forgives — it *is* null for a new/empty file → NRE.
  3. `ElementPath.Split('/')` keeps the empty first entry of paths like `"/root/b"`, so the level
     walk operates on wrong names.
- **Reproduction (ran and confirmed):** file containing `<root><a>1</a></root>`, then
  `new XmlFileElementReference(path, "/root/b").SaveXmlElement(element_b)` →
  `IndexOutOfRangeException`.
- **Fix:** replace the whole `SaveXmlElement` method with:
  ```csharp
  public void SaveXmlElement(XmlElement? xmlElement)
  {
      XmlDocument xmlDoc = new();

      // load the document if exists
      if (System.IO.File.Exists(FilePath))
          xmlDoc.Load(FilePath);

      // path levels; ignore empty entries caused by a leading '/'
      String[] xmlLevels = ElementPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
      if (xmlLevels.Length == 0)
          throw new FormatException("ElementPath is empty");

      // check whether the given element name matches the last XPath level
      if (xmlElement != null && xmlElement.Name != xmlLevels[^1])
          throw new FormatException("Xml Element name does not match the XPath");

      Boolean isDocChanged = false;

      // remove the existing element if found
      XmlNode? xmlElementCurrent = xmlDoc.SelectSingleNode(ElementPath);
      if (xmlElementCurrent != null && xmlElementCurrent.ParentNode != null)
      {
          xmlElementCurrent.ParentNode.RemoveChild(xmlElementCurrent);
          isDocChanged = true;
      }

      if (xmlElement != null)
      {
          // find / create the parent node to attach the element to
          XmlNode xmlElementParent;

          if (xmlLevels.Length == 1)
          {
              // the element is the document root
              xmlElementParent = xmlDoc;
          }
          else
          {
              // ensure the root element exists and matches the first path level
              XmlElement? xmlCursor = xmlDoc.DocumentElement;
              if (xmlCursor == null)
              {
                  xmlCursor = xmlDoc.CreateElement(xmlLevels[0]);
                  xmlDoc.AppendChild(xmlCursor);
                  isDocChanged = true;
              }
              else if (xmlCursor.Name != xmlLevels[0])
              {
                  throw new FormatException("Xml document root does not match the XPath");
              }

              // walk / create intermediate levels (excluding the root and the element itself)
              for (Int32 level = 1; level < xmlLevels.Length - 1; level++)
              {
                  XmlNode? xmlNext = xmlCursor.SelectSingleNode(xmlLevels[level]);
                  if (xmlNext == null)
                  {
                      xmlNext = xmlDoc.CreateElement(xmlLevels[level]);
                      xmlCursor.AppendChild(xmlNext);
                      isDocChanged = true;
                  }
                  xmlCursor = (XmlElement)xmlNext;
              }

              xmlElementParent = xmlCursor;
          }

          // include the given element under the parent one
          xmlElement = (XmlElement)xmlDoc.ImportNode(xmlElement, true);
          xmlElementParent.AppendChild(xmlElement);
          isDocChanged = true;
      }

      // create / update the file
      if (isDocChanged)
          xmlDoc.Save(FilePath);
  }
  ```
  Notes: replacing the document root (`xmlLevels.Length == 1`) works because the old root was
  removed above; appending a second root when the existing root has a different name will throw
  from `XmlDocument` itself, which is acceptable.
- **Verify:** regression test `XmlFileElementReference_SaveNewElement_Succeeds` (F38); also test
  the "file does not exist yet" case.

## F06. `JsonSerializer.ToFile` does not truncate existing files

- **Where:** `Projects/Utils/Serialization/JsonSerializer.cs`, both `ToFile` overloads (~lines
  35 and 43). Search for:
  ```csharp
  using FileStream stream = File.OpenWrite(filePath);
  ```
- **Severity:** High (silent file corruption). **Confidence:** Confirmed. **Effort:** XS.
- **Problem:** `File.OpenWrite` opens without truncation (despite the "create / reset the file"
  comment). Writing shorter JSON over a longer existing file leaves the old tail bytes behind,
  producing an unparseable file.
- **Reproduction (ran and confirmed):** serialize a long payload to a file, then a short one to
  the same path → `FromFile` throws `JsonException`.
- **Fix:** replace `File.OpenWrite(filePath)` with `File.Create(filePath)` in **both** overloads.
- **Verify:** regression test `JsonSerializer_ToFile_TruncatesExistingFile` (F38).

## F07. `ContentsEquals` is uncallable on `Dictionary<K,V>` (CS0121)

- **Where:** `Projects/Utils/Extensions/ContentComparers.cs` — the four dictionary overloads
  (~lines 8–58): two for `IDictionary<TKey, TValue>` and two for `IReadOnlyDictionary<TKey, TValue>`.
- **Severity:** High — the primary use case (`Dictionary<K,V>`) does not compile.
  **Confidence:** Confirmed (compiler). **Effort:** S. **Breaking?:** Yes.
- **Problem:** `Dictionary<K,V>` implements both interfaces; neither overload is more specific,
  so `d1.ContentsEquals(d2)` fails with `error CS0121: The call is ambiguous…`.
- **Fix (Option A — recommended):** delete the two `IDictionary<TKey, TValue>` overloads and keep
  the `IReadOnlyDictionary<TKey, TValue>` pair. All BCL dictionaries (`Dictionary`,
  `SortedDictionary`, `ConcurrentDictionary`, `ReadOnlyDictionary`) implement
  `IReadOnlyDictionary`. Callers holding a plain `IDictionary<K,V>` reference to an exotic
  implementation can enumerate-compare via the `IEnumerable` overload. Document as breaking.
- **Fix (Option B — non-breaking stopgap):** additionally add concrete-type overloads
  `ContentsEquals<TKey, TValue>(this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second, ...)`
  which win overload resolution for the common case; ambiguity remains for other dual-interface
  types.
- **Verify:** the snippet
  `new Dictionary<String, Int32>().ContentsEquals(new Dictionary<String, Int32>())` must compile
  and return `true`. (Cannot be added as a test before the fix — it doesn't compile.)

## F08. `ListDictionary<TKey,TValue>` has ambiguous members when `TKey == TValue`

- **Where:** `Projects/Utils/Collections/ListDictionary.cs` — `public Boolean Remove(TValue item)`
  and `public Boolean Remove(TKey key)`.
- **Severity:** High for such instantiations (e.g. `ListDictionary<String, String>` — every
  `Remove` call is CS0121). Same hazard: the indexers `this[Int32]` vs `this[TKey]` when
  `TKey == Int32`. **Confidence:** Confirmed (compiler). **Effort:** S. **Breaking?:** Yes.
- **Fix (recommended):** keep `Remove(TKey key)` public; make the value-based removal an explicit
  interface implementation plus a distinctly-named public method:
  ```csharp
  Boolean ICollection<TValue>.Remove(TValue item)
  {
      return RemoveValue(item);
  }

  public Boolean RemoveValue(TValue item)
  {
      Int32 index = IndexOf(item);
      if (index == -1)
          return false;

      RemoveAt(index);
      return true;
  }
  ```
  For the indexer collision, either document it as a limitation or add `GetAt(Int32)` /
  `SetAt(Int32, TValue)` helpers and recommend them; do not silently change indexer meaning.
- **Verify:** `ListDictionary<String, String>` with `Remove("x")` (key) and `RemoveValue("x1")`
  must both compile.

## F09. `ExceptionLookupMode` semantics violated by the fallback loop

- **Where:** `Projects/Utils/Extensions/ExceptionHelpers.cs`, `As<T>(this AggregateException exc,
  ExceptionLookupMode elm)` (~line 88). Search for:
  ```csharp
  // lookup for deeper level exceptions in all inner exceptions
  foreach (Exception innerExc in innerExceptions)
  ```
- **Severity:** Medium. **Confidence:** Confirmed. **Effort:** S. **Breaking?:** bug fix.
- **Problem:** After `LookupException` correctly honors the mode, the fallback loop recurses
  `As<T>` into **every** inner exception; the recursion matches `exc is T` unconditionally. So
  `TheOnlyOne` (documented: "result is null if there are multiple exceptions") returns the first
  of two matches; `First`/`Last`/`*IfAllSameType` are similarly bypassed.
- **Reproduction (ran and confirmed):**
  `new AggregateException(ioe1, ioe2).As<InvalidOperationException>(ExceptionLookupMode.TheOnlyOne)`
  returns `ioe1` instead of `null`.
- **Fix:** make the deep recursion respect the mode:
  ```csharp
  // will return the inner exception based on exception lookup options
  T? result = LookupException<T>(innerExceptions, elm);
  if (result != null)
      return result;

  // lookup for deeper level exceptions honoring the lookup mode
  switch (elm)
  {
      case ExceptionLookupMode.AnyMatch:
          foreach (Exception innerExc in innerExceptions)
          {
              result = As<T>(innerExc, elm);
              if (result != null)
                  break;
          }
          break;

      case ExceptionLookupMode.TheOnlyOne:
          if (innerExceptions.Count == 1)
              result = As<T>(innerExceptions[0], elm);
          break;

      case ExceptionLookupMode.First:
      case ExceptionLookupMode.FirstIfAllSameType:
          result = As<T>(innerExceptions[0], elm);
          break;

      case ExceptionLookupMode.Last:
      case ExceptionLookupMode.LastIfAllSameType:
          result = As<T>(innerExceptions[^1], elm);
          break;
  }

  return result;
  ```
- **Verify:** regression test `ExceptionHelpers_TheOnlyOne_ReturnsNullForMultiple` (F38).

## F10. `Counter` ↔ `Int32` comparison operators truncate

- **Where:** `Projects/Utils/Counters/Counter.cs`. The `Value` property casts to `Int32`
  (`(Int32)Get()`), and all ten operators taking `Int32` compare against `Value`. Search for
  `right.Value` / `left.Value` inside the operator region.
- **Severity:** Medium. **Confidence:** Confirmed. **Effort:** S. **Breaking?:** bug fix.
- **Problem:** `new Counter(0x1_0000_0005L) == 5` is `true` because the 64-bit value is truncated
  to 32 bits before comparing. Same for `!=`, `<`, `<=`, `>`, `>=` with `Int32` operands, and for
  `Equals(Object)` when passed an `Int32`.
- **Fix:** in every operator overload (and the `Equals(Object)` branch) that takes `Int32`,
  compare against `Value64` — C# implicitly widens the `Int32` side. Example:
  ```csharp
  public static Boolean operator ==(Int32 left, Counter right)
  {
      return left == right.Value64;
  }
  ```
  Apply mechanically to all ten `Int32` operators and the `obj is Int32 int32` branch of
  `Equals(Object)`. Keep the `Value` property but document that it truncates (or make it
  `checked`).
- **Verify:** regression test `Counter_Int32Comparison_DoesNotTruncate` (F38).

## F11. `ConcurrentList.Equals(itself)` throws `LockRecursionException`

- **Where:** `Projects/Utils/Collections/ConcurrentList.cs`, `Equals(ConcurrentList<T>? list)`
  (~line 468). The lock is created with `LockRecursionPolicy.NoRecursion` and `Equals` takes read
  locks on both operands.
- **Severity:** Medium. **Confidence:** Confirmed. **Effort:** XS.
- **Problem:** `list.Equals(list)` acquires the same read lock twice on one thread →
  `LockRecursionException`. Any distinct-/contains-style code comparing an instance to itself hits it.
- **Fix:** add a reference-equality early exit at the top:
  ```csharp
  public Boolean Equals(ConcurrentList<T>? list)
  {
      if (list == null)
          return false;
      if (ReferenceEquals(list, this))
          return true;
      ...
  ```
- **Verify:** regression test `ConcurrentList_EqualsSelf_DoesNotThrow` (F38).

## F12. `ByteArray.ContentsEquals` ignores length difference

- **Where:** `Projects/Utils/Extensions/ByteArray.cs`, the two-argument overload:
  ```csharp
  public static Boolean ContentsEquals(this Byte[] current, Byte[] other)
  {
      return ContentsEquals(current, 0, other, 0, current.Length);
  }
  ```
- **Severity:** Medium. **Confidence:** Confirmed. **Effort:** S. **Breaking?:** bug fix.
- **Problem:** Only `current.Length` bytes are compared, so `[1,2].ContentsEquals([1,2,3])` is
  `true`. Also inconsistent: this method returns `false` for negative indexes while the sibling
  `ContentsCopy`/`ContentsAnd`/... throw `ArgumentOutOfRangeException`.
- **Fix:**
  ```csharp
  public static Boolean ContentsEquals(this Byte[] current, Byte[] other)
  {
      if (current.Length != other.Length)
          return false;

      return ContentsEquals(current, 0, other, 0, current.Length);
  }
  ```
  Optionally (see F34) reimplement the five-argument overload as
  `current.AsSpan(thisIndex, length).SequenceEqual(other.AsSpan(otherIndex, length))` — simpler
  and faster; note that `AsSpan` throws on bad ranges where the current code returns `false`
  (behavior change — decide and document).
- **Verify:** regression test `ByteArray_ContentsEquals_DifferentLengths_NotEqual` (F38).

## F13. `IndigentList` breaks on null items

- **Where:** `Projects/Utils/Collections/IndigentList.cs`. `T` has no `notnull` constraint, yet:
  - `GetHashCode()` (~line 520): `return _singleItem.GetHashCode();` → NRE in Release when the
    single item is null.
  - Numerous `Debug.Assert(... _singleItem != null ...)` calls (indexer get ~112, CopyTo ~243/269/290,
    ToArray ~311, GetEnumerator ~326, IndexOf ~348, RemoveAt ~451, GetHashCode ~519, SetCapacity ~621)
    fire in Debug builds for perfectly legal null items.
- **Severity:** Medium. **Confidence:** Confirmed (NRE reproduced in Release). **Effort:** S.
- **Fix (recommended — support nulls):**
  1. `GetHashCode` single-item branch:
     ```csharp
     else if (_count == 1)
     {
         // return hash code of the only item (null is a valid item)
         return _singleItem != null ? _singleItem.GetHashCode() : 0;
     }
     ```
  2. Remove the `_singleItem != null` conjunct from every `Debug.Assert` listed above (keep the
     `_count`/`index` parts — `_singleItem` may legitimately be null).
  (Alternative: add `where T : notnull` — rejected: it is a general-purpose `IList<T>` and the
  BCL lists allow null.)
- **Verify:** regression test `IndigentList_SingleNullItem_Works` (F38); run tests in Debug too.

## F14. `PackAll` / `UnpackAll` enumerate the source twice

- **Where:** `Projects/Utils/Serialization/IPackable.cs`, all `PackAll`/`UnpackAll` overloads.
  Pattern to search for:
  ```csharp
  IPackage[] packages = new IPackage[packables.Count()];

  Int32 index = 0;
  foreach (IPackable element in packables)
  ```
- **Severity:** Medium. **Confidence:** Confirmed (iterator source enumerated twice). **Effort:** S.
- **Problem:** `Count()` enumerates once, `foreach` enumerates again. For non-repeatable sources
  (iterators, LINQ over live data, DB readers): side effects run twice, and if the second pass
  yields **more** items than the first, the array write throws `IndexOutOfRangeException`.
- **Fix:** enumerate once per call. Example for one overload — apply the same shape to all six:
  ```csharp
  public static IPackage[] PackAll(this IEnumerable<IPackable> packables)
  {
      List<IPackage> packages = packables is ICollection<IPackable> collection
          ? new List<IPackage>(collection.Count)
          : new List<IPackage>();

      foreach (IPackable element in packables)
          packages.Add(element.Pack());

      return packages.ToArray();
  }
  ```
- **Verify:** regression test `PackAll_EnumeratesSourceOnce` (F38).

## F15. `IndexedList.CopyTo` silently truncates

- **Where:** `Projects/Utils/Collections/IndexedList/IndexedList.cs`, `CopyTo(T[] array, Int32
  arrayIndex)` and `CopyTo(Array array, Int32 arrayIndex)`. Search for:
  ```csharp
  Int32 count = Math.Min(_list.Count, array.Length - arrayIndex);
  ```
- **Severity:** Medium (silent data loss; violates the `ICollection<T>.CopyTo` contract).
  **Confidence:** Confirmed. **Effort:** S. **Breaking?:** bug fix (now throws where it truncated).
- **Fix:** validate and throw instead of `Math.Min`:
  ```csharp
  public void CopyTo(T[] array, Int32 arrayIndex)
  {
      if (array == null)
          throw new ArgumentNullException(nameof(array));
      if (arrayIndex < 0)
          throw new ArgumentOutOfRangeException(nameof(arrayIndex));

      using var rLock = CreateReadLock();

      Int32 count = ExternalCount;
      if (array.Length - arrayIndex < count)
          throw new ArgumentException("The number of elements is greater than the available space.", nameof(array));

      // existing copy loops, but always copying exactly `count` items
      ...
  }
  ```
  Apply the same contract to `KVCollection.CopyTo`, `ValueCollection.CopyTo` (IndexBase.cs),
  `MultiKVCollection`/`MultiKeyCollection`/`MultiValueCollection.CopyTo` (MultiIndexBase.cs), and
  `IndexBase.CopyTo` — all currently return silently on null/short arrays.
  `SegmentedStringDictionaryView.CopyTo` already implements the correct pattern — copy it.
- **Verify:** regression test `IndexedList_CopyTo_ThrowsWhenArrayTooSmall` (F38). `ToArray()`
  passes an exactly-sized array and must keep working.

---

# Part 2 — Design & thread-safety issues (verified by inspection)

## F16. Commit-phase exceptions are swallowed → silent index divergence

- **Where:** all `OnCommit*`/`OnRollback*` dispatch sites wrap handler calls in `try { ... } catch { }`:
  `IndexedList.cs` (private `IndexedListChangeEmitter` + `CommitInsertValue`/`CommitRemoveValue`/…)
  and `ListChangeEmitter.cs` (`StandardListChangeEmitter`). The dangerous case is
  `DictionaryIndex.OnCommitRemoveValue` / `MultiDictionaryIndex.OnCommitRemoveValue`, which call
  the **user-supplied** `IndexReader.GetIndexValue(prevValue)` during commit:
  ```csharp
  public override void OnCommitRemoveValue(Int32 index, T prevValue, Object? state)
  {
      // remove it
      TIndexType key = IndexReader.GetIndexValue(prevValue);
      _indexMap.Remove(key);
  }
  ```
- **Severity:** High risk. **Confidence:** High (inspection). **Effort:** M.
- **Problem:** If the user's index-reader delegate throws during commit, the exception is
  swallowed and the stale key stays in the index forever — the index silently diverges from the
  list. The design rule is "commit must never fail", but commit currently runs user code.
- **Fix (recommended, two parts):**
  1. Move user-code execution out of commit: compute the key in the Begin phase (where a throw
     safely cancels the whole operation) and pass it via `state`:
     ```csharp
     public override Object? OnBeginRemoveValue(Int32 index, T prevValue)
     {
         // compute the key up front: the user-provided IndexReader may throw,
         // and Begin is the only phase where a failure can safely cancel the operation
         return IndexReader.GetIndexValue(prevValue);
     }

     public override void OnCommitRemoveValue(Int32 index, T prevValue, Object? state)
     {
         _indexMap.Remove((TIndexType)state!);
     }
     ```
     Apply to `DictionaryIndex` and `MultiDictionaryIndex` (the multi version keeps its
     list-manipulation logic in commit, but uses the precomputed key).
  2. Optionally add a diagnostics hook on `IndexedList<T>` (e.g.
     `public event EventHandler<Exception>? HandlerFailed;`) invoked from the empty `catch`
     blocks, so swallowed user-event exceptions are at least observable.
- **Verify:** new test: index with an `IndexReader` that throws on a marker item — `RemoveAt` of
  that item must now throw `OperationCanceledException` and leave list + index consistent.

## F17. Finalizer disposes the lock; locker structs aren't copy/double-dispose safe

- **Where:**
  1. `IndexedList.cs` ~line 180:
     ```csharp
     ~IndexedList()
     {
         ReaderWriterLockSlim? rwLock = Interlocked.Exchange(ref _rwLock, null);
         rwLock?.Dispose();
     }
     ```
  2. `LockCounter.cs`, `LockCounterLocker.Dispose` — throws `ObjectDisposedException` on second dispose.
  3. `RWLockers.cs` — `RLockerSlim`/`URLockerSlim`/`WLockerSlim` are mutable structs; a copy
     re-exits the lock.
- **Severity:** Medium. **Confidence:** High. **Effort:** S. **Breaking?:** adds `IDisposable`.
- **Problems:** every `IndexedList` instance pays finalization cost; disposing a
  `ReaderWriterLockSlim` possibly still held throws inside a finalizer, which terminates the
  process; `Dispose` must be idempotent per .NET guidelines (the `LockCounterLocker` throw
  violates it, and its guard doesn't survive struct copies anyway).
- **Fix:**
  1. `IndexedList<T>`: implement `IDisposable`; move the lock disposal there; delete the
     finalizer (managed-only cleanup does not need one):
     ```csharp
     public void Dispose()
     {
         ReaderWriterLockSlim? rwLock = Interlocked.Exchange(ref _rwLock, null);
         rwLock?.Dispose();
     }
     ```
     Update `ListDictionary` (which owns an `IndexedList`) to implement/forward `IDisposable` too.
  2. `LockCounterLocker.Dispose`: make the double dispose a no-op instead of throwing:
     ```csharp
     public void Dispose()
     {
         if (Interlocked.Exchange(ref _disposed, 1) == 0)
             Counter.Unlock();
     }
     ```
  3. `RWLockers` structs: keep (standard guard pattern) but add a doc comment: "do not copy;
     intended for `using` only".
- **Verify:** build + full test run; grep for `new IndexedList` usages in tests to add `using`
  where the synchronized mode is used.

## F18. `ControlledActionInvoker` lost-action race

- **Where:** `Projects/Utils/Counters/ControlledActionInvoker.cs`, `Invoke()`:
  ```csharp
  if (IsLocked)
  {
      HasPendingInvocation = true;
  }
  ```
- **Severity:** Medium (this is a concurrency utility). **Confidence:** High. **Effort:** S.
- **Problem:** Between the `IsLocked` check and setting `HasPendingInvocation`, another thread
  can unlock. `OnUnlocked` has already run and seen no pending invocation → the action is lost
  until some later unlock. `HasPendingInvocation` is also a plain non-volatile bool mutated from
  multiple threads.
- **Fix:** use an interlocked flag and re-check after setting it:
  ```csharp
  private Int32 _pendingInvocation; // 0 = no, 1 = yes; accessed with Interlocked only

  public Boolean HasPendingInvocation
  {
      get { return Interlocked.CompareExchange(ref _pendingInvocation, 0, 0) != 0; }
  }

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

      // re-check: the counter may have been unlocked between the check and the flag assignment;
      // consume the flag atomically so the action runs exactly once
      if (IsUnlocked && InvokesPendingActionOnUnlock &&
          Interlocked.Exchange(ref _pendingInvocation, 0) == 1)
      {
          Action();
      }
  }

  protected override void OnUnlocked()
  {
      base.OnUnlocked();

      if (InvokesPendingActionOnUnlock &&
          Interlocked.Exchange(ref _pendingInvocation, 0) == 1)
      {
          Action();
      }
  }
  ```
  (Also fixes the "teh" typo in the comment above `OnUnlocked` while there — see F36.)
- **Verify:** existing tests still pass; optionally add a stress test (lock on thread A, spam
  `Invoke` on thread B, unlock — the action must run exactly once).

## F19. Index management APIs ignore the `synchronized` mode

- **Where:** `IndexedList.cs` — `RegisterIndex`, `UnregisterIndex`, `GetIndex`, `Indexes`,
  `ReIndex<T>()`, `ReIndex<T>(String id)`, and the lazy init in
  `RegisterChangeHandler` (`_listChangeEmitter ??= new IndexedListChangeEmitter();`).
- **Severity:** Medium. **Confidence:** High. **Effort:** S.
- **Problem:** These methods read/mutate `_mapIndexesByName`, `_listChangeEmitter`, `_list`, and
  `_mask` without taking the read/write lock, even when the list was created with
  `synchronized: true`. `RegisterIndex` iterates the whole list to populate a new index —
  concurrent writers corrupt the new index.
- **Fix:** take the appropriate lock at the top of each member (the lock supports recursion, so
  nested locking from inner calls is fine):
  - `RegisterIndex`, `UnregisterIndex`, `RegisterChangeHandler`, `UnregisterChangeHandler`,
    `ReIndex*`: `using var wLock = CreateWriteLock();`
  - `GetIndex(String)`, `Indexes` getter: `using var rLock = CreateReadLock();` (have `Indexes`
    return a snapshot array instead of the live `Values` collection).
- **Verify:** build + tests; optionally a stress test creating an index while another thread inserts.

## F20. Events raised under the write lock; vetoes surface as `OperationCanceledException`

- **Where:** `IndexedList.cs` — `CommitInsertValue`/`CommitRemoveValue`/… invoke user event
  handlers (`Inserted`, `CollectionChanged`, …) inside the Begin/Commit protocol, which runs
  while the upgradeable-read + write locks are held. Vetoing handler exceptions are wrapped:
  `throw new OperationCanceledException("Insertion of value has been canceled", exc);`.
- **Severity:** Medium. **Confidence:** High. **Effort:** M. **Breaking?:** exception-type change.
- **Problems:**
  1. User handlers run under the list's write lock → deadlock bait (a handler that touches the
     list from another thread, or blocks on one, deadlocks) — at minimum this must be documented.
  2. A duplicate key in a unique index surfaces from `Add`/`Insert`/`this[i] = x` as
     `OperationCanceledException` (wrapping `ArgumentException`). `IDictionary<K,V>.Add` is
     contractually supposed to throw `ArgumentException` for duplicate keys, and OCE has special
     meaning in `Task`-based code (treated as cancellation).
- **Fix (incremental):**
  1. Documentation first: XML-doc `Inserting/Inserted/...` events — "raised while the internal
     write lock is held; handlers must not block on other threads accessing this list".
  2. Preserve the original exception instead of wrapping: in the emitter `catch (Exception exc)`
     blocks, after rolling back succeeded handlers, rethrow with
     `System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exc).Throw();` instead of
     wrapping in `OperationCanceledException`. Callers then see `ArgumentException` for duplicate
     keys (matches `IDictionary` contract). Mark as breaking; update tests that catch OCE.
  3. (Larger, optional) Restructure commit notifications to fire after the locks are released by
     collecting pending notifications in the operation and dispatching after the `using` scopes.
- **Verify:** update/extend tests around duplicate-key behavior (`Serialization`/`IndexedList`
  tests catch `OperationCanceledException` today — adjust accordingly).

## F21. `CreateIndex(Type, ...)` cannot create the built-in index types

- **Where:** `IndexedList.cs`, ~line 1812:
  ```csharp
  Object objInstance = indexClassType.Assembly.CreateInstance(indexClassType.FullName)
  ```
- **Severity:** Low-Medium (dead API path). **Confidence:** High. **Effort:** S.
- **Problem:** `Assembly.CreateInstance` requires a parameterless constructor and resolves the
  type by name string — `HashIndex`/`TreeIndex`/`MultiHashIndex`/`MultiTreeIndex` only have
  comparer-taking constructors, and constructed generic type names resolve poorly. The overload
  cannot instantiate any built-in index.
- **Fix:**
  1. Use `Activator.CreateInstance(indexClassType)` (works with an already-constructed generic
     `Type` object):
     ```csharp
     Object objInstance = Activator.CreateInstance(indexClassType)
         ?? throw new ArgumentException($"Creation of an index of type {indexClassType} failed");
     ```
  2. Add parameterless constructors to the four index classes, e.g.
     `public HashIndex() : this(null) { }` (same pattern for `TreeIndex`, `MultiHashIndex`,
     `MultiTreeIndex`).
- **Verify:** new test: `list.CreateIndex(typeof(HashIndex<String, String>), "byName", reader)`
  returns a working index.

## F22. Type-name-based deserialization is unsafe on untrusted input (advisory)

- **Where:** `JsonSerializer.FromDocument` / `XmlSerializer.FromElement` read `Assembly` +
  `TypeName` from the payload and instantiate whatever type resolves.
  `DefaultTypeLocator.GetType(String className)` (`ITypeLocator.cs`) calls
  `Type.GetType(className, false)`, which **can load assemblies** when given an
  assembly-qualified name.
- **Severity:** Advisory (design-level security note). **Confidence:** High. **Effort:** S (docs + helper).
- **Problem:** Letting serialized data choose the deserialized type is the classic unsafe
  deserialization pattern. `System.Text.Json`/`XmlSerializer` are less gadget-rich than
  `BinaryFormatter`, but attacker-chosen types with side-effecting constructors/setters remain a
  real risk.
- **Fix:**
  1. Add XML-doc + readme warnings: "never deserialize untrusted input with the default type
     locator".
  2. Ship an allow-list locator and recommend it:
     ```csharp
     public sealed class AllowListTypeLocator : ITypeLocator
     {
         private readonly Dictionary<String, Type> _allowedTypes;

         public AllowListTypeLocator(IEnumerable<Type> allowedTypes)
         {
             _allowedTypes = allowedTypes.ToDictionary(t => t.FullName!, t => t);
         }

         public Type? GetType(String className)
         {
             return _allowedTypes.GetValueOrDefault(className);
         }

         public Type? GetType(String className, String assemblyName)
         {
             return GetType(className);
         }
     }
     ```
  3. In `DefaultTypeLocator.GetType(String className)`, consider resolving only against
     already-loaded assemblies (like the two-argument overload does) instead of `Type.GetType`.

---

# Part 3 — Conformance & minor API issues

## F23. `XmlSerializer.ToElement` mutates the caller's `XmlWriterSettings`

- **Where:** `XmlSerializer.cs`: `settings ??= new XmlWriterSettings(); settings.OmitXmlDeclaration = true;`
- **Fix (XS):** don't touch the caller's object:
  ```csharp
  settings = settings != null ? settings.Clone() : new XmlWriterSettings();
  settings.OmitXmlDeclaration = true;
  ```

## F24. `XmlSerializer.ToElement` writes `objectType` attributes but serializes `data.GetType()`

- **Where:** `XmlSerializer.cs`: attributes are written from the `objectType` parameter, but
  `new System.Xml.Serialization.XmlSerializer(data.GetType())` uses the runtime type. If a caller
  passes a base `objectType` for a derived instance, the payload and the recorded type disagree
  and round-tripping fails.
- **Fix (XS):** use `objectType` consistently: `new System.Xml.Serialization.XmlSerializer(objectType)`.
  (The generic `ToElement<T>` path already passes `data.GetType()` as `objectType`, so it is unaffected.)

## F25. `ConcurrentList` non-generic `IList` members throw `InvalidCastException`

- **Where:** `ConcurrentList.cs` — `Contains(Object?)`, `IndexOf(Object?)`, `Remove(Object?)`
  throw `InvalidCastException` for values that are not `T`.
- **Problem:** the non-generic `IList` contract expects `Contains(incompatible)` → `false`,
  `IndexOf(incompatible)` → `-1`, `Remove(incompatible)` → no-op. `IndigentList` and
  `IndexedList` already do this correctly — mirror them.
- **Fix (S):** replace the `else throw new InvalidCastException();` branches with `return false;`
  / `return -1;` / no-op respectively (keep the cast-throw for `Add`/`Insert`/indexer setter,
  where the BCL does throw).

## F26. `IndigentList` throws `IndexOutOfRangeException` instead of `ArgumentOutOfRangeException`

- **Where:** `IndigentList.cs` — `Insert` (~line 362), `RemoveAt` (~line 438), and
  `CopyTo(Int32, T[], Int32, Int32)` (~line 267) throw `IndexOutOfRangeException`.
- **Fix (XS):** throw `ArgumentOutOfRangeException(nameof(index))` — matches `List<T>` and this
  class's own indexer.

## F27. Enumerators misbehave on `Current` before `MoveNext` / after the end

- **Where:** `ConcurrentList.ConcurrentListEnumerator.Current` (no `_index < 0` check → indexes
  `_list[-1]`), `IndexedList.IndexedListEnumerator.Current` (same), `MoveNext` after end keeps
  incrementing.
- **Fix (S):** in both `Current` getters, throw `InvalidOperationException` when `_index < 0` or
  `_index >= Count` (BCL contract), keeping the `ObjectDisposedException` for the disposed state.

## F28. `ListDictionary.Values` leaks the internal mutable list

- **Where:** `ListDictionary.cs`: `public ICollection<TValue> Values => _list;`
- **Problem:** callers can mutate the dictionary through `Values` (add/remove items bypassing
  nothing, but the property is expected to be a read-only view per `IDictionary` convention).
- **Fix (XS, breaking):**
  ```csharp
  public ICollection<TValue> Values
  {
      get { return new System.Collections.ObjectModel.ReadOnlyCollection<TValue>(_list); }
  }
  ```

## F29. `ExceptionHelpers.As` throws `NullReferenceException` explicitly

- **Where:** `ExceptionHelpers.cs`: `throw new NullReferenceException("Exception is null");` (twice).
- **Fix (XS):** `throw new ArgumentNullException(nameof(exc));` — NRE is reserved for actual null
  dereferences.

## F30. `ReIndex<TIndexType>()` type parameter is unused

- **Where:** `IndexedList.cs`: `public void ReIndex<TIndexType>()` and
  `public void ReIndex<TIndexType>(String id)` never use `TIndexType`; callers are forced to
  supply a meaningless type argument.
- **Fix (XS, breaking):** add non-generic `ReIndex()` / `ReIndex(String id)` with the same bodies;
  mark the generic ones `[Obsolete("Use the non-generic ReIndex overloads.")]`; remove next major
  version. Update `ListDictionary.Reindex()` to call the non-generic overload.

---

# Part 4 — Improvements (performance, maintainability, process)

## F31. New `JsonSerializerOptions` per call defeats System.Text.Json caching (+ latent `Allow` crash)

- **Where:** `JsonSerializer.cs`, `Convert(JsonDocumentOptions)` — called on **every**
  deserialization; each new `JsonSerializerOptions` instance re-builds STJ reflection metadata
  (the cache is per-options-instance). Also latent bug: the switch maps
  `JsonCommentHandling.Allow => JsonCommentHandling.Allow`, but the
  `JsonSerializerOptions.ReadCommentHandling` setter **rejects `Allow`** with
  `ArgumentOutOfRangeException` — so deserializing with `CommentHandling = Allow` crashes.
- **Fix (S):** cache options per distinct configuration and map `Allow` to `Skip`:
  ```csharp
  private static readonly System.Collections.Concurrent.ConcurrentDictionary<
      (Boolean AllowTrailingCommas, JsonCommentHandling CommentHandling, Int32 MaxDepth),
      JsonSerializerOptions> _serializerOptionsCache = new();

  private static JsonSerializerOptions Convert(JsonDocumentOptions documentOptions)
  {
      var key = (documentOptions.AllowTrailingCommas, documentOptions.CommentHandling, documentOptions.MaxDepth);

      return _serializerOptionsCache.GetOrAdd(key, static k => new JsonSerializerOptions
      {
          AllowTrailingCommas = k.AllowTrailingCommas,

          // JsonSerializerOptions.ReadCommentHandling only accepts Skip or Disallow;
          // Allow is a JsonDocument-only mode and must be downgraded to Skip
          ReadCommentHandling = k.CommentHandling == JsonCommentHandling.Disallow
              ? JsonCommentHandling.Disallow
              : JsonCommentHandling.Skip,

          MaxDepth = k.MaxDepth
      });
  }
  ```

## F32. `IndexBase.Count/Keys/Values` allocate a new collection per access

- **Where:** `IndexBase.cs`: `Count => Keys.Count` builds a new `KeyCollection` every call;
  `Keys`/`Values` getters likewise; same in `DictionaryIndex`/`MultiDictionaryIndex`.
- **Fix (M):** make the view collections read the index's **current** map lazily (pass the owning
  index instead of the map — important because `RecomputeIndex` replaces `_indexMap`), then cache
  one instance of each view per index. Also give `Count` a direct implementation
  (`_indexMap.Count == 0 ? 0 : Data.ExternalCount` — same semantics, no allocation).

## F33. `ToExternalIndex` is O(n)

- **Where:** `IndexedList.cs`: `ToExternalIndex` = `_mask.IndexOf(internalIndex)` — linear scan,
  called from every key lookup that returns an external index (and, after F02, from value
  lookups too). Acknowledged by the in-code TODO.
- **Fix (M/L):** maintain a reverse map alongside `_mask` (e.g. `Dictionary<Int32, Int32>` or a
  `List<Int32>` indexed by internal index, updated in the same places `_mask`/`_zombies` are).
  Straightforward but touches every mutation path — do after F02/F15 with tests green.

## F34. `ByteArray` should use `Span`-based operations

- **Where:** `ByteArray.cs` — hand-rolled 8-byte-chunk loops.
- **Fix (S):** `ContentsEquals` → `ReadOnlySpan<Byte>.SequenceEqual`; `ContentsCopy` →
  `Array.Copy`/`Span.CopyTo`; the And/Or/Xor/Not loops can use
  `System.Numerics.Vector<Byte>` or stay as-is. Simpler and faster; keep the existing
  argument-validation behavior (or align it per F12).

## F35. Duplicated change-emitter implementations

- **Where:** `IndexedList.IndexedListChangeEmitter` (private, ~600 lines) vs
  `StandardListChangeEmitter<T>` (`ListChangeEmitter.cs`, ~550 lines) — near-identical
  Begin/Commit/Rollback dispatch logic. F04/F16-class fixes must currently be applied twice.
- **Fix (M):** have `IndexedList` reuse `StandardListChangeEmitter<T>` with a thin adapter that
  picks internal vs external index per registered handler. While there, fix the copy-pasted
  exception message `"Setting of value has been canceled"` in the **Move** paths of both emitters
  (should say "Moving of value has been canceled").

## F36. Hygiene batch (safe, mechanical)

1. **Unused `using` directives** (remove, then rebuild to confirm):
   - `IndexBase.cs`: `System.Net.Http.Headers`, `System.Reflection`, `System.Runtime.CompilerServices`, `System.Threading`.
   - `IndigentList.cs`: `System.Data`, `System.Reflection`.
   - `IPackable.cs`: `System.Runtime.Serialization` (and `System.Linq` after F14 removes `Count()`).
   - `XmlFileElementReference.cs`: `System.Collections.Generic`, `System.Linq`, `System.Text`, `System.Threading.Tasks`.
   - `ITypeLocator.cs`: `System.Collections.Generic`, `System.Text`, `System.Threading.Tasks`.
   - `Counter.cs`: `System.Collections.Generic`.
   - `LockCounter.cs`: `using static Armat.Utils.Counter;` (pointless).
   - `SegmentedStringDictionary.cs`: `System.Reflection.Metadata`, `System.Runtime.InteropServices`.
2. **Typos:**
   - `ControlledActionInvoker.cs`: "once teh Counter" → "once the Counter".
   - `ISegmentedStringDictionary.cs`: exception message "Invalid directory key format" → "Invalid dictionary key format".
   - `IndigentList.cs` `SetCapacity`: "greater then the count" → "greater than the count".
   - `XmlFileElementReference.cs` header comment: "elemnt" → "element"; also "read and write and XML element" → "read and write an XML element".
   - `JsonSerializer.cs`: "seriaize" → "serialize". `ITypeLocator.cs`: "deseriaizer" → "deserializer".
3. **Style:** `XmlSerializer.cs` uses a block-scoped namespace — convert to file-scoped like every
   other file (re-indent). `XmlFileElementReference.cs` lines ~51–56 use spaces — convert to tabs.
4. **Dead code:** remove the large commented-out blocks in `IndexedList.cs` /
   `ListChangeEmitter.cs` (`//else { ... }` rollback stubs), the commented `//dataWrapper.Index = clonedIndex;`,
   and commented `Array.Clear` lines in `IndigentList.cs` — git history preserves them.
5. **Test warning:** `Projects/UtilsTest/GenericLists_Simple.cs` ~line 227 — xUnit2010: replace
   `Assert.True(a == b)` string comparison with `Assert.Equal(a, b)`.

## F37. No CI

- **Where:** `.github/workflows/` exists but is empty.
- **Fix (S):** add `.github/workflows/build-test.yml`:
  ```yaml
  name: build-test
  on: [push, pull_request]
  jobs:
    build:
      runs-on: windows-latest
      steps:
        - uses: actions/checkout@v4
        - uses: actions/setup-dotnet@v4
          with:
            dotnet-version: 8.0.x
        - run: dotnet build Solution/Armat.Utilities/Armat.Utilities.sln -c Release
        - run: dotnet test Solution/Armat.Utilities/Armat.Utilities.sln -c Release --no-build
  ```
  Note: build/test **via the solution** — building the csproj directly breaks the output path
  (see CLAUDE.md).

## F38. Regression test suite for the confirmed bugs

Add this file as `Projects/UtilsTest/RegressionTests.cs`. Every test asserts the **correct**
behavior: they FAIL on the current code and must PASS once the corresponding finding is fixed.
(F07/F08 are compile-time findings and cannot be expressed as tests before the fix.)
Note: `IndigentList_SingleNullItem_Works` ships with a `Skip` — running it before F13 is fixed
crashes the Debug test host via `Debug.Assert`/FailFast. Unskip it as part of fixing F13.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Armat.Serialization;
using Armat.Utils;
using Armat.Utils.Extensions;

using Xunit;

namespace Armat.Collections;

// Regression tests for the findings in ClaudeFIndings.md.
// Each test is tagged with the finding it guards.
public class RegressionTests
{
	// F01
	[Fact]
	public void Counter_InequalityOperator()
	{
		Counter a = new(5), b = new(5), c = new(7);

		Assert.False(a != b);
		Assert.True(a != c);
	}

	// F10
	[Fact]
	public void Counter_Int32Comparison_DoesNotTruncate()
	{
		Counter c = new(0x1_0000_0005L);

		Assert.False(c == 5);
		Assert.True(c != 5);
	}

	// F09
	[Fact]
	public void ExceptionHelpers_TheOnlyOne_ReturnsNullForMultiple()
	{
		AggregateException aggr = new(
			new InvalidOperationException("one"),
			new InvalidOperationException("two"));

		Assert.Null(aggr.As<InvalidOperationException>(ExceptionLookupMode.TheOnlyOne));
	}

	// F03 (depends on F02)
	[Fact]
	public void ListDictionary_RemoveByValue_AfterEarlierRemoval()
	{
		ListDictionary<Char, String> ld = new(s => s[0], EqualityComparer<Char>.Default);
		ld.Add("a1"); ld.Add("b1"); ld.Add("c1");

		Assert.True(ld.Remove("b1"));
		Assert.True(ld.Remove("c1"));
		Assert.True(ld.ContainsKey('a'));
		Assert.False(ld.ContainsKey('c'));

		Int32 remainingCount = ld.Count;
		Assert.Equal(1, remainingCount);
	}

	// F02
	[Fact]
	public void Index_RemoveKeyValuePair_RemovesCorrectElement()
	{
		IndexedList<String> list = new();
		var idx = list.CreateHashIndex("k", s => s[..1]);
		list.Add("a1"); list.Add("b1"); list.Add("c1");
		list.Insert(0, "d1"); // forces mask creation

		Assert.True(idx.Remove(new KeyValuePair<String, String>("b", "b1")));
		Assert.Contains("a1", list);
		Assert.DoesNotContain("b1", list);
		Assert.Equal(3, list.Count);
	}

	// F04
	[Fact]
	public void HashIndex_RejectedDuplicateUpdate_KeepsIndexIntact()
	{
		IndexedList<String> list = new();
		var idx = list.CreateHashIndex("k", s => s[..1]);
		list.Add("a1"); list.Add("b1");

		// updating a1 -> b9 must be rejected (duplicate key "b")
		Assert.ThrowsAny<Exception>(() => list[0] = "b9");

		// and the index must still know about key "a"
		Assert.Equal("a1", list[0]);
		Assert.True(idx.ContainsKey("a"));
		Assert.Equal("a1", idx["a"]);
	}

	// F11
	[Fact]
	public void ConcurrentList_EqualsSelf_DoesNotThrow()
	{
		using ConcurrentList<Int32> cl = new(new[] { 1, 2, 3 });

		Assert.True(cl.Equals(cl));
	}

	// F12
	[Fact]
	public void ByteArray_ContentsEquals_DifferentLengths_NotEqual()
	{
		Byte[] x = { 1, 2 };
		Byte[] y = { 1, 2, 3 };

		Assert.False(x.ContentsEquals(y));
	}

	// F13
	// Skipped until F13 is fixed: the Debug.Assert calls inside IndigentList crash the
	// Debug test host (Environment.FailFast) and would abort the whole test run.
	// Remove the Skip argument as part of fixing F13.
	[Fact(Skip = "Crashes the Debug test host until F13 is fixed - unskip when fixing F13")]
	public void IndigentList_SingleNullItem_Works()
	{
		IndigentList<String?> il = new()
		{
			(String?)null
		};

		// must not throw
		il.GetHashCode();
		Assert.Equal(0, il.IndexOf((String?)null));
		Assert.Null(il[0]);
	}

	// F06
	[Fact]
	public void JsonSerializer_ToFile_TruncatesExistingFile()
	{
		String path = Path.Combine(Path.GetTempPath(), "armat_regression.json");
		try
		{
			Armat.Serialization.JsonSerializer.ToFile(path, new JsonPayload { Text = new String('x', 200) });
			Armat.Serialization.JsonSerializer.ToFile(path, new JsonPayload { Text = "short" });

			JsonPayload? roundTripped = Armat.Serialization.JsonSerializer.FromFile<JsonPayload>(path);
			Assert.NotNull(roundTripped);
			Assert.Equal("short", roundTripped!.Text);
		}
		finally
		{
			File.Delete(path);
		}
	}

	// F05
	[Fact]
	public void XmlFileElementReference_SaveNewElement_Succeeds()
	{
		String path = Path.Combine(Path.GetTempPath(), "armat_regression.xml");
		try
		{
			File.WriteAllText(path, "<root><a>1</a></root>");

			XmlFileElementReference reference = new(path, "/root/b");
			XmlDocument doc = new();
			XmlElement element = doc.CreateElement("b");
			element.InnerText = "2";

			reference.SaveXmlElement(element);

			XmlElement? loaded = reference.LoadXmlElement();
			Assert.NotNull(loaded);
			Assert.Equal("2", loaded!.InnerText);
		}
		finally
		{
			File.Delete(path);
		}
	}

	// F14
	[Fact]
	public void PackAll_EnumeratesSourceOnce()
	{
		Int32 enumerations = 0;

		IEnumerable<IPackable> Source()
		{
			enumerations++;
			yield return new TestPackable();
			yield return new TestPackable();
		}

		IPackage[] packages = Source().PackAll();

		Assert.Equal(2, packages.Length);
		Assert.Equal(1, enumerations);
	}

	// F15
	[Fact]
	public void IndexedList_CopyTo_ThrowsWhenArrayTooSmall()
	{
		IndexedList<Int32> list = new(new[] { 1, 2, 3 });
		Int32[] target = new Int32[2];

		Assert.Throws<ArgumentException>(() => list.CopyTo(target, 0));
	}

	public class JsonPayload
	{
		public String Text { get; set; } = String.Empty;
	}

	private class TestPackable : IPackable
	{
		public IPackage Pack()
		{
			return new TestPackage();
		}
	}

	private class TestPackage : IPackage
	{
		public IPackable Unpack()
		{
			return new TestPackable();
		}
	}
}
```

Run with:
```powershell
dotnet test Solution/Armat.Utilities/Armat.Utilities.sln --filter "FullyQualifiedName~RegressionTests"
```

---

*End of findings. Generated by Claude Code from a full source review; findings F01–F15 verified by
execution against `armat.utils` built from commit `008605f`.*
