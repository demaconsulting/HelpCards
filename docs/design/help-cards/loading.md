## Loading Subsystem Design

![Loading Structure](LoadingView.svg)

### Overview

The Loading subsystem turns the files of one help source into definitions. It is the only part of the
library that touches a file system, an assembly, or text on behalf of a source, and it does so only when a
catalog is built. Its responsibilities are to find the files, decode their front matter, read cards,
overlays, translations, and the source description, replace tokens, choose the culture variant of each file,
split bodies into text and links, and report every defect as a problem.

The subsystem contains these units:

- **HelpSource** (public) describes where the cards of a library or tool come from. It is an immutable value
  with a declared name, token values, and imports, and creating it performs no I/O.
- **HelpFileProvider** is the seam that lists and reads files. It has three implementations: assembly
  resources, in-memory text, and a directory that only the validator's embedded-versus-disk comparison uses.
- **FrontMatterDecoder** decodes the restricted subset of YAML front matter and splits off the body.
- **CardReader** classifies the files of a source and reads cards, overlays, translations, examples, and
  `_source.md` into definitions, using the units below.
- **TokenResolver** replaces `{{name}}` tokens in the scope of a source.
- **CultureSelector** chooses the culture variant of a file for a requested culture.
- **BodyParser** splits a body into text parts and link parts.

The definitions (`SourceDefinition`, `CardDefinition`, `OverlayDefinition`, `TranslationDefinition`,
`ExampleDefinition`, `ReferenceDefinition`, `LocatedValue`, `BodyPart`, `ParsedLink`) and the decoder results
(`FrontMatterDocument`, `FrontMatterEntry`, `FrontMatterItem`, `FrontMatterScalar`) are immutable supporting
types that belong to the unit that produces them; they carry data and no behavior.

### Interfaces

The subsystem exposes one public type, `HelpSource`, whose signatures use only Base Class Library types, so it
can appear in the public API of any library. Everything else is `internal` and is used by the Composition
subsystem:

| Interface | Direction | Purpose |
| --- | --- | --- |
| `HelpSource` factories and `With...` methods | Inbound (adopters) | Describe a source without reading it |
| `CardReader.Read(source, culture, factory)` | Inbound (Composition) | Read a source into a `SourceDefinition` |
| `TokenResolver.Create(...)` | Inbound (Composition) | Build the token scope of a source from its imports |
| `CardReader.IsSegment`, `CardReader.IsLocalId` | Inbound (Composition) | Shared grammar checks for names and ids |
| `TokenResolver.FindNames(text)` | Inbound (Testing) | Find the token names in a text, for the validator |
| `BodyParser.TryGetFenceOpen`, `IsFenceClose` | Inbound (Testing) | Fence rules shared with the validator |
| `HelpProblem` records in `SourceDefinition.Problems` | Outbound | Report every defect found |

The subsystem depends on the problem model (`HelpProblem`, `HelpSeverity`, and the rule identifiers) of the
Catalog subsystem and on nothing else outside the Base Class Library.

### Design

A read runs in the order below; every step adds problems to one list and never throws for a defect of a card.

1. **List and classify.** `CardReader` asks the provider for the files and sorts them: `_source.md` and its
   translations at the root, card files, translation files, overlays (decided later by the `extends` key),
   and data files. A name that fits no pattern is a `file-name` problem.
2. **Read the source file.** `_source.md` is decoded first because the name of the source decides the token
   scope. A name set with `HelpSource.WithName` wins over the `name` in the file. A schema newer than the
   highest supported ends the read with a `schema-unsupported` problem and no cards.
3. **Build the token scope.** The caller supplies a factory that turns the resolved name into a
   `TokenResolver`; Composition builds it from the tokens of the source, of its imports, and the built-ins.
4. **Read each card and overlay file** in ordinal id order: decode the front matter, check the key matrix,
   validate every value, replace tokens, read examples (inline content or the file next to the card), and
   split the body with `BodyParser`.
5. **Read translations.** Every translation file of a stem is validated, whether or not its culture is
   requested; `CultureSelector` then picks the most specific one of the culture chain for that file and the
   reader applies it with individual fallback.
6. **Report orphans.** A translation without a neutral file is a `translation-orphan` problem.

A file whose front matter cannot be read, or whose structure is invalid, contributes nothing and its local id
is returned in `FailedIds` so that later steps do not report references to it again (cascade suppression).
Defects in the body, in tokens, in links, and in example content are collected but do not drop the card.

The unit design documents describe each unit in detail; the verification design describes how the subsystem
is tested through its public face and the reader.
