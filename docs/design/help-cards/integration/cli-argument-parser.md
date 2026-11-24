### CliArgumentParser Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`CliArgumentParser` reads the arguments that follow the help verb. It is the only place that knows the grammar of the
command line: which options exist, which take a value, which repeat, and which combinations are allowed.

#### Data Model

An instance holds the state of one parse (the words, the values read so far, the options seen, and the first error). The
entry point is static, so a parse is safe for concurrent use. The result `CliParseResult` holds a `CliRequest` or the
message and hint of the first usage error, and whether the arguments ask for JSON. `CliRequest` holds the mode
(`CliMode`: index, card, examples, example, search, tags), the words of the path, the format, the width or the auto
flag, the name of a search or example, the tags and sources, the all flag, and the culture.

| Option | Value | Rule |
| --- | --- | --- |
| `--json`, `--format` | none; text, markdown, json | Equal when both are given |
| `--examples`, `--example` | none; a name | Need a path; conflict with each other, `--search`, `--tag`, `--all` |
| `--search` | one query | No path; conflicts with `--examples`, `--example`, `--all`; may use `--tag`, `--source` |
| `--tag`, `--source` | a name | May repeat; not with a card path |
| `--all` | none | Index only; not with `--tag` (an addition to spec 6.12: both choose what the index lists) |
| `--width` | 20 to 500, or auto | Validated for every format |
| `--lang` | a culture name | Only when enabled; 2 or 3 letters, then subtags of 2 to 8 letters or digits |

#### Key Methods

- **`Parse(arguments, languageEnabled)`** reads the arguments left to right. A word that does not start with a hyphen, a
  single hyphen, and every word after `--` are path words. A value option takes the next argument only; an argument that
  starts with two hyphens, or is white space, is not a value. The first error is kept: unknown, repeated, or valueless
  options and invalid values during the pass; then `--json` against `--format`; then conflicts between mode options and
  other options; then options that need or forbid a card path.
- **`GetValidOptions(languageEnabled)`** lists the options in the order the messages use.

#### Error Handling

Null arguments throw `ArgumentNullException`. Everything else is a usage error in the result. A culture name must be
two or three letters followed by subtags of 2 to 8 letters or digits (the grammar of a culture suffix, 6.1). It is
not required to be known to the platform: a well-formed name that no file uses falls back to the neutral files, also
under invariant globalization, where the platform cannot represent the name and the request carries the invariant
culture.

#### Dependencies

`HelpFormat` and the width limits of `HelpRenderOptions`.

#### Callers

`CliDispatcher`.
