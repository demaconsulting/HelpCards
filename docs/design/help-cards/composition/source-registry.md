### SourceRegistry Design

![Composition Structure](CompositionView.svg)

#### Purpose

`SourceRegistry` knows which sources a catalog has. It reads them in registration order, decides the name of each,
resolves the imports that connect them, builds the token scope of each source, and answers the one question that every
reference asks: which card does this id name, as seen from this source?

#### Data Model

The registry holds one `SourceEntry` per registration (the index, the registration with its mount, the declared name,
the `SourceDefinition` that Loading read, the resolved imports and their closure, the placed cards, and the flags
`Excluded` and `BrokenImports`), the entries by declared name (ignoring case), and the problems found, each with the
index of its source. `SourceRegistration` is the immutable record of what the adopter registered. `LookupResult` and
`LookupKind` are the outcome of a lookup: found, not found, ambiguous, source absent, or suppressed.

#### Key Methods

- **`Read(registrations, culture, toolName, toolVersion)`** reads every source once in registration order through
  `CardReader`. For each source it resolves the imports and the token scope as soon as the name is known, copies the
  problems of Loading, and registers the name; a name that an earlier source already uses is
  `source-name-duplicate` and excludes the later source.
- **Import resolution.** An import is matched to a registered source when both read the same files (a copy made with
  `WithName`, `WithTokens`, or `Imports` shares them) or when the import has a declared name that equals the name of a
  registered source. A match that is the importing source is `import-cycle`. An import that no earlier source matches is
  classified once every source has been read, because a later source may take its name from its `_source.md`, which is
  known only after it was read: a later source that matches is `import-order`, no match at all is `import-missing`. Such
  an import is not used and marks the source as having a broken import. The imports of a source are listed together with
  the imports of those imports, each once, in search order.
- **Token scope.** Tokens come from what the source declares it imports (transitively, each source once) rather than
  from the registration, so a misplaced or missing import does not also make its tokens unknown. A name defined twice,
  or a built-in redefined, is `token-redefined`.
- **`Lookup(from, sourceName, localId)`** answers a reference. A qualified reference is looked up only in the named
  source; a source that is not registered gives `SourceAbsent`. A bare id is looked up in the source itself and then in
  its imports in search order; an id that the source and an import both have is `Ambiguous`. A card that failed to
  load, a source that was excluded, and a bare id that matches nothing in a source with a broken import are
  `Suppressed`, because the cause is already a problem.
- **`FindByName(name)`** finds a registered source by its declared name, ignoring case.

#### Error Handling

Null arguments throw `ArgumentNullException`. Every defect is a problem; the registry never throws for one and does
not catch exceptions of the file provider, which Loading turns into `source-read` problems.

#### Dependencies

Loading (`CardReader`, `HelpSource`, `TokenResolver`, `SourceDefinition`), Composition (`CardBuild`, `SourceTree`),
and the problem model of Catalog.

#### Callers

`HelpCatalogBuilder` creates the registry in step 1 to 3 and passes it to `Mounter`, `OverlayMerger`, and
`ReferenceValidator`, which ask it for sources and lookups.
