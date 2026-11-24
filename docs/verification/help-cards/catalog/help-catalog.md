### HelpCatalog Verification Design

#### Verification Approach

The catalog is tested directly in `HelpCatalogTests.cs` and `HelpCatalogTests.Views.cs` with catalogs created from the
embedded `simple` fixture and from small in-memory sources. The builder of the Composition subsystem is real because
it is the only way to create a catalog. A counting file provider proves that no file is read after creation.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures; `Parallel.For` for the concurrency scenario; no sleeps or timers; the
determinism scenario changes the current culture of the test thread and restores it.

#### Acceptance Criteria

All tests in the two files pass: results are exactly as specified, views never change their parent, hidden cards are
never returned, and parallel use gives consistent results with every predicate call counted.

#### Test Scenarios

- **Creation**: a null callback, no source (argument exception, not catalog exception), and a defective source with
  every error, the message of the first, and the count of the rest.
- **Properties and no I/O**: roots, depth-first cards, source description, tag names, culture, informational
  diagnostics with file and line; one listing and one read per file, none after use.
- **Find and Resolve**: names, aliases, case, slashes and white space, unknown and unqualified paths, a failing child
  and a failing root with the valid names.
- **Relations and examples**: children and see-also in order; examples by name, by card form, and by scope card;
  depth-first single example search.
- **Read-only lists**: every list refuses writes through the collection interface.
- **Views**: tag include and exclude, closed vocabulary rejection, source include and exclude, eager predicate, hiding
  by id, lifting to the roots and to the nearest visible ancestor, unknown ids, composition by intersection, shared
  data, and independent card objects.
- **Hidden cards**: lookups walk through them, return null for them, and examples and see-also entries of hidden cards
  are not found.
- **Concurrency and determinism**: sixty-four workers; two creations under two different current cultures.
