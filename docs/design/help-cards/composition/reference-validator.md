### ReferenceValidator Design

![Composition Structure](CompositionView.svg)

#### Purpose

`ReferenceValidator` checks the final tree of a catalog. It resolves every link and see-also entry to global ids,
finds the card that owns each linked example, and checks that example names, names, and aliases are unique. It exists
so that every reference is judged against the finished tree, which makes forward references valid.

#### Data Model

The validator is created for one build with the source registry, the nodes of the tree by global id (children and
examples in place), and the collector that receives the problems. Every text is resolved in the scope of the source
that wrote it: the validator asks the registry which card an id names, as seen from that source. It keeps no other
state and is used once per build.

#### Key Methods

- **`ResolveBody(build)`** walks the body segments of a card: its own body and the bodies that overlays append, each in
  the scope of its source and separated by a blank line (nothing is added for an empty body). A link to a source that is
  not registered becomes its display text with an informational `external-source-absent` problem. Otherwise the named
  card is looked up by the registry (`link-target` when missing, `ambiguous-reference` when a bare id names a card of
  the source and of an import, nothing when suppressed); a card link keeps the global id, and an example link searches
  the card, or for `[[#name]]` the containing card, with `ExampleResolver.FindCandidates`: the card's own example when
  it has one of that name, otherwise every example of the name below it (`example-target` when none matches,
  `ambiguous-reference` when the examples belong to two or more cards, naming them and the form that chooses one). The
  result records the card the link names, the example, the card that owns the example, and for external links the source
  and display text. Unresolvable links are left out because the build fails anyway.
- **`ResolveLanding(entry, root)`** resolves the body of `_source.md` for the generated root card of a mounted source.
- **`ResolveSeeAlso(build)`** resolves the entries of a card and of its overlays in the order written, drops an entry
  for an absent source with the same informational problem, reports an entry that names no card (`see-also-target`) or
  an ambiguous one, and removes duplicates.
- **`CheckExampleNames(builds)`** reports the second use of an example name within a card of a source, counting
  the examples that overlays add to the card they extend (`duplicate-example`, at the
  file and line of the second example, which for an overlay is the overlay file).
- **`CheckSiblingNames(entry, siblings)`** claims the name and then each alias, normalized to NFC, of every sibling in
  order in a case-insensitive table and reports a claim that is already taken (`name-duplicate`), including a card's own
  alias that equals its name or another alias.

#### Error Handling

The validator never throws for a defect. A reference to a suppressed id yields no problem. A missing card or example
yields one error and the reference is dropped.

#### Dependencies

Composition (`SourceRegistry`, `CardBuild`), Loading definitions (`CardDefinition`, `ParsedLink`,
`ReferenceDefinition`), `SiblingOrder` for names, `ExampleResolver`, `CardNode`, `ResolvedPart`, `ResolvedLink`, and the
problem model.

#### Callers

`HelpCatalogBuilder` creates one validator per build and calls it for every mount root and card in ordinal id order,
then for the uniqueness checks.
