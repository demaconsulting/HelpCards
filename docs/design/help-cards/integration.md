## Integration Subsystem Design

![Integration Structure](IntegrationView.svg)

### Overview

The Integration subsystem connects the catalog to its two kinds of readers: people at a command line, and AI agents.
Both are provider-neutral: the command-line helper works with any command-line library, and the knowledge tool works
with any agent framework, because each takes plain strings and writes plain strings. Neither reads a file; both read the
immutable catalog.

The subsystem contains these units:

- **HelpCli** (public) is the command-line helper: a tool creates it once, calls `TryRun` before its own parser, and
  prints `Footer` in its usage text. With it go the public `HelpCliOptions`, `HelpCliSection`, and `HelpCliAnswer`.
- **CliArgumentParser** (internal) reads the arguments after the verb into a request or the first usage error. The
  internal `CliRequest`, `CliMode`, and `CliParseResult` are its supporting types.
- **CliDispatcher** (internal) runs one help command: it creates the catalog when it is needed, selects the answer of
  the mode, and writes it with the exit code. It is a partial class: the flow, the answers of the modes, and the
  lookups. The internal `CliOutcome` and `CliFailure` are its supporting types.
- **HelpAgentTool** (public) is the knowledge tool for AI agents: name, description, JSON schema, and `Invoke`. With it
  go the public `HelpAgentToolOptions` and the static factory `HelpAgentTools`.
- **AgentArgumentReader** (internal) reads the JSON arguments of a call and checks them against the schema.
  `AgentArguments` and `AgentReadResult` are its supporting types.
- **AgentDispatcher** (internal) answers one call from checked arguments: the index, a card, an example, or search
  results. It is a partial class: the choice of the answer and the index, and the lookups.

### Interfaces

| Interface | Direction | Purpose |
| --- | --- | --- |
| `HelpCli.Create(options)` | Inbound (tools) | Create the helper without creating the catalog |
| `HelpCli.TryRun`, `Run`, `Footer` | Inbound (tools) | Answer a help command |
| `HelpCliOptions.CatalogFactory` | Outbound (tools) | Create the catalog, only after the verb was recognized |
| `HelpCliOptions.FallbackResolver` | Outbound (tools) | Let a tool answer subjects of its own |
| `HelpAgentTools.Create(catalog, options)` | Inbound (hosts) | Create the knowledge tool for a catalog |
| `HelpAgentTool.Name`, `Description`, `ParametersSchema` | Outbound (hosts) | Register the tool with a framework |
| `HelpAgentTool.Invoke(json)` | Inbound (hosts) | Answer one call |
| `HelpCatalog` members | Outbound (Catalog, Presentation) | Look up, search, suggest, and render the help |

The Integration subsystem depends on the Catalog subsystem for lookup, views, search, and suggestions, and on the
Presentation subsystem for every document it writes (it calls the `Render` members and the error document writer of the
JSON writer). No other subsystem depends on it.

### Design

**Deferred catalog.** The helper holds a catalog factory instead of a catalog. `TryRun` compares the first argument with
the verb and returns false at once for anything else, so a tool pays nothing for its ordinary commands; `Footer` is a
constant. The factory is called once for each help command, after the arguments were read, so a usage error never
creates the catalog. A tool may offer the words of a card command to a fallback resolver first, which also runs before
the factory.

**One grammar.** The parser is the only reader of the command line. It reads words as one path, options anywhere, a
value as the next argument only, and reports the first usage error in a fixed order, so the same mistake always gives
the same message. Whether the arguments ask for JSON is decided from the arguments alone, so that even a usage error is
written as JSON.

**Exit codes and channels.** Success is 0. Wrong arguments, a card, example, source, or tag that does not exist, and an
example name that matches several examples are 2; a catalog that is defective or not created is 70. Text and Markdown
errors are written to the error writer as `Error: {message}` with an optional hint line; JSON errors are the error
document on the output. Everything is written with line feeds only.

**Links as commands.** The helper renders with the command link style and a command template made from the tool name and
the verb, so every link in an answer is something the reader can type. The width is the width of the command, the
default of the options, or, for `auto`, the console width clamped to 40 to 120.

**One tool for the catalog.** The knowledge tool serves a whole catalog; sources are told apart by the `source`
argument. The first rule that applies decides an answer: an example, else a topic, else a query, else the index. Every
miss says what to send instead, with suggestions, and every wrong argument is a plain-text message, because an agent
corrects itself from the text.

**Budget and boundary.** An answer over the output limit is cut at a line end and says how much was left out. `Invoke`
catches the recoverable exceptions (catalog, argument, invalid operation, key not found, format, and JSON) and answers
with the name of the exception type only, never its text, which could hold what the agent sent. It does not catch
out-of-memory, cancellation, or other conditions of the process. Argument text is compared with names only and is never
used as a path.

**Failure model.** Programming mistakes (null arguments, invalid options) throw `ArgumentException` or
`ArgumentNullException` where the helper or tool is created or called; bad input from a person or an agent never throws.

The unit design documents describe each unit in detail; the verification design describes how the subsystem is tested
through the helper and the tool.
