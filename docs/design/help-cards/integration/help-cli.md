### HelpCli Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`HelpCli` is the command-line helper that a tool adds in front of its own argument parser. It decides whether a command
line is a help command, runs it, and gives the tool a footer sentence for its usage text. It is independent of any
command-line library.

#### Data Model

An immutable object holding the verb and a `CliDispatcher` created from the checked options. The public supporting types
are `HelpCliOptions` (tool name, version, which is for information only and not printed, verb, catalog factory, usage
lines, preamble, index sections, fallback resolver, output and error writers, command template, default width, language
option, and width provider), `HelpCliSection` (a heading and lines), and `HelpCliAnswer` (text and exit code). The
helper keeps the options object by reference; the checks are made at `Create`, and the writers, usage lines, sections,
fallback, and other settings are read from the object each time a command runs.

#### Key Methods

- **`Create(options)`** checks the options and creates the helper. It rejects null options and factory, a tool name or
  verb that is white space, a verb with white space or a leading hyphen, a tool name with a brace, and, through
  `HelpRenderOptions`, a white-space command template and a default width that is not 0 or 20 to 500. It never calls the
  factory and does no I/O.
- **`TryRun(args, out exitCode)`** returns false with exit code 0 unless there is a first argument that equals the verb,
  ignoring case; otherwise it runs the remaining arguments and returns true.
- **`Run(helpArguments)`** runs a help command whose verb is already removed.
- **`Footer`** is the constant sentence `Run '{tool} {verb}' for topics and examples.`

#### Error Handling

Programming mistakes throw `ArgumentException` or `ArgumentNullException`. Help commands never throw for their input; a
defective catalog is exit code 70.

#### Dependencies

`CliDispatcher` and `HelpRenderOptions`.

#### Callers

The tools that adopt HelpCards, usually from `Main` and from the tool's own usage text.
