### FrontMatterDecoder Design

![Loading Structure](LoadingView.svg)

#### Purpose

`FrontMatterDecoder` decodes the restricted subset of YAML front matter that cards use, and splits a card
file into entries and Markdown body. HelpCards owns this decoder so that adopters stay free of a YAML
dependency, errors are deterministic and name the exact line, and no YAML dialect can drift into the format.

#### Data Model

The result is an immutable tree of records, all produced by this unit:

| Type | Content |
| --- | --- |
| `FrontMatterDocument` | The entries in file order, the body, and the line the body starts on |
| `FrontMatterEntry` | A key, its line, and either a scalar or a list of items |
| `FrontMatterItem` | A list item that is either a scalar or a map of entries whose values are scalars |
| `FrontMatterScalar` | The unquoted text of a value and the line it starts on |

The decoder never goes deeper than a list of maps. An empty value (a key with nothing after the colon and no
list below it) is a scalar with empty text.

#### Key Methods

**`TryDecode(text, sourceName, file, out document, out problem)`** is the only entry point. It follows these
steps, and stops at the first defect of the file:

1. Remove a byte order mark and convert CRLF to LF, so line numbers match what an author sees.
2. Require `---` on line 1 and find the closing `---`; everything after it is the body.
3. Walk the front-matter lines. Blank lines and full-line comments are skipped. Tabs in indentation are
   errors. A top-level line is `key: value`; a key with nothing after the colon takes the list below it
   (dashes may start in the first column).
4. Parse a scalar by its first character: quotes start a quoted scalar (`''` is one quote in single quotes;
   only `\\` and `\"` are escapes in double quotes); `|` and `>` start a block scalar; `[ { & * ! # % @` and a
   backtick are rejected with a message that tells the author to quote the text; a plain scalar must not
   contain ` #` or `: ` and must not end with `:`.
5. Parse a literal block scalar only when the key is `content` of an item of the `examples` list. The block
   indent is the indent of the first non-blank line and must exceed the key's indent; the block runs while
   lines are blank or have at least that indent; the indent is removed, inner blank lines and `#` lines are
   kept, and trailing blank lines are dropped. Only `|` and `|-` are supported.
6. Skip one blank line after the closing delimiter and trim trailing line feeds of the body.

#### Error Handling

A defect ends the decoding of that file and is returned as a problem with rule `syntax` (or
`block-scalar-not-allowed` for a block scalar where only a single-line value is allowed), the source name,
the file, and the line. Inside the decoder the walk is left by a `FormatException` that carries the rule and
the line in its `Data`; it never leaves `TryDecode`. Validation of key names and value grammars is not done
here; that belongs to `CardReader`.

#### Dependencies

- `HelpProblem` and `RuleIds` of the Catalog subsystem for the reported problem.
- Base Class Library only (`System.Text.RegularExpressions` with a match timeout).

#### Callers

`CardReader` decodes every card, overlay, translation, and source file.
