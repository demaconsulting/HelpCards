### SearchIndex Verification Design

#### Verification Approach

The index is tested directly in `SearchIndexTests.cs` through the internal index of catalogs created from the embedded
`big` fixture and from small in-memory sources. A golden file pins the ranking of ten queries over the twelve cards,
including the matched fields and the order of equal scores. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures; no sleeps or timers.

#### Acceptance Criteria

All tests in `SearchIndexTests.cs` pass, and the ranking report equals the golden file `ranking.expected.txt` of the
`cli` fixture exactly.

#### Test Scenarios

- **Ranking golden**: ten queries (a word, a plural, two words, an alias, a path, a prefix, a phrase, and one without a
  match) with scores and matched fields.
- **Fields**: one card for each field scores the weight of that field plus the bonuses.
- **Prefixes**: a prefix scores less than a word, needs three letters, and is not used for tags.
- **Mounts**: the name of a mount does not make the cards below it match.
- **Plurals**: stemming for neutral and English catalogs, none for German, and none for short words.
- **Normalization**: repeated words, case, punctuation, and an empty query; null arguments throw.
- **Order**: equal scores are ordered by depth and then by id.
- **Suggestions**: distance, order, the count limit, case, the third of the length for long words, and the arguments.
