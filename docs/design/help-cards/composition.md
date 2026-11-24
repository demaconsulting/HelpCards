## Composition Subsystem Design

![Composition Structure](CompositionView.svg)

### Overview

The Composition subsystem turns the definitions that Loading reads from the sources of a catalog into one frozen
tree. It is configured through `HelpCatalogBuilder`, registers and reads the sources, mounts them, resolves their
imports and overlays, runs the one composition pipeline, orders siblings, validates every reference and every name,
and collects all problems in one deterministic order. It never throws for a defect of a card; defects become
problems, and the Catalog subsystem turns the errors into one exception.

The subsystem contains these units:

- **HelpCatalogBuilder** (public) is the configuration face of the catalog creation (sources, culture, tool name and
  version, tag vocabulary, expected roots) and runs the pipeline. Its partial files hold the configuration, the
  pipeline steps, and the placement of cards in the tree.
- **SourceRegistry** registers the sources in registration order, reads each one through Loading, resolves imports,
  builds the token scope of each source, and looks up the cards that references name.
- **Mounter** gives every card its global id, creates the generated root card of each mounted source, and finds the
  clashes that two sources can cause.
- **OverlayMerger** applies the overlays of every source to the cards they extend.
- **ReferenceValidator** resolves card links, example links, external links, and see-also entries over the final tree
  in the scope of the source that wrote them, and checks that example names, names, and aliases are unique.
- **SiblingOrder** is the one comparator for the order of siblings and checks the `children` lists.
- **ProblemCollector** collects problems and hands them out in display order without duplicates.

`CompositionResult`, `SourceTree`, `CardBuild`, `SourceEntry`, `SourceRegistration`, `LookupKind`, and `LookupResult`
are immutable or build-scoped supporting types that are documented in the units that own them.

### Interfaces

| Interface | Direction | Purpose |
| --- | --- | --- |
| `HelpCatalogBuilder` settings methods | Inbound (adopters) | Add sources flat or mounted; set culture, tool, and rules |
| `HelpCatalogBuilder.Compose()` | Inbound (Catalog, Testing) | Run the pipeline; return `CompositionResult` |
| `CardReader.Read`, `TokenResolver.Create` | Outbound (Loading) | Read each source into definitions |
| `CatalogData`, `CardNode`, `ResolvedPart`, `ResolvedLink` | Outbound (Catalog) | The frozen result |
| `HelpProblem`, `HelpSeverity`, `RuleIds` | Outbound (Catalog) | The problems that every step reports |

The public signatures of `HelpCatalogBuilder` use only Base Class Library types and `HelpSource`. The builder is
created only by `HelpCatalog.Create` and by the validator of the Testing subsystem; it has no public constructor.

### Design

The pipeline runs these steps in order; every step runs on the data that survived the earlier ones.

1. **Validate builder input.** Argument errors were thrown when the settings were made; composing without a source is
   an argument error too, and never a catalog problem. Source names, imports, and schemas are checked by
   `SourceRegistry` while each source is read, in registration order: a duplicate name is `source-name-duplicate`, an
   import that is not registered is `import-missing`, one registered later is `import-order`, and one that is the
   importing source itself is `import-cycle`.
2. **Read the files** of each source through Loading, which lists them in ordinal order and groups them.
3. **Read the cards.** Loading builds the card definitions, replaces tokens, selects the culture variant of each file,
   and parses the bodies. The token scope of a source is its own tokens, the tokens of every source it imports
   (transitively), and the built-ins: `tool` is the tool name of the builder or else the name of the first source;
   `version` is the tool version or empty.
4. **Place the cards.** `Mounter` claims the global id of every card in registration order: a mounted source has the
   mount as prefix and a generated root card, a flat source keeps local ids. A card or mount whose id is taken is
   `collision`, a mount that is not a segment is `mount-invalid`, and the later holder is left out. In ordinal id order,
   each remaining card is placed below its parent, so a parent always comes first. A card whose parent card does not
   exist is reported once, as `tree-foreign` when the parent id is a card of another source and as `parent-missing`
   otherwise; cards below it, and cards below a file that failed to load, are left out without further problems.
   Siblings are put in order by `SiblingOrder`.
5. **Check the tree.** The `children` lists were checked while ordering; tags are checked against the optional
   vocabulary (`tag-unknown`, `tag-required`; generated mount roots have no tags and are exempt); the names and
   aliases of the roots of different sources must differ (`collision`). Example kinds and example files were checked
   by Loading.
6. **Apply overlays** with `OverlayMerger`: sources in registration order and, inside one source, overlays by ordinal
   file path. The target is `[source:]id`; a bare id is looked up in the source of the overlay and then in its imports.
   Title and summary replace those of the target, the body is appended after a blank line, and see-also entries and
   examples are appended. An optional overlay of a source that is not part of the catalog is skipped with the
   informational `overlay-optional-skipped`; any other overlay without a target is an error.
7. **Resolve references** in one pass over the final tree with `ReferenceValidator`: forward references are as valid
   as backward ones. Every text is resolved in the scope of the source that wrote it, so the body of an overlay uses
   its own source and imports while it is shown on the target card. `[[#name]]` searches the subtree of the card it
   ends up in (the target for an overlay); `[[card#name]]` the subtree of that card. An external link to a registered
   source resolves like a local link; a link to a source that is not present becomes its display text and an
   informational `external-source-absent` problem.
8. **Check uniqueness**: example names within a scope (the source of the target card and the first segment of its id,
   counting the examples that overlays add), names and aliases among siblings, ignoring case (`name-duplicate`), and the
   expected roots (`roots-mismatch`), which are the mount roots of mounted sources and the roots of flat sources.
9. **Freeze.** When no error exists, the nodes are put in depth-first order into `CatalogData`: the roots follow the
   registration order, a mounted source being its generated root with the cards below it; informational problems
   become its diagnostics. Warnings are never produced here; only the validator of the Testing subsystem produces them.

**Cascade suppression.** A file that failed to load, a card with no place in the tree, a card or source that was
left out, and a bare reference after an import that could not be resolved are kept as suppressed. A link, see-also
entry, overlay target, or `children` entry that names one of them is not reported again, so one defect yields one
problem.

**Failure model.** The pipeline never throws for a defect. All problems are collected, ordered by source (in
registration order), file, line, and rule, and returned with the data only when there is no error. The split of the
rules `collision` and `name-duplicate`: within one source, a repeated sibling or root name or alias is
`name-duplicate`; `collision` is for clashes between sources.

The unit design documents describe each unit in detail; the verification design describes how the subsystem is tested
through the builder on whole sources.
