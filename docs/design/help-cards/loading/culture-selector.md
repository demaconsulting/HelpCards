### CultureSelector Design

![Loading Structure](LoadingView.svg)

#### Purpose

`CultureSelector` chooses which culture variant of a file supplies the text for a requested culture. It exists
so that the rule is in one place: an explicit culture, never the ambient UI culture, with per-file fallback
from the most specific translation to the neutral file.

#### Data Model

The class is static and stateless. A culture chain is a read-only list of culture names, most specific first,
without the neutral file. The culture suffix grammar is two or three lower-case letters followed by
dash-separated subtags of two to eight letters or digits.

#### Key Methods

- **`GetFallbackChain(culture)`** starts with the name of the culture and removes the last subtag each time:
  `de-DE` gives `de-DE`, `de`; `zh-Hans-CN` gives `zh-Hans-CN`, `zh-Hans`, `zh`. The invariant culture gives
  an empty chain, which selects the neutral file. A null culture raises `ArgumentNullException`.
- **`Select(chain, available)`** returns the first chain entry that equals (ignoring case) one of the suffixes
  of the translation files that exist for a file stem, as written in the file name, or `null` when the neutral
  file must be used. It never selects a more specific culture than requested.
- **`IsCultureSuffix(text)`** tests the suffix grammar; names are not validated against the operating system.

#### Error Handling

Only the null culture argument throws. A name that does not fit the grammar is simply not a culture suffix and
is reported as a `file-name` problem by `CardReader`.

#### Dependencies

Base Class Library only (`System.Globalization`, `System.Text.RegularExpressions`).

#### Callers

`CardReader` selects the translation of every card, overlay, and the source file; it also classifies file names
with `IsCultureSuffix`.
