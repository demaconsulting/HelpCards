# Composing Sources

An application that uses several libraries, each with its own cards, composes their sources into one catalog. This
chapter describes the builder, mounts, imports, overlays, collisions, views, and the way problems are reported.

## The Builder

`HelpCatalog.Create` takes a configuration callback and returns an immutable catalog:

```csharp
var catalog = HelpCatalog.Create(builder => builder
    .AddMountedSource(CanvasHelp.Source)
    .AddMountedSource(PlexusHelp.Source, "plexus")
    .AddSource(AppHelp.Source)
    .ToolName("myapp")
    .ToolVersion("2.1.0")
    .Culture(CultureInfo.GetCultureInfo("de"))
    .TagVocabulary(["tutorial", "reference"], requireOneOf: ["tutorial", "reference"])
    .ExpectedRoots(["canvas", "plexus", "getting-started"]));
```

| Method | Effect |
| --- | --- |
| `AddSource(source)` | Adds the source flat: its cards keep their local ids |
| `AddMountedSource(source, mount)` | Adds the source below one root card; the default mount is the source name |
| `Culture(culture)` | Chooses the culture of the translations; the default is the invariant culture |
| `ToolName(name)`, `ToolVersion(version)` | Set the built-in tokens `tool` and `version` |
| `TagVocabulary(allowed, requireOneOf)` | Declares the closed set of tags and the tags of which each card needs one |
| `ExpectedRoots(ids)` | Requires that the global root ids equal this set |

The order of the calls matters. It decides the order of the roots, the order in which overlays apply, and the order in
which imports must appear.

## Mounts

A mounted source gets a generated root card whose id is the mount. Its title, summary, aliases, and body come from
`_source.md`; when they are missing the title is the name of the source and the summary is `Help cards of {name}.`.
The cards of the source sit below that root, and every global id gets the mount as its first segment, so the card
`graph/layout` of the source mounted as `plexus` is `plexus/graph/layout`. A mount is exactly one segment. Flat
sources keep their local ids.

Global ids appear in every result: in renderings, links, search results, and JSON. Authored text never uses them,
because cards link by local id, so you can change a mount without editing a card. A mount is therefore the application's
decision and a source name is the library's.

## Imports

A tool and its own companion library often share tokens and refer to each other's cards. `Imports` declares that one
source belongs to the same product as another:

```csharp
public static HelpSource Source { get; } = HelpSource.FromAssembly(typeof(ToolHelp).Assembly)
    .WithTokens(new Dictionary<string, string> { ["schemaVersion"] = "3" })
    .Imports(LibraryHelp.Source);
```

An import has these effects, and the catalog validates all of them:

- The imported source must be added to the builder before the source that imports it (`import-order`), found by its
  declared name (`import-missing`). Imports are transitive, and a cycle is `import-cycle`.
- The importer sees the tokens of the imported source, so `{{maxDpi}}` works in its cards. A token with the same name
  in both is `token-redefined`.
- A bare link, see-also entry, or `extends` reference resolves in the importer first and then in the imports in
  declaration order. An id found in both places is `ambiguous-reference`; write `source:id` to choose.
- Mounting is independent. The importer and the imported source can be flat or mounted in any way.

An import is not a dependency of your project. For a library that you do not import, use the external link
`[[source:id|text]]`; it degrades to the text when the source is absent from the application.

## Overlays

An overlay is a card file that adds to a card of another source, or of its own. It has the key `extends` and little
else:

```markdown
---
extends: mermaid
summary: Mermaid diagrams, with the settings of the tool.
see-also:
  - schema
examples:
  - name: tool-render
    kind: command
    language: shell
    content: '{{tool}} render flow.mmd'
    description: Draw a flowchart.
---
The tool draws every diagram type of the library.
```

`extends` takes a reference, `localId` or `source:localId`. The file name of the overlay only has to be a valid id that
is unique in its source, for example `overlay-mermaid.md`. The overlay applies in this order: sources in registration
order, then the overlays of one source in the order of their file paths. A title or summary replaces the earlier one,
the body is appended after a blank line, see-also entries are appended without duplicates, and examples are appended.
Overlays cannot extend the generated root card of a mount.

An overlay whose target does not exist is the error `overlay-target`. With `optional: true` and a target source that is
not registered, the overlay is skipped and the catalog records the information `overlay-optional-skipped`; that lets a
tool add details about a library that an application may not include. A card of one source never sits below a card of
another source (`tree-foreign`); sources contribute to each other only through overlays.

## Collisions

Nothing shadows silently. These are errors that name both sources and files:

- A duplicate global id, a mount that another mount or root card already uses, or a name or alias that a sibling or
  root of another source already uses (`collision`); the same clash inside one source is `name-duplicate`
- A duplicate source name (`source-name-duplicate`)
- A mount that is not a valid single segment (`mount-invalid`)
- Two examples of one card with the same name (`duplicate-example`)
- A set of root ids that differs from `ExpectedRoots` (`roots-mismatch`)

To fix a name clash between two libraries, rename the card or the declared source name in one of them, or mount one of
the sources under another name.

