### ExampleResolver Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`ExampleResolver` finds examples by name. Composition uses it to resolve example links over the final tree, and
catalogs use it for `FindExample` and `FindExamples`, so one walk defines what "the example of that name below this
card" means.

#### Data Model

The class is static and stateless. A search is a depth-first walk that visits a card, then its children in order; the
children are supplied by the caller as a function, so the same walk serves the final tree of composition and the lifted
tree of a view. Names are compared with `OrdinalIgnoreCase`.

#### Key Methods

- **`FindInSubtree(start, name, children)`** returns the first example of the name, with the id of the card that owns
  it, in the card and the cards below it, or null.
- **`FindCandidates(start, name, children)`** returns what a link to an example name can mean from a card: the
  card's own example when it has one of the name, otherwise every example of the name below it in depth-first order.
  The caller reads one result as a match and several as an ambiguity. The card itself is not searched twice.
- **`FindAll(scope, name, scopeCardId)`** returns every match in the visible cards in depth-first order. A name written
  `card#name` limits the search to the subtree of that card (a global id or a path); `scopeCardId` limits it in the same
  way; both together limit it to the intersection. When a limit applies and the card the limits narrow the search to
  owns an example of the name, only that card's own example is returned (an example name is unique within a card, so
  the own example is the one meant); the cards below count only when it has none. An unknown or hidden card gives an
  empty list.

#### Error Handling

N/A - searches that find nothing return null or an empty list; arguments are validated by the catalog.

#### Dependencies

`CatalogScope`, `PathResolver`, `CardNode`, `HelpExample`, and `HelpExampleReference`.

#### Callers

`ReferenceValidator` (example links) and `HelpCatalog` (`FindExample` and `FindExamples`).
