### Mounter Verification Design

#### Verification Approach

The unit is tested directly in `MounterTests.cs`. The tests read in-memory sources into a registry with the real loader,
give each source a simple tree of its cards, and call the unit's methods. Collisions are produced with small sources
that use the same ids, mounts, and names. Nothing is mocked.

#### Test Environment

Standard xUnit v3 runner; in-memory files; no temporary files.

#### Acceptance Criteria

All tests in `MounterTests.cs` pass: global ids, owners, left-out sources and cards, problems, and the generated root
card are exactly as documented.

#### Test Scenarios

- **Global ids**: a flat source keeps local ids, a mounted source prefixes them with its default or given mount; null
  arguments throw.
- **Claims**: distinct ids give owners and no problem; a flat card that holds an id, a mount that equals it, and a card
  that equals a mount give `collision` at the later source and the earlier holder is named with its file; an invalid
  mount gives `mount-invalid` and excludes the source; a dropped card is recorded for the tree placement.
- **Mount root**: the id, name, source, title, summary, aliases, and extension keys come from `_source.md`, the card has
  no parent, tags, or examples, and its children are the roots only.
- **Root names**: an alias of a root that another source uses as a name is `collision`; two roots of one flat source are
  left to the sibling check; a mount alias that repeats the mount is `name-duplicate`.
