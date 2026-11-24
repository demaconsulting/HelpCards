### HelpSource Design

![Loading Structure](LoadingView.svg)

#### Purpose

`HelpSource` is the public value that describes where the help cards of one library or tool come from. It
exists so that a library can expose its cards as one cheap, immutable, dependency-free object and so that an
application can combine the sources of several libraries without knowing where each one keeps its files.

#### Data Model

| Member | Type | Invariant |
| --- | --- | --- |
| `Provider` (internal) | `HelpFileProvider` | Never null; lists and reads the files |
| `Name` (internal) | `string?` | The name set with `WithName`; `null` when the source declares it in `_source.md` |
| `Tokens` (internal) | read-only dictionary | Names are ASCII letters and digits; values are not null |
| `ImportedSources` (internal) | read-only list | Imported sources in declaration order, never the source itself |

The type is immutable: every `With...` method and `Imports` returns a new instance that shares the provider
and copies the other values. Instances are safe for concurrent use.

#### Key Methods

- **`FromAssembly(assembly, prefix)`** creates a source over `AssemblyFileProvider`. The default prefix
  `{AssemblyName}.Help` is resolved when the files are listed, so creation touches nothing. Preconditions: the
  assembly is not null and a given prefix is not empty or white space.
- **`FromFiles(files, name)`** copies the dictionary, converts `\` in paths to `/`, and rejects empty paths,
  null texts, and two paths that are equal after the conversion.
- **`WithName(name)`** sets the declared name. The grammar of the name is checked when the source is read, so
  a violation is reported as a `source-invalid` problem together with the other defects of the source.
- **`WithTokens(tokens)`** replaces the token values; names must be ASCII letters and digits and values must
  not be null. The built-in names `tool` and `version` are reported as `token-redefined` when the source is
  read.
- **`Imports(imported)`** appends an imported source. The order, existence, and cycles of imports are checked
  by Composition when the catalog is built.
- **`FromProvider(provider, name)`** (internal) creates a source over any provider; tests use it to count or
  fail provider calls.

#### Error Handling

Argument defects (null or empty values, invalid token names, an import of itself, duplicate paths) throw
`ArgumentNullException` or `ArgumentException` at the call. Defects in the cards or in the source description
are never thrown here; they are problems that the reader reports.

#### Dependencies

- `HelpFileProvider` and its three implementations.
- `TokenResolver.IsValidTokenName` for the token name grammar.

#### Callers

- Adopting libraries and tools create sources and expose them.
- `HelpCatalogBuilder` (Composition) registers sources and passes them to `CardReader`.
- Tests create sources over counting providers.
