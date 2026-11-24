### CardReader Design

![Loading Structure](LoadingView.svg)

#### Purpose

`CardReader` reads one help source completely: it classifies the files, decodes them, checks the key matrix and
the value grammars, replaces tokens, parses bodies, selects translations, and returns definitions together
with every problem it found. It exists so that all rules of the card format are applied in one place, in a
deterministic order, and so that one run reports all defects of a source.

#### Data Model

All definitions are immutable records:

- `SourceDefinition`: name, source file, title, summary, language (the culture of the neutral files), schema, aliases,
  children, extensions, culture (of the file that supplied the landing body), landing body and the line it starts on
  (`BodyLine`), cards, overlays, failed ids, and problems.
- `CardDefinition`: source name, local id, file, title, summary, aliases, tags, order, children, see-also,
  examples, extensions, culture, body parts, and the file that supplied the body (a translation file when one was
  selected), so a problem in a link can name the right file.
- `OverlayDefinition`: the extended reference, the optional flag, optional title and summary, see-also,
  examples, extensions, culture, body parts, and the file that supplied the body.
- `TranslationDefinition`: culture suffix, the file it was read from, optional title and summary, extra aliases,
  example descriptions, `HasBody` (a translation without a body is ignored completely), the line its body starts on
  (`BodyLine`), and body parts.
- `ExampleDefinition`: name, kind, language, file name, description, normalized content, the expected exit code
  and expected output fragments of a command (0 and none when not stated), and where it was written.
- `ReferenceDefinition`: optional source name and local id of a see-also or extends reference.
- `LocatedValue`: a string value with the file and line it was written at.

Titles and summaries of the source have their defaults applied (the name, and `Help cards of {name}.`). The
language of a source defaults to `en` and the schema to 1. The reader remembers the schema of the source for the files
it reads afterwards, because the keys of an example depend on it.

#### Key Methods

**`Read(source, culture, resolverFactory)`** creates a reader for one read and runs the steps of the Loading
design: list, classify, read `_source.md`, create the token scope, read every card and overlay in ordinal
order, validate translations, and report orphans. The class is split into partial files by concern (files and
grammars, values and the key matrix, examples, cards and translations, and the source file).

- **Classification.** A card file is `<segment>.md` in folders that are segments (at most eight segments in
  total); a translation is `<segment>.<culture>.md`; `_source.md` and `_source.<culture>.md` only at the root;
  a data file matches `^[A-Za-z0-9][A-Za-z0-9._-]*$` and does not end in `.md`. Other names are `file-name`.
- **Key matrix.** Cards allow title, summary (both required), aliases, tags, order, children, see-also,
  examples (whose items allow name, kind, language, description, file, content, exit, and expect); overlays (files with
  `extends`) allow extends, optional, title, summary, see-also, examples;
  `_source.md` allows title, summary, aliases, children, schema, name, language; extension keys `x-*` are
  allowed except in translations; translations allow title, summary, aliases (not for overlays), examples
  with name and description only, and report other keys as `translation-key`.
- **Values.** Names, tags, aliases, and children are segments; `order` is an integer of at most seven digits;
  references are `[source:]localId`; examples follow the kind matrix (command: one-line content, no file;
  source and snippet: see the schemas below). Data files are resolved relative to the folder of the card or overlay
  file and are read only when no inline content is given.
- **Schemas of an example.** Schema 1 (and a source without `schema`) is read exactly as before: a source or snippet
  needs `file`, which is both the data file to read when there is no `content` and the name to save the text as (the
  whole path, so `ExampleDefinition.FileName` keeps its folders). Schema 2 splits the two: `save-as` is the name to
  save as and `file` is a companion data file. A source or snippet has either inline `content` with a required
  `save-as`, or a `file` with an optional `save-as` that defaults to the last segment of `file`; `file` together with
  `content`, and neither of them, are errors, and a command has neither key. Both names use the grammar of data file
  names (relative, `/`-separated, no `..`, not ending in `.md`). In schema 1 the key `save-as` is `key-unknown`
  with a message that names schema 2 and what `file` means in schema 1; the generic list of allowed keys of an
  unknown key is the list of the schema in force. The rules apply to the examples of cards and overlays alike.
- **Expected results.** A command may have `exit` (an integer from 0 to 255, default 0) and `expect` (one line, or a
  literal block of which every non-blank line is one fragment). Line endings become LF and tokens are replaced in
  `expect` as in the content. `exit` or `expect` on a source or snippet, an `exit` outside 0 to 255, and an empty
  `expect` or one that starts with a blank line are `value-invalid` at the line of the key. Both keys are additive:
  they need no schema change and are read in every schema.
- **Cultures.** All translations are read and validated; `CultureSelector` picks one per file; title, summary,
  and example descriptions fall back individually; a translation without a body is ignored completely.
- **Source name.** A name given with `WithName` wins over `name` in `_source.md`; a missing or malformed name
  is `source-invalid`. A schema above `HighestSchema` (2) is `schema-unsupported` and no card is read. A name given
  with `WithName` is not checked against the `name` of `_source.md`, which is ignored in that case.
- **Source language.** The `language` of `_source.md` must be a culture name of the grammar of 6.1 (a 2 or 3 letter
  language and subtags of 2 to 8 letters or digits). A value that is not is the problem `culture-invalid` at the
  line of the key, and the language falls back to `en`; the culture is never looked up in the operating system, so
  the check does not depend on the globalization mode.
- **Front-matter edges.** Duplicate keys, nesting deeper than a list of maps, and document markers are decoded as
  `syntax` errors by `FrontMatterDecoder` (see its design), so a file with one of them contributes nothing and its id
  is in `FailedIds`; a second `---` after the closing one is body text and is not an error.
- **Example data files.** A data file that several examples name is read once; the text is cached by path for the read.
- **Shared grammar checks** `IsSegment` and `IsLocalId` are internal static helpers that Composition reuses
  for mounts and source names.

#### Error Handling

No defect of a source is thrown. Every problem is added to one list and returned in `SourceDefinition.Problems`.
Problems that make a file unusable (syntax, key, and value problems) count as structure errors: the file
contributes nothing and its local id is listed in `FailedIds`. Token, link, and example-content problems keep
the card. A failure of the provider to list or read becomes a `source-read` problem that carries the exception
type and message; only fatal exceptions (out of memory, stack overflow, access violation) are not caught.
Argument defects of `Read` throw `ArgumentNullException`.

#### Dependencies

- `HelpSource` and its `HelpFileProvider` for the files.
- `FrontMatterDecoder`, `TokenResolver`, `CultureSelector`, and `BodyParser`.
- `HelpProblem` and `RuleIds` of the Catalog subsystem.

#### Callers

`HelpCatalogBuilder` (Composition) reads every registered source; the validator reuses it through the builder.
