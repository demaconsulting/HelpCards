### Mounter Design

![Composition Structure](CompositionView.svg)

#### Purpose

`Mounter` decides where the cards of each source live in the catalog. A source added flat keeps its local ids; a source
added mounted gets a generated root card and global ids prefixed with its mount. The unit also finds the clashes that
two sources can cause, so that nothing shadows another card silently.

#### Data Model

The class is static and stateless. It works on the `SourceEntry` objects of the registry and records problems in the
`ProblemCollector`. The only state it changes is on the entries: `Excluded` for a source that cannot take part and
`Dropped` for card ids that another source already holds.

#### Key Methods

- **`GlobalId(entry, localId)`** returns the local id for a flat source and `mount/localId` for a mounted one. The
  mount is the one given to `AddMountedSource`, or else the declared name of the source.
- **`Register(registry, problems)`** visits the sources in registration order and claims every global id: first the
  mount of a mounted source, then its cards in ordinal order. A mount that is not a segment is `mount-invalid` and
  excludes the source; a mount that another id already uses is `collision` and excludes the source; a mounted source
  without a usable name (so its mount would fall back to the placeholder name) is excluded without a problem, because
  Loading already reported `source-invalid`; a card whose id is taken is `collision` and is recorded in `Dropped`, and
  the cards below it (visited after it, in ordinal order) claim no id unless another source already holds it (which is
  still a `collision`), so a dropped subtree never reserves ids that another source could use. The message names the
  later source and file and the source and file that hold the id. It returns the owner of every claimed id, which the
  tree placement uses to tell `tree-foreign` from `parent-missing`.
- **`CreateRoot(entry)`** builds the generated root card of a mounted source: the id, name, and `ParentId` are the mount
  and none; the title, summary, aliases, culture, extension keys, and body come from `_source.md` (the title defaults to
  the name and the summary to `Help cards of {name}.`); the card has no tags and no examples; its children are the
  roots of the source in sibling order.
- **`CheckRootNames(registry, problems)`** claims the name and the aliases of every root of the catalog, ignoring case:
  the mount and aliases of a mount root, and the name and aliases of each root card of a flat source. A claim that
  another source already holds is `collision`; a repeat inside one flat source is left to the sibling check
  (`name-duplicate`); a mount alias that repeats the mount is `name-duplicate`.

#### Error Handling

Null arguments throw `ArgumentNullException`. Defects are problems; the later source or card is left out and the
earlier one is kept, so one clash yields one problem.

#### Dependencies

Composition (`SourceRegistry`, `SourceEntry`, `ProblemCollector`, `SiblingOrder`), Loading (`CardReader.IsSegment`),
and Catalog (`CardNode`, `HelpProblem`, `RuleIds`).

#### Callers

`HelpCatalogBuilder` calls `Register` and `CheckRootNames` during placement, `CreateRoot` and `GlobalId` while it
creates the nodes, and `GlobalId` while it freezes the data.
