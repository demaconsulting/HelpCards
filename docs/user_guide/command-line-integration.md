# Command-Line Integration

`HelpCli` adds a help command to a tool. It is independent of any argument-parsing library: the tool hands it the
raw arguments before its own parser runs, and it answers only when the first argument is the help verb.

## The Integration Recipe

There are two integration points, both before the tool's own parser, and one line in the usage text:

```csharp
using DemaConsulting.HelpCards.Integration;

internal static class Program
{
    private static readonly HelpCli Help = HelpCli.Create(new HelpCliOptions
    {
        ToolName = "tool",
        Version = "1.4.0",
        ConfigureCatalog = (builder, culture) => builder.AddSource(MyLibHelp.Source),
    });

    public static int Main(string[] args)
    {
        if (Help.TryRun(args, out var exitCode))
        {
            return exitCode;
        }

        // The tool's own commands. At the end of its usage text:
        Console.WriteLine(Help.Footer);
        return 0;
    }
}
```

`TryRun` returns `false` at once, without creating the catalog and without touching any resource, unless there is at
least one argument and the first one equals the verb, ignoring case. `Footer` is a constant sentence, `Run 'tool
help' for topics and examples.`, that also needs no catalog. A normal run of your tool therefore pays nothing for
help. The catalog is built the first time a help command arrives. The helper creates the catalog builder, sets
the tool name from `ToolName`, the version from `Version`, and the culture of the `--lang` option, and then calls
`ConfigureCatalog(builder, culture)`, where the culture is the one the user asked for or `null`. You state the tool
name and the version once, in the options; they are the `{{tool}}` and `{{version}}` of the cards, the commands in
the links, and the version of the JSON documents. The action can still call `ToolName` or `ToolVersion` on the builder
to override them. The older `CatalogFactory` (a function from the culture to a whole catalog) still works, but then you
set `ToolName` and `ToolVersion` on your own builder as well; set exactly one of the two.

If building the catalog throws a `HelpCatalogException`, the helper prints `Help is unavailable: {first problem}` and
exits with 70. A tool that wants to answer a `help` call with its own text uses the `FallbackResolver` described below.

## Command Grammar

```text
tool help [PATH...] [OPTIONS]
```

The words of `PATH` form one path, split on slashes and white space, so `tool help build compile` and
`tool help build/compile` are the same. Names and aliases work and case does not matter. Options can appear
anywhere, `--` ends the options, and a value option takes the next argument only; a value that starts with `--` is not
consumed. Option names are case-sensitive, and an unknown option is a usage error that lists the valid ones. Every
option can appear once, except `--tag` and `--source`, which can repeat and combine with OR.

| Option | Value | Applies to | Meaning |
| --- | --- | --- | --- |
| `--json` | none | all | Same as `--format json`; a different `--format` as well is a usage error |
| `--format` | `text`, `markdown`, `json` | all | The output format; the default is `text` |
| `--examples` | none | needs a PATH | Prints the card's examples in full and lists cards below it with examples |
| `--example` | name | needs a PATH | Prints one example alone; `card#name` is accepted |
| `--search` | one query | no PATH | Searches; quote several words; combines with `--tag` and `--source` |
| `--tag` | tag | no PATH | Lists the cards that have any of the tags; repeatable |
| `--source` | source name | no PATH | Limits the index, `--search`, and `--tag` to the sources; repeatable |
| `--all` | none | no PATH | Lists the whole tree in the index instead of the roots |
| `--width` | 20 to 500, or `auto` | text, Markdown | Wrapping width; `auto` is the console width, or 78 |
| `--lang` | culture name | all | Chooses the culture; exists only when `EnableLanguageOption` is set |

The helper works in one of six modes: the **index** (no path, no mode option), a **card** (a path), **examples**
(`--examples`), one **example** (`--example`), **search** (`--search`), and **tags** (`--tag`). These combinations are
usage errors: two mode options together (`--examples`, `--example`, `--search`), `--examples` or `--example` with
`--tag` or `--all`, `--search` with `--all`, `--search` with a path, `--examples` or `--example` without a path, and a
path together with `--tag`, `--source`, or `--all`. `--search` may be combined with `--tag` and `--source`. `--tag` with
`--all` is also a usage error: this is an intentional addition to the option table of the specification (6.12), because
`--all` widens the index to the whole tree while `--tag` lists the cards that have a tag, and the two cannot both apply.
With `--width auto`, the console width is clamped to 40 to 120 columns, and 78 is used when the output is redirected.

