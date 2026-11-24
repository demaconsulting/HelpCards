# Writing Cards

A card is a Markdown file with a block of front matter. This chapter describes the file layout, every key of the front
matter, the names that cards and examples carry, links, tokens, examples, and translations.

## File Layout

The files of a source live below one folder, by default the `Help` folder of the project. The path of a file below
that folder, without the extension, is the **local id** of the card.

| File | Meaning |
| --- | --- |
| `Help/build.md` | The card `build`, a root card |
| `Help/build/compile.md` | The card `build/compile`, a child of `build` |
| `Help/_source.md` | Describes the source; not a card. Needs a `name:` unless `WithName` is used |
| `Help/build/compile.de.md` | The German translation of the card `build/compile` |
| `Help/build/sample.yaml` | A data file that an example of `build` or `build/compile` can name |

A folder never needs a card of its own, but a card `a/b` needs its parent `a`: a missing parent is the error
`parent-missing`. Every file name is a *segment*, a name made of lower-case letters and digits with single hyphens
between them, at most 48 characters, so `compile`, `flow-chart`, and `step2` are valid and `Compile`, `my_card`, and
`a--b` are not. A local id has one to eight segments. The names are stored in lower case and compared without regard
to case.

## The Card File

A card file is a line with three hyphens, the front matter, another line with three hyphens, an optional blank line, and
the body. Line endings can be LF or CRLF, and a byte order mark is ignored. This card uses every construct that the
front matter accepts (the examples use `save-as`, so the source declares `schema: 2` in `_source.md`; see *Examples*):

```yaml
---
# Full-line comments are allowed between entries.
title: Flow charts
summary: Nodes, edges, sections, and the direction of a flow chart.
aliases:
  - flow-chart
tags:
  - authoring
order: 20
children:
  - settings
see-also:
  - mermaid/style
  - plexus:graph/layout
examples:
  - name: flow
    kind: source
    language: mermaid
    file: flow.mmd
    description: A small flow chart with two sections.
  - name: render
    kind: command
    language: shell
    content: '{{tool}} render flow.mmd -o flow.png'
    description: Render the chart.
  - name: config
    kind: source
    language: yaml
    save-as: settings.yaml
    content: |
      title: Demo
      sections:
        - name: Plan
    description: A minimal inline configuration file.
x-audience: agent
---
Body in Markdown.
```

### Keys

A file is an **overlay** when it has the key `extends`, a **translation** when its name has a culture suffix such as
`.de`, and a **card** otherwise. In the table, R means required, O optional, and a hyphen means not allowed. A key
that is not allowed is the error `key-unknown`, and its message lists the allowed keys.

| Key | Value | Card | Overlay | Translation | `_source.md` |
| --- | --- | --- | --- | --- | --- |
| `title` | One-line scalar | R | O | O | O |
| `summary` | One-line scalar | R | O | O | O |
| `aliases` | List of alternative names | O | - | O (added) | O (names the mount root) |
| `tags` | List of unique segments | O | - | - | - |
| `order` | Integer, at most seven digits | O | - | - | - |
| `children` | Child names that set the order | O | - | - | O (orders the source roots) |
| `see-also` | List of references | O | O | - | - |
| `examples` | List of examples | O | O | O (`name`, `description`) | - |
| `extends` | One reference | - | R | - | - |
| `optional` | `true` or `false` | - | O | - | - |
| `schema` | Integer, default 1; 1 and 2 are read, use 2 | - | - | - | O |
| `name` | Segment | - | - | - | R (or `HelpSource.WithName`) |
| `language` | Culture name, default `en` | - | - | - | O |
| `x-` followed by a name | One-line scalar | O | O | - | O |

Keys that start with `x-` carry your own data. HelpCards keeps them in `HelpCard.Extensions` and otherwise ignores
them. A missing required key is `key-missing`, and a value of the wrong shape is `value-invalid`.

### Scalars

A value is a plain, a single-quoted, or a double-quoted scalar. A single-quoted scalar writes a quote as two quotes.
A double-quoted scalar knows two escapes only, a backslash and a quote. A plain scalar must not start with one of the
characters `[ { | > & * ! # % @` or a backtick, must not contain a colon followed by a space or a space followed by a
hash character, and must not end with a colon. When a value needs one of those, quote it:

```yaml
title: 'Setup: first steps'
summary: "Use the \"help\" command"
```

Tabs in the indentation, duplicate keys, flow collections such as `[a, b]`, anchors, aliases, tags, trailing
comments, and nesting deeper than a list of maps are errors. `title` and `summary` are single lines.

### The Literal Block

The only values that can span several lines are the `content` and the `expect` of an example, written as a literal block
with `|` or `|-`. The block starts on the next line. Its indentation is that of its first non-blank line and must be
deeper than the key. The block ends at the first non-blank line with less indentation. Blank lines inside the block are
kept, a line that starts with a hash character inside the block is content, and the indentation is removed. Trailing
line feeds are removed, so `|` and `|-` give the same text. The forms `|+`, `|2`, and the folded `>` are errors, and a
block anywhere else is the error `block-scalar-not-allowed`.

### The Body

