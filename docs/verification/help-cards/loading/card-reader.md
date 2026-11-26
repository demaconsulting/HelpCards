### CardReader Verification Design

#### Verification Approach

The reader is tested through its single entry point `CardReader.Read` with in-memory sources built from path
and text pairs (the helper `SourceReading` in the test support folder), so every scenario shows the input
files and the exact definition or problems that result. A counting provider proves how often files are listed
and read and makes reads fail on purpose. The test class is split over several files of one partial class:
cards and overlays, examples, cultures, and the source file. Error sources live in memory only.

#### Test Environment

Standard xUnit v3 runner; the German UI culture is set on the test's own thread for one test and restored;
a unique temporary directory is used by one test.

#### Acceptance Criteria

All tests of `CardReaderTests` pass: valid files give exact definitions with exact lines, every defect gives
exactly the expected problems (rule, file, line), files with structure errors are dropped and listed as failed,
and no scenario depends on the machine's culture.

#### Test Scenarios

- **File classification**: nested and unordered files, data files, upper-case card and folder names,
  underscore names, source file in a folder, invalid and case-clashing culture suffixes, hidden data file,
  a path of nine segments.
- **Cards**: a card with every key; unknown key with the allowed keys; source keys in a card; missing title
  and summary; empty or list title; invalid alias, tag, child, see-also; duplicate tags; duplicate children
  kept for composition; order values at the limits; scalar for a list key; extension as a list; empty tags.
- **Overlays**: every key, only `extends`, card keys rejected, invalid `optional`, invalid `extends`, example
  file relative to the overlay folder.
- **Examples**: command, source with file, snippet in a subfolder, inline content without reading the file,
  missing file, tokens in file content, missing file key, command with file or without content, multi-line
  command, empty content, blank first line, block scalar content, invalid kind, language, name, missing keys,
  unknown key, unsafe file names, examples that are not maps, structure errors that drop the card.
- **Schema 2 examples**: inline content that needs `save-as`, inline content with a nested name (source and snippet),
  `file` with `content`, a file only (the name is the last segment), `save-as` with `file`, neither content nor file,
  `save-as` on a command, a command with a result, invalid names (`..`, a leading `/`, `.md`, `//`, a space), the
  allowed keys in the message, and an overlay example in a schema 2 source.
- **Schema 1 examples**: `save-as` is `key-unknown` with the message that names schema 2, other unknown keys list no
  `save-as`, `file` with `content` is still the name, and a file only keeps its folder prefix.
- **Expected results**: `exit` and `expect` of a command (also as a literal block with CRLF line endings and blank lines
  between fragments), tokens in `expect` with the line of an unknown token, an exit code outside 0 to 255 or not an
  integer, the bounds 0 and 255, an empty `expect` and one that starts with a blank line, and both keys on a source and
  a snippet.
- **Tokens**: replaced in title, summary, and body; escape (`\{{`) rendered literally in title, summary, and body;
  unknown token with exact line, also beside an escape; unknown token in summary; redefined built-in at source level.
- **Escape in data files**: an escaped name in a data file (`pkg\{{Package}}`) loads as `pkg{{Package}}` without a
  problem, and a real unknown token next to an escape in a data file is still `token-unknown` at its line.
- **Token opt-out (`tokens`)**: `tokens: false` keeps a data file and inline content (command and snippet, with CRLF
  line endings normalized) as written, with no replacement, escape, or problem; one data file shared by two examples is
  verbatim for the one with `tokens: false` and replaced for the other; `tokens: true` replaces like the default;
  `description` and `expect` are still replaced; values `maybe`, `True`, and empty are `value-invalid` at the
  line of the key; empty content is still `example-invalid`; the key is accepted in schema 1 and schema 2.
- **Links**: parsed links and a typo with the right line; an empty body.
- **Cultures**: invariant ignores translations and the UI culture; translation applied; example descriptions;
  region falls back to language; specific preferred; no translation uses neutral; title fallback; empty
  translation body; per-file selection; source language; overlay and source translations; the file that supplied the
  body is recorded.
- **Translation validation**: structural keys, a syntax error of an unused culture, example keys, alias normalization,
  orphans (including the source file), aliases of overlay translations, every translation read.
- **Source file**: every key, defaults, name precedence, missing or invalid names, unsupported schema (no
  cards read), schema values, invalid language (`culture-invalid` at the line of `language`, falling back to `en`),
  card keys rejected, syntax error reported once.
- **Collect all problems**: a null argument; a syntax error ends only its file; several defective files; a
  failing read; a failing listing (which still reports the problems of the token scope); a missing directory; one
  listing and one read per Markdown file, and one read of a data file that several examples share.
