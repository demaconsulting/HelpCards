## Composition Subsystem Design

![Composition Structure](CompositionView.svg)

### Overview

The Composition subsystem turns the definitions that Loading reads from the source of a catalog into one frozen
tree. It is configured through `HelpCatalogBuilder`, runs the one composition pipeline, orders siblings, validates
every reference and every name, and collects all problems in one deterministic order. It never throws for a defect of
a card; defects become problems, and the Catalog subsystem turns the errors into one exception.

The subsystem contains these units:

- **HelpCatalogBuilder** (public) is the configuration face of the catalog creation (source, culture, tool name and
  version, tag vocabulary, expected roots) and runs the pipeline. Its partial files hold the configuration, the
  pipeline steps, and the placement of cards in the tree.
- **ReferenceValidator** resolves card links, example links, external links, and see-also entries over the final tree
  and checks that example names, names, and aliases are unique.
- **SiblingOrder** is the one comparator for the order of siblings and checks the `children` lists.
- **ProblemCollector** collects problems and hands them out in display order without duplicates.

`CompositionResult` is an immutable supporting type of `HelpCatalogBuilder`: the frozen data (or nothing) with the
errors and all problems.

### Interfaces

| Interface | Direction | Purpose |
| --- | --- | --- |
| `HelpCatalogBuilder` settings methods | Inbound (adopters) | Add the source; set culture, tool, and rules |
| `HelpCatalogBuilder.Compose()` | Inbound (Catalog, Testing) | Run the pipeline; return `CompositionResult` |
| `CardReader.Read`, `TokenResolver.Create` | Outbound (Loading) | Read the source into definitions |
| `CatalogData`, `CardNode`, `ResolvedPart`, `ResolvedLink` | Outbound (Catalog) | The frozen result |
| `HelpProblem`, `HelpSeverity`, `RuleIds` | Outbound (Catalog) | The problems that every step reports |

The public signatures of `HelpCatalogBuilder` use only Base Class Library types and `HelpSource`. The builder is
created only by `HelpCatalog.Create` and by the validator of the Testing subsystem; it has no public constructor.

### Design

The pipeline runs these steps in order; every step runs on the data that survived the earlier ones.

1. **Validate builder input.** Argument errors were thrown when the settings were made; composing without a source is
   an argument error too, and never a catalog problem.
2. **Read the files** of the source through Loading, which lists them in ordinal order and groups them.
3. **Read the cards.** Loading builds the card definitions, replaces tokens (the built-in `tool` is the tool name of
   the builder or else the source name; `version` is the tool version or empty), selects the culture variant of each
   file, and parses the bodies.
4. **Place the cards.** In ordinal id order, each card is placed below its parent, so a parent always comes first. A
   card whose parent card does not exist is reported once as `parent-missing`; cards below it, and cards below a file
   that failed to load, are left out without further problems. Siblings are put in order by `SiblingOrder`.
5. **Check the tree.** The `children` lists were checked while ordering; tags are checked against the optional
   vocabulary (`tag-unknown`, `tag-required`). Example kinds and example files were checked by Loading.
6. **Apply overlays** is a reserved step. This release reads and validates overlay files in Loading but does not merge
   them, so an overlay has no effect on the catalog. The step is one empty method, the single place where merging will
   be added together with the registration of more than one source.
7. **Resolve references** in one pass over the final tree with `ReferenceValidator`: forward references are as valid
   as backward ones. `[[#name]]` searches the subtree of the card it is written in; `[[card#name]]` the subtree of
   that card. An external link to the registered source resolves like a local link; a link to a source that is not
   present becomes its display text and an informational `external-source-absent` problem.
8. **Check uniqueness**: example names within a scope (the source and the first segment of the owning id), names and
   aliases among siblings and roots, ignoring case (`name-duplicate`), and the expected roots (`roots-mismatch`).
9. **Freeze.** When no error exists, the nodes are put in depth-first order into `CatalogData`; informational problems
   become its diagnostics. Warnings are never produced here; only the validator of the Testing subsystem produces them.

**Cascade suppression.** A file that failed to load, and a card with no place in the tree, are kept in a suppressed
set. A link, see-also entry, or `children` entry that names one of them is not reported again, so one broken file
yields one problem.

**Single source seam.** `AddSource` accepts exactly one source without imports; a second source or a source with
imports is an `ArgumentException`. Registering more sources, mounting, merging overlays, and import scopes are
additions to steps 1, 4, 6, and 7 and do not change the shape of the pipeline.

**Failure model.** The pipeline never throws for a defect. All problems are collected, ordered by source, file, line,
and rule, and returned with the data only when there is no error. The split of the rules `collision` and
`name-duplicate`: within one source, a repeated sibling or root name or alias is `name-duplicate`; `collision` is
reserved for clashes between sources.

The unit design documents describe each unit in detail; the verification design describes how the subsystem is tested
through the builder on whole sources.
