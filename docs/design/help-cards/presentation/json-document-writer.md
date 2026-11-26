### JsonDocumentWriter Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`JsonDocumentWriter` writes the documents of JSON contract version 1: `index`, `card`, `examples`, `example`, `search`,
`cards`, and `error`. Each is one line of ASCII, with the keys in a fixed order, no JSON null, and arrays that are
always present.

#### Data Model

The class is static, stateless, and partial: `JsonDocumentWriter.cs` holds the envelope, the escaping, and the error
document; `.Cards.cs` holds the card, examples, and example documents; `.Lists.cs` holds the index, search, and card
list documents. Documents are written with `Utf8JsonWriter` into a buffer.

| Document | Fields after `helpSchemaVersion`, `ok`, `kind` |
| --- | --- |
| `index` | `tool`, `version` (only when set), `usage` (only when supplied), `sources`, `cards` with `depth` |
| `card` | the identity fields, `culture`, `body`, `children`, `seeAlso`, `examples`, `links` |
| `examples` | `id`, `examples`, `cards` with `id`, `summary`, `command`, `examples` |
| `example` | `id`, `example` with `card` and the fields of an item of `examples` |
| `search` | `query`, `results` with `id`, `title`, `summary`, `source`, `score`, `matchedOn`, `command` |
| `cards` | `filter` with `tags` and `sources`, `cards` with `id`, `title`, `summary`, `tags`, `source`, `command` |
| `error` | `code`, `message`, `hint` (optional), `candidates` (only for `not-found`, `ambiguous`); see `WriteError` |

#### Key Methods

- **`WriteDocument`** writes the default envelope and the fields, or passes the kind and an action that writes the
  fields to the `JsonEnvelope` callback of the caller, which writes the top-level object itself.
- **`EscapeNonAscii`** replaces every character from U+007F upward with its `\uXXXX` escape (surrogate pairs as two
  units) after the document is complete. The writer uses the relaxed escaping, so quotes, apostrophes, backticks, and
  angle brackets stay readable. Because the replacement runs on the finished text, it also covers a callback envelope.
- **`WriteCommand`** writes `command` only when the link style is `Command`.
- **`WriteExample`** writes an example object; after `content` it appends `exitCode` (a number, only when it is not 0)
  and `expect` (an array of strings, only when not empty), so documents of examples that state no result are unchanged.
- **`WriteLinks`** lists the visible link targets of the body with `cardId`, `exampleName`, `exampleCardId`, and `text`,
  each only when present.
- **`WriteError`** accepts only the codes `usage-error`, `not-found`, `ambiguous`, and `catalog-error`. A caller may
  give a body callback, which alone writes the members of the error in place of the default ones (`code`, `message`,
  `hint`, `candidates`) inside the envelope in effect (used by the command-line helper for the `JsonError` option of a
  tool). The message, hint, and candidates are not passed to the body callback by the writer, which only checks the
  code; the helper closes over them in a `HelpCliError`.

The examples of a JSON card always carry their content, because a JSON reader asks for data; `IncludeTags` and
`IncludeExampleContent` shape only the text formats.

#### Error Handling

Null arguments throw `ArgumentNullException`; an unknown error code, an unknown example, and a `JsonEnvelope` callback
that returns null throw `ArgumentException`.

#### Dependencies

`RenderContext`, `LinkFormatter` (for the command and the body), `HelpCard`, `HelpExample`, `HelpSearchResult`, and
`System.Text.Json`.

#### Callers

The `Render*` members of `HelpCatalog` when the format is `Json` (used by `HelpCli` and `HelpAgentTool`), and the
command-line dispatcher of `HelpCli` for the error document and the usage lines.
