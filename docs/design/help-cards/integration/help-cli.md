### HelpCli Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`HelpCli` is the command-line helper that a tool adds in front of its own argument parser. It decides whether a command
line is a help command, runs it, and gives the tool a footer sentence for its usage text. It is independent of any
command-line library.

#### Data Model

An immutable object holding the verb and a `CliDispatcher` created from the checked options. The public supporting types
are `HelpCliOptions` (tool name and version, verb, the catalog configuration action or, instead of it, a catalog
factory, usage lines, preamble, index sections, fallback resolver, output and error writers or the providers that return
them when a command writes, command template, default width, language option, and width provider), `HelpCliSection` (a
heading and lines), and `HelpCliAnswer` (text and exit code). The helper keeps the options object by reference; the
checks are made at `Create`, and the writers, usage lines, sections, fallback, and other settings are read from the
object each time a command runs.

`HelpCliOptions.ConfigureCatalog` is the single place where a tool describes how its catalog is built. The helper sets
`ToolName`, `ToolVersion` (when `Version` is set), and `Culture` (when the `--lang` option gave one) on the builder and
then calls the action, so the tool name and the version are written once. `GetCatalogConfiguration(culture)` returns
that same sequence as an `Action<HelpCatalogBuilder>` for `HelpCardValidator.Validate` and `AssertValid`, so the tests
of a tool validate the composition that users get. `CatalogFactory` stays for tools that build the whole catalog
themselves and then set the name and the version on their own builder; exactly one of the two must be set.

#### Key Methods

- **`Create(options)`** checks the options and creates the helper. It rejects null options, options with neither or both
  of `ConfigureCatalog` and `CatalogFactory`, an output or error writer set together with its provider, a tool name or
  verb that is white space, a verb with white space or a leading hyphen, a tool name with a brace, and, through
  `HelpRenderOptions`, a white-space command template and a default width that is not 0 or 20 to 500. It never builds
  the catalog and does no I/O.
- **`HelpCliOptions.GetCatalogConfiguration(culture)`** returns the builder action that the helper itself applies (name,
  version, culture, then `ConfigureCatalog`) and throws `InvalidOperationException` when `ConfigureCatalog` is not set.
- **`TryRun(args, out exitCode)`** returns false with exit code 0 unless there is a first argument that equals the verb,
  ignoring case; otherwise it runs the remaining arguments and returns true.
- **`Run(helpArguments)`** runs a help command whose verb is already removed.
- **`Footer`** is the constant sentence `Run '{tool} {verb}' for topics and examples.`

#### Error Handling

Programming mistakes throw `ArgumentException` or `ArgumentNullException`. Help commands never throw for their input; a
defective catalog is exit code 70.

#### Dependencies

`CliDispatcher`, `HelpRenderOptions`, and, for `GetCatalogConfiguration`, the public `HelpCatalogBuilder` of the
Composition subsystem.

#### Callers

The tools that adopt HelpCards, usually from `Main` and from the tool's own usage text.
