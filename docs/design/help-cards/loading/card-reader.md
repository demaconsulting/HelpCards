### CardReader Design

![Loading Structure](LoadingView.svg)

#### Purpose

`CardReader` reads one help source completely: it classifies the files, decodes them, checks the key matrix and
the value grammars, replaces tokens, parses bodies, selects translations, and returns definitions together
with every problem it found. It exists so that all rules of the card format are applied in one place, in a
deterministic order, and so that one run reports all defects of a source.

#### Data Model

All definitions are immutable records:

- `SourceDefinition`: name, source file, title, summary, language, schema, aliases, children, extensions,
  landing body, cards, overlays, failed ids, and problems.
- `CardDefinition`: source name, local id, file, title, summary, aliases, tags, order, children, see-also,
  examples, extensions, culture, and body parts.
- `OverlayDefinition`: the extended reference, the optional flag, optional title and summary, see-also,
  examples, extensions, culture, and body parts.
- `TranslationDefinition`: culture suffix, optional title and summary, extra aliases, example descriptions,
  and body parts.
- `ExampleDefinition`: name, kind, language, file name, description, normalized content, and where it was
  written.
- `ReferenceDefinition`: optional source name and local id of a see-also or extends reference.
- `LocatedValue`: a string value with the file and line it was written at.

Titles and summaries of the source have their defaults applied (the name, and `Help cards of {name}.`). The
language of a source defaults to `en` and the schema to 1.

#### Key Methods

**`Read(source, culture, resolverFactory)`** creates a reader for one read and runs the steps of the Loading
design: list, classify, read `_source.md`, create the token scope, read every card and overlay in ordinal
order, validate translations, and report orphans. The class is split into partial files by concern (files and
grammars, values and the key matrix, examples, cards and translations, and the source file).

- **Classification.** A card file is `<segment>.md` in folders that are segments (at most eight segments in
  total); a translation is `<segment>.<culture>.md`; `_source.md` and `_source.<culture>.md` only at the root;
  a data file matches `^[A-Za-z0-9][A-Za-z0-9._-]*$` and does not end in `.md`. Other names are `file-name`.
- **Key matrix.** Cards allow title, summary (both required), aliases, tags, order, children, see-also,
  examples; overlays (files with `extends`) allow extends, optional, title, summary, see-also, examples;
  `_source.md` allows title, summary, aliases, children, schema, name, language; extension keys `x-*` are
  allowed except in translations; translations allow title, summary, aliases (not for overlays), examples
  with name and description only, and report other keys as `translation-key`.
- **Values.** Names, tags, aliases, and children are segments; `order` is an integer of at most seven digits;
  references are `[source:]localId`; examples follow the kind matrix (command: one-line content, no file;
  source and snippet: a file and optional content). Example files are resolved relative to the folder of the
  card or overlay file and are read only when no inline content is given.
- **Cultures.** All translations are read and validated; `CultureSelector` picks one per file; title, summary,
  and example descriptions fall back individually; a translation without a body is ignored completely.
- **Source name.** A name given with `WithName` wins over `name` in `_source.md`; a missing or malformed name
  is `source-invalid`. A schema above 1 is `schema-unsupported` and no card is read.
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
