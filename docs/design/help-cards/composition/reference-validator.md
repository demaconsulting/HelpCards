### ReferenceValidator Design

![Composition Structure](CompositionView.svg)

#### Purpose

`ReferenceValidator` checks the final tree of a source. It resolves every link and see-also entry to global ids,
finds the card that owns each linked example, and checks that example names, names, and aliases are unique. It exists
so that every reference is judged against the finished tree, which makes forward references valid.

#### Data Model

The validator is created for one source with the nodes of the tree by id (children and examples in place), the set of
suppressed ids (files that failed to load and cards with no place in the tree), the declared names of the registered
sources, and the list that receives the problems. It keeps no other state and is used once per build.

#### Key Methods

- **`ResolveBody(card)`** walks the body parts of a card. A link to a source that is not registered becomes its display
  text with an informational `external-source-absent` problem. Otherwise the named card is looked up (`link-target`
  when missing, unless suppressed); a card link keeps the global id, and an example link searches the card, or for
  `[[#name]]` the containing card, and its subtree depth first with `ExampleResolver` (`example-target` when no
  example matches). The result records the card the link names, the example, the card that owns the example, and
  for external links the source and display text. Unresolvable links are left out because the build fails anyway.
- **`ResolveSeeAlso(card)`** resolves the entries in the order written, drops an entry for an absent source with the
  same informational problem, reports an entry that names no card (`see-also-target`), and removes duplicates.
- **`CheckExampleNames(cards)`** reports the second use of an example name within a scope, which is the first segment
  of the id of the owning card (`duplicate-example`, at the file and line of the second example).
- **`CheckSiblingNames(siblings)`** claims the name and then each alias, normalized to NFC, of every sibling in order in
  a case-insensitive table and reports a claim that is already taken (`name-duplicate`), including a card's own alias
  that equals its name or another alias.

#### Error Handling

The validator never throws for a defect. A reference to a suppressed id yields no problem. A missing card or example
yields one error and the reference is dropped.

#### Dependencies

Loading definitions (`CardDefinition`, `ParsedLink`, `ReferenceDefinition`), `SiblingOrder` for names,
`ExampleResolver`, `CardNode`, `ResolvedPart`, `ResolvedLink`, and the problem model.

#### Callers

`HelpCatalogBuilder` creates one validator per build and calls it for every card in ordinal id order, then for the
uniqueness checks.