The body is Markdown. It must not contain a line that starts with `# ` outside a fenced block, because the title is
the first-level heading, and it must not contain the generated headings `## Cards`, `## Examples`, and `## See also`.
The test helpers report both as the warning `heading-reserved`. Give every fenced block a language so that readers and
tools can format it.

## Names, Aliases, and Tags

The name of a card is its file name. An alias is another name for the same card, so `tool help start` can print the
card `guide`. Aliases are listed in the key `aliases`, each alias is unique among its siblings and roots,
and the lookup ignores case. An alias of a translation is added to the aliases of the neutral file for the readers of
that culture.

The grammar of an alias depends on the file that declares it. An alias in a neutral file is a segment like a card name:
lower-case ASCII letters and digits in groups separated by single hyphens, 1 to 48 characters. An alias in a translation
may use any Unicode lower-case letters (and other letters) and digits in groups separated by single hyphens, 1 to 48
characters, and is normalized to Unicode form NFC; it never contains `/`, `:`, `#`, `|`, or white space.

Tags group cards across the tree. They are free-form segments by default. A catalog can declare a closed vocabulary
and a set of tags that every card must have at least one of; see the chapter *Composing Sources*. Readers filter by
tag with `catalog.Filter(...)` and with the option `--tag` of the command-line helper.

## Order and Children

Siblings are ordered in three steps. The entries of the key `children` of the parent come first, in the order listed.
The remaining siblings follow, ordered by the key `order`, where a card without `order` sorts after every card that
has one. Ties are broken by name. Each entry of `children` must exist and appear only once; the errors are
`children-unknown` and `children-duplicate`. The key `order` compares siblings of one source only. The roots of a
source are ordered by the `children` key of `_source.md` in the same way.

## See Also

The key `see-also` lists references to related cards, written as `localId` or `source:localId`. The rendered card
lists them under *See also*. A reference that does not resolve is the error `see-also-target`.

## Links

Links connect the text of one card to another card or example. All of them are checked when the catalog is built, and
a forward reference to a card that is defined later is valid.

| Link | Meaning |
| --- | --- |
| `[[build/compile]]` | The card with the local id `build/compile` |
| `[[build/compile#settings]]` | The example `settings` of that card, else of the one card below it that has it |
| `[[#settings]]` | The example `settings` of the card that holds the link, found in the same way |
| `[[other:build/compile\|the compile card]]` | A card of the declared source `other`, shown as the given text |
| `[[other:build/compile#settings\|the settings]]` | An example of a card of the source `other` |

Local links use the **local id** of the card, so they keep working when an application mounts your source under a
prefix. A bare link first looks for the id in the source of the card and then in the sources that it imports; if both
have the card, the link is ambiguous (`ambiguous-reference`) and needs the form `source:id`. Links with a source
name name a *declared* source, the `name` of its `_source.md`. They are the way to refer to a library that is not a
dependency of yours: when the source is absent from the application, the link degrades to its display text, and only
the validator can ask that every external source be present. When the source is present but has no such card, it is
an error. Renaming the declared name of a source breaks every external link that names it, so treat it as public
contract.

Links are recognized in the body, outside fenced blocks and inline code spans, so `` `[[x]]` `` is plain text. Text
that starts like a link but is not one is the error `link-syntax`. In an overlay, `[[#name]]` refers to the examples
of the card that the overlay extends. A link to an example finds the card's own example first; when the card has none
of that name and two or more cards below it do, the link is ambiguous (`ambiguous-reference`) and you write
`[[card#name]]` with the card that owns the example.

How a link is shown depends on the rendering. In plain text a card link reads ``the card `a/b` ``; in the command style
that the command-line helper uses it becomes the command that prints the card; in Markdown it is a Markdown link.

## Tokens

A token is a name in double braces, such as `{{tool}}` or `{{version}}`. The library replaces tokens with text when it
reads a file, in the title, the summary, the descriptions and contents of examples, and the body, including code
blocks and code spans. Token names are letters and digits and are case-sensitive; an unknown token is the error
`token-unknown` at its line.

Two tokens always exist: `tool` and `version`. They come from the builder, `ToolName` and `ToolVersion`; the tool
name defaults to the name of the first source and the version to nothing. Further tokens belong to one source and are
set when the source is created:

```csharp
var source = HelpSource.FromAssembly(typeof(MyLibHelp).Assembly)
    .WithTokens(new Dictionary<string, string> { ["maxItems"] = "42" });
```

The cards of that source write `{{maxItems}}`. A source sees only its own tokens, the tokens of the sources it imports,
and the two built-in tokens. A name defined twice in that scope is the error `token-redefined`, and the built-in names
cannot be redefined.

## Examples

An example is a named, ready-to-use piece of text attached to a card. Each item of `examples` has these keys.

| Key | Meaning |
| --- | --- |
| `name` | A segment, unique within the card (see below) |
| `kind` | `command`, `source`, or `snippet` |
| `language` | One lower-case word such as `shell`, `yaml`, or `csharp` |
| `description` | One line that says what the example shows |
| `file` | A data file next to the card that holds the text (schema 1: also the name to save it as) |
| `save-as` | Schema 2 only: the name to save the text of a `source` or `snippet` as |
| `content` | The text itself, a single line or a literal block |
| `exit` | A `command` only: the exit code the command is expected to end with, from 0 to 255 (default 0) |
| `expect` | A `command` only: text that the output must contain, a single line or a literal block |

