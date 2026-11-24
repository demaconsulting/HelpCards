### CultureSelector Verification Design

#### Verification Approach

The selector is stateless and tested directly with `CultureInfo` values and lists of suffixes.

#### Test Environment

Standard xUnit v3 runner; cultures that the runtime knows (`de`, `de-DE`, `de-CH`, `zh-Hans-CN`); no files.

#### Acceptance Criteria

All tests in `CultureSelectorTests.cs` pass: chains are exact, the most specific available translation is
selected, and the suffix grammar accepts and rejects the listed names.

#### Test Scenarios

- **Fallback chain**: invariant (empty), neutral (one name), region (two names), script and region (three
  names), null culture raises `ArgumentNullException`.
- **Selection**: the specific translation preferred over the language, the language used when the region is
  missing, none when nothing matches, a region translation not used for a language request, case ignored with
  the written suffix returned, an empty chain selects nothing.
- **Culture names**: valid names (`de`, `fil`, `de-DE`, `zh-Hans-CN`, `es-419`) and invalid names (empty,
  one letter, upper case, underscore, short subtag, trailing dash, digits only).
