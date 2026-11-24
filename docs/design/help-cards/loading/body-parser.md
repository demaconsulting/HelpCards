### BodyParser Design

![Loading Structure](LoadingView.svg)

#### Purpose

`BodyParser` splits the Markdown body of a card into literal text parts and link parts. It exists so that every
rendering can write a link in the style the reader needs, and so that a link typo is an error at its line
instead of stray brackets in the output.

#### Data Model

| Type | Content |
| --- | --- |
| `BodyPart` | Exactly one of a text string and a link |
| `ParsedLink` | Source name, card id, example name, display text (each optional), and the line |

Links are syntax only. Their targets are checked later over the final tree because forward references are
valid.

#### Key Methods

**`Parse(body, firstLine, sourceName, file, problems)`** scans the body once:

1. `FindCode` marks the characters that are code. A fence opens with three or more backticks or tildes (a
   backtick line with more backticks later on the same line is an inline span, not a fence) and closes at a
   line that is a run of at least the same length of the same character; an unclosed fence runs to the end.
   An inline span opens with a run of backticks and closes at the next run of exactly the same length in the
   same paragraph; a span may cross a line break; an unmatched run is literal text.
2. Outside code, at each `[[` the external pattern `[[source:id#example|text]]` and then the local patterns
   `[[id]]`, `[[id#example]]`, `[[#example]]` are tried. Ids are canonical lower-case local ids of at most
   eight segments of at most 48 characters; display text has 1 to 120 characters, no `]`, no line break, and
   no leading or trailing space, and the first bar splits.
3. Text that starts like a link (`[[` followed by a letter, a digit, or `#`) but is not a valid link is a
   `link-syntax` problem at its line. Other brackets are literal text.

Adjacent text is merged and empty text is omitted.

#### Error Handling

Defects are returned as `link-syntax` problems; the parser never throws for body text.

#### Dependencies

`HelpProblem` and `RuleIds` of the Catalog subsystem; Base Class Library only (`System.Text.RegularExpressions`
with a match timeout).

#### Callers

`CardReader` parses the body of every card, overlay, translation, and source file after token replacement.
