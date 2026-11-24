### CatalogScope Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`CatalogScope` is the set of cards a view shows and the tree those cards form when hidden cards are lifted out. It
separates the question "which cards are visible" from the frozen data, so that views are created by restriction and
never copy cards.

#### Data Model

A scope holds its `CatalogData`, the set of visible ids, the visible cards in depth-first order, the lifted roots, the
lifted visible children of each visible card, and the nearest visible ancestor of each visible card. All of it is
computed when the scope is created by one walk over the real tree and never changes afterwards. The class is named
`CatalogScope` because the SysML2 view of the subsystem is called `CatalogView`.

#### Key Methods

- **`All(data)`** creates the scope that shows every card.
- **`Restrict(keep)`** returns a new scope that shows the cards of this scope for which the delegate returns true. The
  delegate is called once per visible card, in depth-first order, on the calling thread, and is not kept.
- **`IsVisible(id)`**, **`GetChildren(node)`**, **`GetParentId(node)`** answer the questions of a view. A hidden card is
  not visible, has no children in the scope, and its visible descendants are children of its nearest visible ancestor
  or roots of the scope.
- **`ShowsEverything`** says whether no card is hidden, which lets a catalog list all sources and tags without
  filtering.

#### Error Handling

Null arguments throw `ArgumentNullException`. Exceptions of a restriction delegate propagate to the caller of
`Restrict`; no scope is created in that case.

#### Dependencies

`CatalogData` and `CardNode`.

#### Callers

`HelpCatalog` creates and restricts scopes; `PathResolver` and `ExampleResolver` read them.
