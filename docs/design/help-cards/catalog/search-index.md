### SearchIndex Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`SearchIndex` ranks the cards of a catalog for a query and finds the names that are closest to a word that was not
found. It holds the whole scoring and suggestion rules in one place, so that the command-line helper, the agent tool,
and adopters get the same ranking.

#### Data Model

The class is internal and partial: `SearchIndex.cs` holds the index and the scoring, `SearchIndex.Suggest.cs` holds the
suggestions. An index belongs to one `CatalogData` and is built on first use by a thread-safe `Lazy` of the data. It
holds, for each card, the tokens of six fields (aliases, local id, example names, title, tags, summary), the lower-case
global id, local id, name, and aliases for the whole-query bonus, and the depth of the local id. It never changes
after it is built, so any number of threads and views can use it. A view does not own an index; it passes the cards it
shows.

| Field | Exact weight | Prefix weight |
| --- | --- | --- |
| alias | 40 | 20 |
| id (local id, without a mount prefix) | 40 | 20 |
| example | 25 | 12 |
| title | 30 | 15 |
| tag | 20 | none |
| summary | 10 | 5 |

#### Key Methods

- **`Build(data)`** reads the cards of the data into tokens. Tokens are runs of letters and digits in lower case with
  the invariant culture. When the culture of the catalog is neutral or English, a token of more than three letters that
  ends in `s` loses that `s`.
- **`Search(cards, query)`** tokenizes the query, drops repeated tokens, and scores each given card. For each query
  token the best field counts: the exact weight when a field has the token, else the prefix weight when the token has at
  least three letters and a field token starts with it. Every field that matched is reported, in the fixed order alias,
  id, example, title, tag, summary. The sum is raised by 50 when every token matched and by 100 when the query, lower
  case with runs of spaces and slashes read as one slash, equals the global id, local id, name, or an alias of the card.
  Cards with a score of zero are left out. The order is score descending, depth ascending, global id ordinal.
- **`Suggest(word, candidates, maxCount)`** compares the lower-cased word with each distinct candidate by Levenshtein
  distance and accepts a distance up to the larger of two and a third of the length of the word. A candidate whose
  length differs by more than that is skipped without computing, and the computation stops early once a whole row is
  over the limit, so a long hostile word costs little. The order is distance, then ordinal.

#### Error Handling

Null arguments throw `ArgumentNullException`; a count below one throws `ArgumentOutOfRangeException`. A query without a
word gives an empty list.

#### Dependencies

`CatalogData` and `CardNode`; `HelpSearchResult` for the results.

#### Callers

`HelpCatalog.Search` and `HelpCatalog.Suggest`, and `PathResolver` for the suggestion of a failed path.
