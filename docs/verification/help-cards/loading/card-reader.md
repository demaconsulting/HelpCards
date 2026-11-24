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
- **Tokens**: replaced in title, summary, and body; unknown token with exact line; unknown token in summary;
  redefined built-in at source level.
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