`--lang` takes a culture name of 2 or 3 letters followed by subtags of 2 to 8 letters or digits, such as `de` or
`de-DE`; any other value is a usage error. A well-formed name for which no card has a translation is not an error: the
neutral files are shown. The name is not checked against the operating system, so the answer does not depend on the
machine (on a machine whose runtime has no culture data, for example with invariant globalization, translations cannot
be selected and the neutral files are shown).

## What Is Printed

A success prints the rendering and a line feed to the output writer and exits with 0. Text and Markdown errors go to
the error writer as `Error: {message}` with an optional second line for the hint; JSON errors are a document on the
output writer. The exit codes are:

| Exit code | Meaning |
| --- | --- |
| 0 | Success |
| 2 | Usage error, or a card, example, tag, or source that is not found or not unique |
| 70 | The catalog could not be built: a defect in a source |

The index is the usage lines, a blank line, the preamble, the sections, the `Help topics:` list, a blank line, and
the footer:

```text
Usage:
  tool [options]
  tool help [PATH]

Tool does things.

Commands:
  build  Build it

Help topics:
tool help guide      How to set up and use the tool.
tool help reference  Facts about limits and options.

Run 'tool help' for topics and examples.
```

Links in cards appear as commands that the user can paste. A card printed with `tool help guide` says ``Start with
`tool help guide setup` `` and ``Run it with `tool help guide --example run` ``, and ends with the lists of child
cards, examples, and related cards. A link to an example is always the full command, also when the example belongs to
the card that holds the link, so it works wherever it is copied (the Markdown style likewise names the card in the
target, `[run](guide#run)`). A path that does not exist gives a hint:

```text
Error: Unknown help card 'usages' under 'guide'.
Did you mean 'usage'? Cards under 'guide': setup, usage. Run 'tool help guide' for their summaries.
```

`--examples` prints each example of the card in a fenced block, then a hint for printing one alone, and then lists the
descendant cards with examples without printing their content. `--example run` prints exactly the content of that
example and nothing else, so the output can be piped. An example name that matches several examples is ambiguous and
exits with 2; use `card#name`.

## Customizing the Helper

| Option of `HelpCliOptions` | Meaning |
| --- | --- |
| `ToolName` | Required: the name used in commands, a word without braces |
| `Version` | The version of the tool; with `ConfigureCatalog` it is the catalog's `ToolVersion` too |
| `Verb` | The help verb; the default is `help` |
| `ConfigureCatalog` | Required, or `CatalogFactory`: `(builder, culture)` adds the sources |
| `CatalogFactory` | The older alternative: builds the whole catalog; not combined with `ConfigureCatalog` |
| `GetCatalogConfiguration(culture)` | A method: the same configuration as an action for the validator |
| `UsageLines` | The usage lines at the top of the index |
| `IndexPreamble` | A paragraph between the usage lines and the topics |
| `IndexSections` | A list of `HelpCliSection(heading, lines)` printed between the preamble and the topics |
| `FallbackResolver` | Offered the path words of a card command before the catalog is searched |
| `Output`, `Error` | The writers; the defaults are the console |
| `OutputProvider`, `ErrorProvider` | Return the writer when a command writes; `null` is the console |
| `CommandTemplate` | The template for command links, `{tool}` and `{path}`; the default is `{tool} help {path}` |
| `DefaultWidth` | The wrapping width, 78 by default |
| `EnableLanguageOption` | Adds `--lang` |
| `OutputWidthProvider` | Supplies the console width for `--width auto`, which helps in tests |

A `FallbackResolver` lets a tool keep answers that are not cards. It receives the path words and returns a
`HelpCliAnswer(text, exitCode)` or `null` to continue with the cards. A tool can print the usage of its own command
`build` this way and let the cards document everything else. The resolver is offered plain card commands only
(`help build`); `help build --examples` and `help build --example x` go to the catalog, which does not know the subject
and answers with a not-found error. The options object is kept by the helper, not copied; do not change it after you
create a helper from it.

A provider is the way to follow an option that the tool reads after it created the helper, such as `--silent`:
`OutputProvider = () => silent ? TextWriter.Null : null`, with `ErrorProvider` likewise, is evaluated each time a
command writes. A provider counts as a writer that is not the console, so `--width auto` uses `OutputWidthProvider`
or 78.

To prove in a test that the cards of the tool are valid in the composition that users get, pass the same
configuration to the validator:

