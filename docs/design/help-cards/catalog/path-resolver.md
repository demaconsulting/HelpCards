### PathResolver Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`PathResolver` turns a path such as `requirements tags` into a card, and says where and why a path fails. It is the one
place where the rules of path lookup live: segments, names, aliases, case, and hidden cards.

#### Data Model

The class is static and stateless. Its result `PathMatch` is a record with the card (or null), the global id of the
parent whose children were searched, the segment that matched nothing, the visible names of the level that failed, and
the closest name or alias at that level.

#### Key Methods

- **`Resolve(scope, path)`** splits the path on `/` and white space, drops empty segments, and walks the real tree: the
  first segment is matched against the names and aliases of the roots, each later one against those of the children of
  the card found so far, with `OrdinalIgnoreCase` after NFC normalization of the segment (text that is not valid Unicode
  is used as it is). The walk goes through hidden cards. A segment that matches nothing, or a final card that is hidden,
  fails with the parent, the segment, and the names of the visible cards at that level. A hidden card is never named in
  the outcome: when the failing parent is hidden, the parent is its nearest visible ancestor (null when there is
  none), and the names are those of the visible children of the hidden card, because the walk follows the real tree.
  The closest
  name or alias of the visible cards at that level is chosen with `SearchIndex.Suggest`. An empty path names nothing,
  lists the roots, and has no suggestion.
- **`Find(scope, path)`** returns only the card of `Resolve`.

#### Error Handling

Null arguments throw `ArgumentNullException`. A path that names nothing is a normal result, not an error.

#### Dependencies

`CatalogScope`, `CatalogData`, `CardNode`, and `SearchIndex` for the suggestion.

#### Callers

`HelpCatalog.Find` and `HelpCatalog.Resolve`, and `ExampleResolver` for the card forms of an example search.
