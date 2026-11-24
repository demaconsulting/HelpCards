### TextWrapper Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`TextWrapper` breaks generated text into lines of at most a width, with a prefix on the first line and a hanging indent
on the others. It never touches authored text.

#### Data Model

The class is static and stateless. Words are the runs of characters between space characters (U+0020); no other white
space separates words, so a tab or line feed inside a text stays inside its word. Runs of spaces collapse to one space.
Every line is appended to a `StringBuilder` and ends with a line feed.

#### Key Methods

- **`Append(builder, text, width, firstPrefix, restPrefix)`** adds words to the current line while the line, including
  its prefix, stays within the width; otherwise it starts a new line with the hanging indent. A word that is longer than
  the width stands on a line of its own. A width of zero writes everything on one line. Text without a word writes
  nothing.
- **`AppendHanging(builder, text, width, prefix)`** is `Append` with a hanging indent of spaces as wide as the first
  prefix, which is what a two-column list needs.

#### Error Handling

Null arguments throw `ArgumentNullException`.

#### Dependencies

None beyond the Base Class Library.

#### Callers

`TextRenderer`.
