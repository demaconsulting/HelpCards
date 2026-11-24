### HelpCatalogBuilder Design

![Composition Structure](CompositionView.svg)

#### Purpose

`HelpCatalogBuilder` is the one public type of the Composition subsystem. It receives the configuration of a catalog
(the sources, flat or mounted, the culture, the tool name and version, the tag vocabulary, and the expected roots), runs
the one composition pipeline over the sources, and returns the frozen data together with every problem. It exists so
that building a catalog is one fixed sequence of steps whose behavior does not depend on who calls it.

#### Data Model

| Member | Type | Meaning |
| --- | --- | --- |
| `_registrations` | list of `SourceRegistration` | The registered sources in order, flat or mounted |
| `_sources` | list of `HelpSource` | The same sources, for the validator |
| `_culture` | `CultureInfo` | The culture whose translations are used; invariant by default |
| `_toolName`, `_toolVersion` | nullable strings | The values of the built-in tokens `tool` and `version` |
| `_allowedTags`, `_requiredTags` | sets | The closed tag vocabulary and the tags of which a card needs one |
| `_expectedRoots` | set | The ids that the roots must equal, or none |

Supporting types: `CompositionResult` (the frozen data or nothing, the errors, and all problems, as an immutable
record), `SourceTree` (the placed cards of one source, their ordered children, and the suppressed ids), and
`CardBuild` (a card while overlays are merged into it). The class is
partial: `HelpCatalogBuilder.cs` holds the configuration, `HelpCatalogBuilder.Pipeline.cs` the steps, and
`HelpCatalogBuilder.Tree.cs` the placement of cards and the tag check. The constructor is internal, so the builder is
created only by `HelpCatalog.Create` and by the validator.

#### Key Methods

- **`AddSource(source)`** registers a source whose cards keep their local ids. A null source is
  `ArgumentNullException`. Any number of sources, with or without imports, can be added; imports are checked when the
  catalog is composed. Nothing is read.
- **`AddMountedSource(source, mount)`** registers a source below a generated root card. The mount defaults to the
  declared name of the source; an empty or white-space mount is `ArgumentException`, and a mount that is not a segment
  is the problem `mount-invalid` when the catalog is composed.
- **`Culture`, `ToolName`, `ToolVersion`** set the culture and the two built-in tokens. Null or white space is an
  argument error; the version may be empty.
- **`TagVocabulary(allowed, requireOneOf)`** closes the tag set; every tag must be a segment and every required tag
  must be allowed. **`ExpectedRoots(ids)`** sets the expected roots, each a single segment.
- **`Sources`** (internal) lists the registered sources in registration order, for the validator.
- **`Compose()`** (internal) runs the pipeline: it reads every source with `SourceRegistry`, claims global ids with
  `Mounter`, places the cards of each source (`PlaceCards`), checks the tags, applies overlays with `OverlayMerger`,
  creates the nodes (the generated roots of mounted sources included), resolves references with `ReferenceValidator`,
  checks uniqueness and the expected roots, and freezes `CatalogData` when no error exists. The expected roots are
  the mount roots and the roots of the flat sources; the check counts a root that failed to load as present and is
  skipped when a source could not be read (no name, unsupported schema, or the listing failed) or was left out.

#### Error Handling

Argument errors throw at once from the configuration methods and from `Compose` when no source was added. Every
defect of the source is a problem: the builder collects the problems of Loading and of its own steps in a
`ProblemCollector` and returns them; it never throws for them. The builder does not catch exceptions from the file
provider; Loading turns read failures into `source-read` problems.

#### Dependencies

- Loading: `HelpSource` and the definition types.
- Composition: `SourceRegistry`, `Mounter`, `OverlayMerger`, `ReferenceValidator`, `SiblingOrder`, `ProblemCollector`.
- Catalog: `CatalogData`, `CardNode`, `ResolvedPart`, `ResolvedLink`, `HelpExample`, `HelpSourceInfo`, `HelpProblem`,
  `HelpSeverity`, and `RuleIds`.

#### Callers

`HelpCatalog.Create` calls the configuration callback with a builder and then `Compose`; the validator of the Testing
subsystem does the same without throwing. Adopters call the configuration methods inside their callback.
