### HelpCardMarkdown Design

![Testing Structure](TestingView.svg)

#### Purpose

`HelpCardMarkdown` reads the structure of a card body for parity tests, which compare the help with other documents
such as a user guide, and gives the validator its classification of body lines. Fenced blocks are never searched for
headings or bullets.

#### Data Model

The class is static and has no state. The internal record `MarkdownLine` holds the one-based number, the text, the kind,
and, for the line that opens a fence, the language that follows the fence characters. The internal enumeration
`MarkdownLineKind` has the values `Text`, `FenceOpen`, `FenceBody`, and `FenceClose`.

#### Key Methods

- **`Headings(body)`** lists the texts of the ATX headings of levels one to six outside fences: up to three spaces of
  indent, one to six `#`, a space, the text, and an optional closing run of `#`.
- **`GetSection(body, heading)`** returns the lines from the first heading with that text to the next heading of the
  same or a higher level, without blank lines at its ends, or null when the heading does not exist.
- **`Bullets(body, heading)`** returns the bullets of that section: a line that starts with `-`, `*`, or `+` and a
  space, joined with the indented lines that follow it with single spaces; a fence or other text ends a bullet.
- **`Scan(body, out unclosedFenceLine)`** (internal) splits the body at line feeds, ignores carriage returns, and
  classifies each line with the fence rule of `BodyParser`; it reports the line of a fence that is never closed.
- **`TryParseHeading`** (internal) reads the level and text of a heading line for the validator.

#### Error Handling

A null body or heading throws `ArgumentNullException`; a white-space heading throws `ArgumentException`. A missing
heading is a null section and no bullets.

#### Dependencies

`BodyParser` of the Loading subsystem, for the rule that opens and closes a fence.

#### Callers

`HelpCardValidator` (the body rules) and adopters.
