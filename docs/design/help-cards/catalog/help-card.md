### HelpCard Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`HelpCard` is one card as a catalog view shows it. It gives callers the facts of the card and, for renderers, the
body as text and links to visible targets. With it are the small public model types of the catalog, which carry data
and no behavior of their own.

#### Data Model

`HelpCard` wraps a frozen `CardNode` and the `CatalogScope` of its view. `ParentId`, `ChildIds`, and `SeeAlsoIds` are
computed from the scope when the card is created: the nearest visible ancestor, the lifted visible children, and the
visible see-also ids. The other properties read the node. The body and the body pieces are `Lazy` values with
`ExecutionAndPublication`, so they are computed once even under concurrent access.

Supporting types, all immutable and created only by the catalog:

| Type | Meaning |
| --- | --- |
| `HelpExample` | Name, kind, language, file name, description, and content of an example |
| `HelpExampleReference` | An example with the global id of the card that owns it (a record) |
| `HelpLink` | A link to a visible target: ids, example name, title, display text, and source |
| `HelpSourceInfo` | Name, mount, title, summary, language, and schema of a source (a record) |
| `HelpPathResolution` | The card, or the failing parent, segment, valid names, and a suggestion |
| `HelpSearchResult` | Id, title, summary, source, score, and matched fields; compares matched fields by item |
| `BodyPiece` (internal) | Literal text or a `HelpLink`, after the hiding rules of the view |

The kinds of an example are `command`, `source`, and `snippet`. The suggestion of a path resolution is the closest name
or alias of the failing parent, filled by the path resolver.

`HelpLink` follows these rules: `CardId` is the card the link names (null for `[[#name]]`), `ExampleCardId` is the
card that owns the linked example, `Title` is the title of the card the link displays (the owner for example links),
and `Text` and `SourceName` are present only for external links.

#### Key Methods

- **`Body`** writes the pieces in the plain style: a card link reads ``the card `a/b` ``, an example link reads
  ``the example `x` of the card `a/b` ``, and a link written `[[#x]]` whose example the card itself owns reads
  ``the example `x` ``. This is the only place in the Catalog subsystem that writes link text; the Presentation
  subsystem writes the other styles from the same pieces.
- **`BodyPieces`** (internal) applies the hiding rules of the view to the resolved body: a link to a hidden card
  becomes the card's title, a link to a hidden example becomes `an example`, and an external link to a hidden target
  becomes its display text. Adjacent text is merged.
- **`File`, `BodyFile`, `BodyLine`** (internal) tell where the card was written: the neutral card file, the file that
  supplied the body (a translation when one was selected), and the line of the first body line in that file. The
  validator reports defects of a body at that file and line.

#### Error Handling

N/A - a card validates nothing and throws nothing; all values were validated when the catalog was created.

#### Dependencies

`CardNode`, `CatalogScope`, `ResolvedLink`, `HelpLink`, and `HelpExample`.

#### Callers

`HelpCatalog` creates the cards; callers read them; the Presentation subsystem reads `BodyPieces`.
