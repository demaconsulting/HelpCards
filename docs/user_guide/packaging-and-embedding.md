# Packaging and Embedding

A library ships its cards inside its own assembly, so the help is always the help of the exact version in use. This
chapter describes how the package embeds the files, how to change that, and the three kinds of `HelpSource`.

## How the Package Embeds Cards

The package contains two MSBuild files that import into every project that references the package directly:

- `build/DemaConsulting.HelpCards.props` sets the default `HelpCardsEnabled` to `true`.
- `build/DemaConsulting.HelpCards.targets` imports after the body of your project, so it sees your overrides and the
  `AssemblyName`. It embeds every file below the folder `HelpCardsDirectory` as an embedded resource, if the folder
  exists.

The logical name of each resource is the prefix, a slash, and the path of the file below the folder, with `/` between
the folders. With the defaults, the file `Help/build/compile.md` of the assembly `MyLib` becomes the resource
`MyLib.Help/build/compile.md`. All files are embedded, including the data files of examples such as `flow.mmd`.

Both files are packed under `build/`, not `buildTransitive/`. Only a project that references HelpCards directly
embeds cards. A project that references your library does not embed anything, and it needs no knowledge of HelpCards
unless it composes help.

## MSBuild Properties

| Property | Default | Meaning |
| --- | --- | --- |
| `HelpCardsEnabled` | `true` | Set to `false` to switch embedding off |
| `HelpCardsDirectory` | `Help` | The folder below the project directory that holds the cards |
| `HelpCardsPrefix` | `$(AssemblyName).Help` | The prefix of the logical resource names |

Set the properties in the body of the project file. Because the targets file imports after the project body, an
override is honored:

```xml
<PropertyGroup>
  <HelpCardsDirectory>Commands\Help</HelpCardsDirectory>
  <HelpCardsPrefix>MyLib.Commands.Help</HelpCardsPrefix>
</PropertyGroup>
```

When you change the prefix, pass the same prefix to `HelpSource.FromAssembly`, as the next section shows.

## The Three Kinds of Source

`HelpSource` is immutable: each `With...` method returns a new source, and creating one never reads a file. All
reading happens when `HelpCatalog.Create` runs.

| Factory | Reads | Use |
| --- | --- | --- |
| `HelpSource.FromAssembly(assembly, prefix)` | Embedded resources whose names start with `prefix/` | Shipping help |
| `HelpSource.FromDirectory(directory)` | The files below a folder on disk | Authoring and tests |
| `HelpSource.FromFiles(files, name)` | A dictionary of path to UTF-8 text | Tests of your own code |

```csharp
using DemaConsulting.HelpCards.Loading;

// The default prefix is "<AssemblyName>.Help"
HelpSource shipped = HelpSource.FromAssembly(typeof(MyLibHelp).Assembly);

// A prefix that was changed in the project file
HelpSource custom = HelpSource.FromAssembly(typeof(MyLibHelp).Assembly, "MyLib.Commands.Help");

// While writing cards: edit a file and see the change without a rebuild
HelpSource authoring = HelpSource.FromDirectory("src/MyLib/Help");

// In a unit test
HelpSource inMemory = HelpSource.FromFiles(new Dictionary<string, string>
{
    ["a.md"] = "---\ntitle: A\nsummary: The card A.\n---\nBody.",
}, name: "test");
```

The name of a source comes from the key `name` of `_source.md`. A source without that file needs
`WithName("demo")`. A name is a segment, unique in a catalog, and it is public contract: external links name it.
There is no way to rename a source in the consumer, because that would break those links. When two libraries declare
the same name, one of them must change its `_source.md`.

`FromDirectory` is for development and tests. It reads whatever folder it is given, so never build the path from
input of a user. Ship help with `FromAssembly`.

## Exposing the Source

Give every library one static member that exposes its source, so applications can compose it without knowing where
the files are:

```csharp
public static class MyLibHelp
{
    public static HelpSource Source { get; } = HelpSource.FromAssembly(typeof(MyLibHelp).Assembly)
        .WithTokens(new Dictionary<string, string> { ["maxItems"] = "42" });
}
```

The signature of `HelpSource` uses only types of the Base Class Library, so exposing it does not force a version of
anything else on your callers.

## Checking the Package

Two checks keep embedding honest:

- A test that builds the catalog from `MyLibHelp.Source` with `HelpCardValidator.AssertValid` finds every defect of
  the cards. Add an assertion that a known card exists, for example `catalog.Find("getting-started")` is not null,
  because a wrong prefix finds no files at all. The chapter *Testing Your Help* describes the validator.
- `HelpCardValidator.CompareToDirectory(source, directory)` compares the embedded files with the files on disk and
  reports a file that exists on only one side as the rule `resources-mismatch`. Use it in a test to catch a card that
  was added to the folder but is not embedded, for example because the project file excludes it.

The HelpCards repository checks its own package with FileAssert to prove that the two build files are in the package.
Resources are stored uncompressed in this release.
