# Agent Knowledge Tool

`HelpAgentTool` lets an AI agent read the cards of a catalog. It is one tool for the whole catalog, with an index, a
card, an example, and a search operation. It does not depend on any agent framework: it has a name, a description, a
JSON schema for its parameters, and a method that takes the arguments as JSON text and returns text.

## Creating the Tool

```csharp
using DemaConsulting.HelpCards.Integration;

HelpAgentTool tool = HelpAgentTools.Create(catalog, new HelpAgentToolOptions
{
    Name = "myapp_help",
    MaxResults = 8,
    MaxOutputChars = 16000,
    IndexDepth = 2,
});

// With the agent framework of your choice:
//   register tool.Name, tool.Description, and tool.ParametersSchema,
//   and answer each call with tool.Invoke(argumentsJson).
string answer = tool.Invoke("""{"topic":"getting-started"}""");
```

A combined catalog of several libraries is one tool, so the agent has one place to ask. Use views to give an agent
a restricted catalog.

| Option | Default | Meaning |
| --- | --- | --- |
| `Name` | `help` | The tool name: a lower-case letter, then letters, digits, or underscores; 64 at most |
| `Description` | Generated | The description; the generated one lists each source with its summary |
| `MaxResults` | 8 | The default number of search results, 1 to 20 |
| `MaxOutputChars` | 16000 | The longest answer, 1000 to 200000 characters |
| `IndexDepth` | 2 | How deep the index goes, 0 to 8 |
| `ToolName` | The catalog's tool name | The name used in the text of the answers |

An option outside its range is an `ArgumentException` when the tool is created.

## The Parameter Schema

`tool.ParametersSchema` is a JSON Schema for the arguments. It is always the same text for the same version of the
library. It is written here in a readable form; the property returns it on one line without white space:

```json
{
  "type": "object",
  "properties": {
    "topic": {
      "type": "string",
      "description": "Card id or path such as svg/framed-figure; names and aliases work, case does not matter."
    },
    "example": {
      "type": "string",
      "description": "Example name, or card-id#name when several cards have an example of that name. With topic, only that card and the cards below it are searched."
    },
    "query": {
      "type": "string",
      "description": "Words to search for in titles, summaries, tags, aliases, and ids."
    },
    "source": {
      "type": "string",
      "description": "Restrict the index or the search to one source (library) name."
    },
    "tag": {
      "type": "string",
      "description": "Restrict the index or the search to cards with this tag."
    },
    "maxResults": {
      "type": "integer",
      "minimum": 1,
      "maximum": 20,
      "description": "The most search results to return."
    }
  },
  "additionalProperties": false
}
```

## The Arguments

`Invoke` accepts the arguments as a JSON object. It never throws for bad input: every failure is a plain-text answer
that says what to send instead.

| Arguments | Answer |
| --- | --- |
| `null`, empty, blank, JSON `null`, or `{}` | The index in Markdown down to `IndexDepth`, one block per source |
| `{"topic":"build/compile"}` | The card in Markdown with its examples in full |
| `{"example":"run"}` | The example, or the list of `card#name` choices when several cards have one |
| `{"topic":"build","example":"run"}` | The example, searched in that card and the cards below it |
| `{"query":"install"}` | The ranked search results |
| `{"source":"plexus"}` or `{"tag":"tutorial"}` | The index restricted to the source or the tag |
| `{"query":"graph","source":"plexus","maxResults":3}` | A search restricted and shortened |

These rules decide the answer when several arguments appear. When `example` is present the tool answers it, and a
`topic` only limits where it looks. Otherwise a `topic` is answered, and otherwise a `query`. A `query` that is
ignored because of a `topic` or an `example` is reported in one last line such as `Ignored 'query' because 'topic' was
given.` A property that is `null`, empty, or blank counts as absent, and the text of a property that is given is trimmed
of leading and trailing white space. A number for `maxResults` must be written as an integer: `5.0` is rejected.

| Input | Answer |
| --- | --- |
| Not valid JSON | `The arguments are not valid JSON ({reason}). Send an object such as {"topic":"svg"}.` |
| JSON that is not an object | `The arguments must be a JSON object.` |
| An unknown property | `Unknown argument 'x'. Valid arguments: topic, example, query, source, tag, maxResults.` |
| A property of the wrong type | `The argument 'topic' must be a string.` or the same for an integer 1 to 20 |
| A topic that is not found | Up to five suggestions, or the candidate ids when an unqualified name is ambiguous |
| An example that is not found | Up to five suggestions of example names, then card names |
| A search without results | `No cards match 'x'. Call with no arguments for the index.` |
| A tag in a catalog without any tag | `No cards have the tag 'x'; this help has no tags.` |

A topic is found by the global id or the path, names and aliases included. An unqualified local id of a mounted source
also works when exactly one card has it. A card answer is plain Markdown with the `Plain` link style, so a link reads
as the id of a card that the agent can ask for next. The `source` argument is the declared name of a source (the `name`
of its `_source.md`), not the mount that the application chose for it; the ids in the answers carry the mount.

## Output Budget

If an answer is longer than `MaxOutputChars`, the tool cuts it after the last complete line that fits and appends:

```text
[Truncated: 1234 characters omitted. Ask for a narrower topic or an example.]
```

The agent can then ask for a child card or for one example, which is the way the tool teaches progressive disclosure.
The line that reports an ignored `query` is appended after the cut, so a long answer never hides it.

## Failure Boundary and Security

`Invoke` catches the exceptions that a defective catalog or bad input can cause, `HelpCatalogException`,
`ArgumentException`, `InvalidOperationException`, `KeyNotFoundException`, `FormatException`, and `JsonException`, and
answers `The help tool could not answer ({ExceptionTypeName}).` without the text of the exception. It does not catch
fatal conditions such as running out of memory or a canceled operation.

The arguments of an agent are untrusted input. The tool never uses them as a file name or a path: after the catalog is
built the tool performs no file input or output, and no code is run anywhere. The cards themselves are trusted
content that the developers of the application wrote.
