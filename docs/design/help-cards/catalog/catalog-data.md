### CatalogData Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`CatalogData` is the immutable result of composition. It is created once, by the builder, and shared by a catalog and
all of its views, so that views cost no copy of the cards and every thread sees the same data.

#### Data Model

| Member | Meaning |
| --- | --- |
| `Cards` | Every `CardNode` in depth-first order over the roots |
| `RootIds` | The global ids of the roots in order |
| `Sources` | The `HelpSourceInfo` of each source in registration order |
| `Diagnostics` | The informational problems in display order |
| `TagNames` | The distinct tags in ordinal order |
| `Culture` | The culture the catalog was created with |
| `ToolName`, `ToolVersion` | The values set on the builder, or null when not set |
| `AllowedTags` | The closed tag vocabulary, or null when tags are free-form |

Supporting types: `CardNode` (a record with the id, local id, name, parent id, source, title, summary, tags, aliases,
culture, the neutral card file, the body file, the first body line, child ids, see-also ids, examples, extensions, and
the resolved body; the three file and line members tell a validator where the card was written), `ResolvedPart` (text or
a resolved link), and `ResolvedLink` (the global ids of the linked card, example, and example owner, and the source and
display text of an external link). Lists are read-only collections made by `CardNode.Freeze`, which copies its input;
the closed tag vocabulary is copied into a frozen set when the data is created. Lookups by id ignore case.

#### Key Methods

- **`Find(id)`** returns the node with the id, ignoring case, or null.
- **`CardNode.Freeze`** copies a sequence or a dictionary into a read-only collection that cannot be changed through a
  cast.

#### Error Handling

N/A - the data is a value holder; the builder guarantees that it is consistent (every child, see-also, and link id
names an existing node) before it creates the data.

#### Dependencies

`HelpExample`, `HelpSourceInfo`, `HelpProblem`, and the Base Class Library.

#### Callers

`HelpCatalogBuilder` creates it, `CatalogScope` and `HelpCatalog` read it, and the Presentation and Testing subsystems
read it through the catalog.
