### CliArgumentParser Verification Design

#### Verification Approach

The parser is tested directly in `CliArgumentParserTests.cs` by reading argument lists and comparing the request, the
message, and the hint. No mocks, no catalog.

#### Test Environment

Standard xUnit v3 runner.

#### Acceptance Criteria

All tests in `CliArgumentParserTests.cs` pass; every row of the option table of the design has at least one test.

#### Test Scenarios

- **Words and options**: no arguments, words, options between words, a double hyphen, and a single hyphen.
- **Format**: every format value and the combinations with `--json`.
- **Values**: invalid formats and widths, valueless options, an empty or white-space value, and an unknown option with
  the list of valid options, with and without the language option.
- **Repeats**: every option that may not repeat, and the tag and source options that may.
- **Modes**: each combination that chooses the index, a card, examples, an example, a search, or the tag listing.
- **Conflicts**: every pair of mode options, mode options with the tag and all options, `--tag` with `--all`, a search
  with tag and source (accepted), and the path rules.
- **Language**: valid and invalid culture names (including a one-character subtag), well-formed names that no platform
  knows (accepted, also under invariant globalization), enabled and disabled.
- **JSON requested**: before and after an error, and after a double hyphen.
- **Arguments**: null arguments.
