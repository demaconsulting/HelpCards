### TokenResolver Design

![Loading Structure](LoadingView.svg)

#### Purpose

`TokenResolver` replaces `{{name}}` tokens in the text of one source with the values that are in scope for it.
It exists so that shared values such as limits and the tool name live in one place and cards stay correct when
they change.

#### Data Model

| Member | Meaning |
| --- | --- |
| values | Token name to value for the scope: built-ins, own tokens, imported tokens |
| source name | The name of the source the scope belongs to, used in problems |
| `ScopeProblems` | The `token-redefined` problems found while the scope was built |

A resolver is immutable after creation and safe for concurrent use. Names are case-sensitive and use ASCII
letters and digits. The built-in names are `tool` and `version`.

#### Key Methods

- **`Create(sourceName, ownTokens, importedTokens, tool, version)`** builds the scope: the built-ins first,
  then the own tokens, then the tokens of each imported source in the order given, visiting names in ordinal
  order so messages are stable. A name that is a built-in, or that was already defined by the source or an
  earlier import, is reported as `token-redefined` and the first definition is kept. Composition supplies the
  imports transitively and the default `tool` (the name of the first source).
- **`Substitute(text, file, firstLine, problems)`** scans the text once, left to right, for the alternation
  `\\\{\{|\{\{(?<name>[A-Za-z0-9]+)\}\}`. A well-formed token is replaced; the line of an unknown token is
  `firstLine` plus the line feeds before it, the token stays in the text, and a `token-unknown` problem is added
  (its message also names the escape). The escape `\{{` is replaced with `{{`: the backslash is dropped and, because
  the braces are consumed by the match, the text after the escape is not scanned as a token, so `\{{name}}` gives
  `{{name}}` and is never reported, whether or not `name` is defined. The escape needs no name after it
  (`a\{{ b` gives `a{{ b`). Every other backslash is kept; in `\\{{tool}}` the first backslash is literal and the
  second escapes, so a backslash cannot directly precede a replaced token. Text such as `{{ x }}` or `{{a-b}}` is
  not a token and is left alone. A text without `{{` is returned without a scan.
- **`IsValidTokenName(name)`** tells whether a name is one or more ASCII letters and digits.
- **`FindNames(text)`** lists the distinct names of the well-formed tokens of a text in order of first use, with the
  same pattern as `Substitute` but skipping escapes, so `\{{x}}` does not list `x`; the validator uses it to find
  tokens that no file uses. The validator scans whole raw files, so a token that appears only inside an example with
  `tokens: false` still counts as used; this is conservative and never produces a false `unused-token`.

#### Error Handling

Defects are returned as problems, never thrown. `Substitute` never throws for text content.

#### Dependencies

`HelpProblem` and `RuleIds` of the Catalog subsystem; Base Class Library only.

#### Callers

`CardReader` substitutes titles, summaries, example descriptions and content, and bodies; `HelpSource` uses
`IsValidTokenName`; Composition creates resolvers from the import list.
