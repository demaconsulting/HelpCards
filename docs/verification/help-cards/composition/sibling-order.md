### SiblingOrder Verification Design

#### Verification Approach

The comparator is tested directly in `SiblingOrderTests.cs` with card definitions that the real card reader produces
from in-memory files, and with `children` entries built by the test. No mocks.

#### Test Environment

Standard xUnit v3 runner; in-memory files.

#### Acceptance Criteria

All tests in `SiblingOrderTests.cs` pass: the order is exactly as specified for every combination, and problems carry
the file and line of the entry.

#### Test Scenarios

- **Order values**: negative, equal, and missing values; ascending order, then cards without a value by name.
- **Children list**: listed cards first in the listed order (also a listed card with an order value), the rest after
  them in the normal order.
- **Ordinal ties**: a name with a dash sorts before a name with a digit, which proves ordinal and not culture order.
- **Repeated entry**: reported once at the line of the second entry, the card placed once.
- **Unknown entry**: reported unless the id of the entry, with the parent prefix, is suppressed.
- **Names and arguments**: the name of a nested card is its last segment; null arguments throw.
