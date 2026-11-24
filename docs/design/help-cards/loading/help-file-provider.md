### HelpFileProvider Design

![Loading Structure](LoadingView.svg)

#### Purpose

`HelpFileProvider` is the seam through which the loader lists and reads the files of one source. It exists so
that the rest of the loader never knows whether files are embedded resources, files on disk, or text in
memory, and so that tests can count or fail calls without touching a file system. Its implementations
`AssemblyFileProvider`, `DirectoryFileProvider`, and `MemoryFileProvider` are supporting types of this unit.

#### Data Model

The abstract class has no state. Each implementation holds the minimum it needs:

| Implementation | State | Notes |
| --- | --- | --- |
| `AssemblyFileProvider` | assembly, optional prefix, lazy map of paths to resource names | Built once under a lock |
| `DirectoryFileProvider` | the directory as given | Resolved and enumerated on every call |
| `MemoryFileProvider` | a copy of the files by path | Immutable after construction |

Paths are relative to the help root, use `/` as the separator, and are listed in `StringComparer.Ordinal`
order.

#### Key Methods

- **`ListFiles()`** returns every file of the source in ordinal order. `AssemblyFileProvider` keeps the
  resources whose names start with `{prefix}/`, converts `\` (which MSBuild writes for nested folders on
  Windows) to `/`, and remembers the real resource name for reading. `DirectoryFileProvider` enumerates all
  files below the directory.
- **`ReadText(path)`** returns the text of a listed file. All implementations decode strict UTF-8 and remove a
  byte order mark (`DecodeUtf8`); bytes that are not valid UTF-8 raise an exception instead of being replaced.
  `DirectoryFileProvider` refuses a path that resolves outside its directory.

#### Error Handling

Providers throw the natural exception of the failure (`DirectoryNotFoundException`, `IOException`,
`ArgumentException` for an unknown path or invalid bytes). They do not catch; `CardReader` converts every
failure of a list or a read into a `source-read` problem that names the exception type and message.

#### Dependencies

Base Class Library only (`System.Reflection`, `System.IO`, `System.Text`).

#### Callers

`HelpSource` creates providers; `CardReader` lists and reads through them.