## The Catalog

The catalog and every object it returns are immutable and safe to use from several threads.

| Member | Meaning |
| --- | --- |
| `Roots`, `Cards` | The root cards, and all cards depth first |
| `Sources`, `TagNames` | Information about the sources, and the union of all tags |
| `Find(path)` | The card for a path such as `build compile` or `build/compile`, or `null`; aliases work |
| `Resolve(path)` | The same walk with the details of a failure: the unknown segment, valid names, a suggestion |
| `GetChildren(card)`, `GetSeeAlso(card)` | The visible children and related cards in order |
| `FindExamples(name, scopeCardId)`, `FindExample(card, name)` | Examples by name, optionally in a subtree |
| `Search(query, options)`, `Suggest(word, max)` | Ranked search and spelling suggestions |
| `Diagnostics` | The informational problems of the build |
| `Render`, `RenderIndex`, `RenderExamples`, `RenderExample`, `RenderSearch` | Text, Markdown, or JSON |
| `RenderCardList`, `FormatBody` | A list of cards, and the body of one card with its links written |

A lookup is strict: the local id of a mounted source does not resolve, and the global id does. The path is split on
slashes and white space, and the first segment matches a root name or alias.

### Views

A view is a new catalog over the same data that shows only some cards:

```csharp
HelpCatalog tutorials = catalog.Filter(includeTags: ["tutorial"]);
HelpCatalog onlyMine = catalog.FilterSource(includeSources: ["myapp"]);
HelpCatalog withoutDraft = catalog.Without(["plexus/draft"]);
HelpCatalog custom = catalog.Where(card => card.Extensions.GetValueOrDefault("x-audience") != "internal");
```

A predicate of `Where` is evaluated once per card, immediately, on the calling thread, and is not kept. Views of views
combine by intersection. A hidden parent does not hide its children: the nearest visible descendants move up to the
nearest visible ancestor. `Find` returns `null` for a hidden card, see-also entries of hidden cards disappear, and a
link to a hidden card is written as the title of the card. An unknown source, an unknown tag in a closed vocabulary, or
an unknown id passed to `Without` is an `ArgumentException`.

### Search and Suggestions

`catalog.Search("install tool")` ranks the cards by how well their aliases, ids, example names, titles, tags, and
summaries match the words of the query. A word loses a trailing `s` when the language is English, an exact match
scores more than a prefix match, and a card that matches every word gets a bonus. The result lists the id, the title,
the summary, the score, and the fields that matched. `HelpSearchOptions` limits the number of results, the sources, and
the tags. `catalog.Suggest("instal")` returns up to three names, aliases, or ids that are within a small edit distance
of the word.

## Problems and Exceptions

The loader and the composition pipeline collect the problems they find instead of stopping at the first one. A problem
has a severity, a rule id, the source name, the file, the line, and a message. `HelpCatalog.Create` throws one
`HelpCatalogException` when any error exists; its `Problems` list holds the errors in a fixed order (source
registration order, file path, line, rule id), and its message names the first one and the number of the others.

The checks run in stages, so the list is not always complete. Every file is checked on its own, so the errors of
different files appear together. Within one file, a `syntax` error in the front matter hides every other finding of that
file; a key or value error (`key-unknown`, `key-missing`, `value-invalid`) still lets `token-unknown` and `link-syntax`
of the body appear, but the checks that need the finished card (`link-target`, `example-target`, `see-also-target`) are
skipped for that file, and a link to such a file is not reported. Warnings of the validator appear only when there is
no error. After you fix the listed errors, run again: more can show up. A source without a name is the error
`source-invalid`; the other files of the catalog are still checked.

The rules that `Create` reports as errors are:

- Files and keys: `file-name`, `syntax`, `key-unknown`, `key-missing`, `value-invalid`, `block-scalar-not-allowed`,
  `source-read`, `source-invalid`, `schema-unsupported`
- Tokens and links: `token-unknown`, `token-redefined`, `link-syntax`, `link-target`, `see-also-target`,
  `example-target`, `extends-target`, `ambiguous-reference`
- Structure: `parent-missing`, `children-duplicate`, `children-unknown`, `collision`, `name-duplicate`,
  `tree-foreign`, `overlay-target`, `roots-mismatch`, `tag-unknown`, `tag-required`
- Examples: `example-invalid`, `example-file-missing`, `duplicate-example`
- Composition: `source-name-duplicate`, `mount-invalid`, `import-order`, `import-missing`, `import-cycle`
- Cultures: `translation-orphan`, `translation-key`, `culture-invalid`

The information problems that a successful `Create` reports in `Diagnostics` are `external-source-absent` and
`overlay-optional-skipped`. Warnings about quality come only from the test helpers, never from `Create`.

The library throws `ArgumentException` and `ArgumentNullException` for wrong arguments, and `HelpCatalogException` for
defective sources. A file that cannot be read is reported as `source-read` with the type and message of the failure.
Callbacks that you pass in, such as a link formatter or a `Where` predicate, run on the calling thread, can be called
from several threads at once, and must be thread-safe and free of side effects when the catalog is shared.
