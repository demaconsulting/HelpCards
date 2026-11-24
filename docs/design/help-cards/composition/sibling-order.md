### SiblingOrder Design

![Composition Structure](CompositionView.svg)

#### Purpose

`SiblingOrder` is the one comparator for the order of the cards that share a parent and of the roots of a source. It
also checks the `children` list that takes part in the order, so that a list that names nothing is reported where it
is applied.

#### Data Model

The class is static and stateless. Its input is the sibling definitions, the local id of the parent (or none for
roots), the `children` entries with their file and line, the set of suppressed ids, the source name, and the list
that receives problems.

#### Key Methods

- **`Order(...)`** returns the siblings in this order: first the cards named in the `children` list, in the order
  listed; then the other cards by ascending `order`, cards without `order` after all cards with one, and ties by name
  with `StringComparer.Ordinal`. A repeated entry is `children-duplicate`; an entry that is not a direct child card is
  `children-unknown`, unless the id it would have was suppressed because that file failed to load. `order` compares
  only siblings of one source.
- **`NameOf(card)`** returns the last segment of the local id of a card.

#### Error Handling

Null arguments throw `ArgumentNullException`. Defects of the list are problems; the ordering itself cannot fail.

#### Dependencies

Loading definitions (`CardDefinition`, `LocatedValue`) and the problem model.

#### Callers

`HelpCatalogBuilder` calls `Order` for the roots and for the children of every placed card; `ReferenceValidator` uses
`NameOf`.