The kinds differ in how they use `file`, `save-as`, and `content` (schema 2):

| Kind | `content` | `save-as` | `file` | Meaning |
| --- | --- | --- | --- | --- |
| `command` | Required, one line | Not allowed | Not allowed | One command line that starts with the tool name |
| `source` | Inline text | Required with `content` | Not with `content` | A complete file; inline, or read from `file` |
| `snippet` | Inline text | Required with `content` | Not with `content` | A fragment that never claims to work alone |

A `source` or `snippet` has inline `content` with `save-as`, or a `file` (and then an optional `save-as`, which defaults
to the last segment of `file`). `file` together with `content` is `value-invalid`, and so is a `save-as` or `file` on a
command; an example with neither `content` nor `file` is `key-missing`. Two source examples of one card that are
saved under the same name are the validator warning `save-as-duplicate`.

**Schema 1.** A source without a `schema` key (or with `schema: 1`) is read as it always was: there is no `save-as`, and
a `source` or `snippet` needs a `file`, which is the name to save the text as (the whole path, with its folders) and the
data file to read when there is no `content`. The key `save-as` in such a source is `key-unknown` and the message tells
you to add `schema: 2` to `_source.md`. Schema 1 stays supported by every 0.x and 1.x release; to migrate a source, add
`schema: 2`, rename `file` to `save-as` in each example that has both `file` and `content`, and give an example that
has only `file` a `save-as` with its old path when the folders of the old name matter (the new default is the last
segment of `file`). The schema belongs to one source, so a library migrates on its own.

The data file of a `source` or `snippet` without `content` is read from the folder of the card and must exist and be
valid, otherwise the catalog reports `example-file-missing` or `example-invalid`. File names use letters, digits,
dots, hyphens, and underscores, `/` separates folders, and `..`, a leading `/`, and a drive are not allowed. The text
of an example is normalized: CRLF becomes LF, a trailing line feed is removed, tokens are replaced, and a file is read
as UTF-8.

An example name is unique within its *card*, counting examples that overlays add: two examples of one card with the
same name are the error `duplicate-example`, while different cards, including a parent and its children, may use the
same name (so the example of every card can be called `settings` or `check`). A reader who asks a card for an example
by name finds the card's own example first; if the card has none of that name, the cards below it are searched, and the
name must then be unique among them (the command line and the agent tool list the qualified names `card#name` to choose
from when it is not). `card#name` always names one card.

**Beta note.** Before 0.1.0-beta.3 an example name had to be unique across a card and every card below it (the first
segment of its id). Cards that were valid stay valid with the same meaning; the new rule only allows more.

### Expected Results

A command example can state the result a correct run gives, so that a test can run every example of a card and compare.
`exit` is the expected exit code. `expect` is the text the output must contain: a single line, or a literal block in
which every non-blank line is one fragment. Every fragment must appear in the output, in any order, and the case is
compared as written. Tokens such as `{{tool}}` are replaced in `expect` like in the content.

```yaml
examples:
  - name: check-missing
    kind: command
    language: shell
    content: '{{tool}} check absent.yaml'
    exit: 2
    expect: |
      Cannot open absent.yaml
      1 error
    description: Check a file that does not exist.
```

The keys are additive and need no schema change. An `exit` or `expect` on a `source` or `snippet`, an `exit` that is not
an integer from 0 to 255, and an `expect` that is empty or starts with a blank line are the error `value-invalid`. The
examples document of the card prints the result after the example (the line `Expected exit code: 2` and one line
`Expected output contains: ...` per fragment), the JSON documents add `exitCode` and `expect` when they are stated, and
printing one example alone still prints exactly its content. HelpCards never starts a process: your own test runs the
command and compares the result.

## Cultures

A translation is a file with a culture suffix before the extension, for example `compile.de.md` or `compile.de-DE.md`.
The suffix is two or three letters and optional subtags. The culture of the catalog is chosen by the application with
`HelpCatalogBuilder.Culture`; the default is the invariant culture, which reads only the neutral files, and HelpCards
never reads the current culture of the thread.

For each card the loader tries the culture, then the culture with its last subtag removed, then the neutral file, so
`de-DE` tries `de-DE`, `de`, and the neutral file. The first existing file supplies the body, and a source never mixes
bodies. A translation can contain only `title`, `summary`, `aliases`, `examples` items with `name` and `description`,
and a body; any other key is `translation-key`. A missing title, summary, or example description falls back to the
neutral text one by one, and a translation with an empty body is ignored in favor of the neutral file. A translation
without a neutral file is the error `translation-orphan`. `HelpCard.Culture` is the culture of the file that supplied
the body, and for a neutral file it is the `language` of the source, `en` by default.

The headings and labels that HelpCards generates, such as *Cards* and *See also*, are English in this release, and the
keys of the JSON documents are never translated.