```csharp
HelpCardValidator.AssertValid(options.GetCatalogConfiguration());
```

`Run(arguments)` is the same helper without the verb check, for a tool whose own parser has already recognized the
help command and removed the verb.

## JSON Output

`--json` and `HelpRenderOptions.Format = HelpFormat.Json` produce one document per call on a single line, with ASCII
characters only, a fixed key order, and no `null`: an absent value is left out and a list is always present. With the
default envelope every document starts with the keys `helpSchemaVersion` (now 1), `ok`, and `kind`; `ok` is `true`
except for an `error` document, which has `"ok": false`. New keys appear only at the end of an
object in later minor releases; keys are never removed or reordered within version 1.

| `kind` | Content |
| --- | --- |
| `index` | `tool`, `version`, `usage`, `sources`, and `cards` |
| `card` | `id`, `title`, `summary`, `aliases`, `tags`, `source`, `culture`, `body`, `children`, `seeAlso`, `examples` |
| `examples` | `id`, `examples`, and `cards`: the descendants with examples |
| `example` | `id` and `example` |
| `search` | `query` and `results` |
| `cards` | `filter` with `tags` and `sources`, and `cards` for the tag listing |
| `error` | `ok` is `false`, `code`, `message`, `hint`, and `candidates` |

The entries of the lists have these keys:

- `sources` of an index: `name`, `title`, `summary`.
- `cards` of an index: `id`, `title`, `summary`, `aliases`, `tags`, `source`, `depth`, `command`.
- `children` and `seeAlso` of a card: `id`, `title`, `summary`.
- `examples` of a card or an examples document, and `example` of an example document (which adds `card`): `name`,
  `kind`, `language`, `fileName`, `description`, `content`.
- `cards` of an examples document: `id`, `summary`, `command`, and `examples`, the names of the examples.
- `results` of a search: `id`, `title`, `summary`, `source`, `score`, `matchedOn`, `command`.
- `cards` of a tag listing: `id`, `title`, `summary`, `tags`, `source`, `command`.
- `links` of a card: `cardId`, and `text`, `exampleName`, and `exampleCardId` when they apply.

The `command` key appears on cards only when the document is made by the command-line helper (it is the command that
shows the card). A `card` document also has a `links` list of the visible link targets of the body. The error codes are
`usage-error`, `not-found`, `ambiguous`, and `catalog-error`. This is a not-found error, formatted here for reading:

```json
{
  "helpSchemaVersion": 1,
  "ok": false,
  "kind": "error",
  "code": "not-found",
  "message": "Unknown help card 'setups' under 'guide'.",
  "hint": "Did you mean 'setup'? Cards under 'guide': setup, usage. Run 'tool help guide' for their summaries.",
  "candidates": ["guide/setup", "guide/usage"]
}
```

A tool that must keep an existing JSON envelope sets `HelpRenderOptions.JsonEnvelope`, a callback that receives the
kind (not `ok`) and a function that writes the fields, and returns the whole document. The callback replaces the
default envelope, so `helpSchemaVersion`, `ok`, and `kind` appear only if it writes them. HelpCards then writes the
fields of the document after the members that the callback wrote itself.

## Rendering Without the Helper

The helper is a thin layer over the catalog. An application that wants its own commands calls the catalog directly with
`HelpRenderOptions`:

```csharp
var options = new HelpRenderOptions
{
    Format = HelpFormat.Markdown,
    LinkStyle = HelpLinkStyle.Command,
    CommandTemplate = "{tool} help {path}",
    Width = 100,
};

string page = catalog.Render(card, options);
string index = catalog.RenderIndex(options, maxDepth: 1);
```

| Option | Meaning |
| --- | --- |
| `Format` | `Text` (default), `Markdown`, or `Json` |
| `LinkStyle` | `Plain` (default), `Command`, or `Markdown`; shapes how `[[links]]` and lists appear |
| `CommandTemplate` | The command for the `Command` style; `{path}` is the global id with spaces for slashes |
| `LinkFormatter` | A callback that writes every link of a visible target itself; overrides `LinkStyle` |
| `Width` | The wrapping width, 78 by default, 0 for none, otherwise 20 to 500 |
| `IncludeTags` | Show the tags line, `true` by default |
| `IncludeExampleContent` | List examples with their content, `false` by default |
| `JsonEnvelope` | Replaces the top-level object of JSON documents |

The output has LF line endings, no final newline, and no color codes, and it is the same on every machine. Only
generated lines are wrapped; the authored body is never re-flowed.
