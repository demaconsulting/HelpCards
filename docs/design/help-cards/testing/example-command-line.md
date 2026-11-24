### ExampleCommandLine Design

![Testing Structure](TestingView.svg)

#### Purpose

`ExampleCommandLine` reads the one-line content of a `command` example into the arguments that follow the tool name,
with a grammar that never needs a shell. An adopter runs the command itself, for example by passing the arguments to
its own `Main`, and the library never starts a process.

#### Data Model

The class is static and has no state. The grammar is:

| Part | Rule |
| --- | --- |
| Line | One line; a line feed or carriage return is an error that names the column |
| Words | Separated by one or more spaces; the first word is the tool name |
| Unquoted text | Any character except a space, quotes, and the shell characters listed below the table |
| Single quotes | Literal text up to the next single quote; no escapes |
| Double quotes | `\\` and `\"` are the only escapes; `$` and the backtick are errors |
| Joining | Adjacent unquoted, single-quoted, and double-quoted parts form one word; empty quotes make an empty word |

The shell characters that are errors outside quotes are the pipe, ampersand, semicolon, less-than and greater-than
signs, parentheses, dollar sign, backtick, asterisk, question mark, square and curly brackets, tilde, exclamation mark,
number sign, backslash, and percent sign.

#### Key Methods

- **`TryParse(content, toolName, out arguments, out error)`** splits the content into words with the grammar above,
  checks that the first word equals the tool name (ordinal), and returns the other words. On failure it returns
  `false`, an empty argument list, and an error that says what is wrong and where, for example
  `shell operator '|' is not allowed in an example command (column 7)`.
- The private readers handle one unquoted character, a single-quoted part, and a double-quoted part; each reports
  the first character the grammar does not allow with its one-based column.

#### Error Handling

A bad command line is a `false` result with an error text, never an exception. A null content or tool name throws
`ArgumentNullException`; an empty or white-space tool name throws `ArgumentException`.

#### Dependencies

None inside the library.

#### Callers

`HelpCardValidator` (rule `command-parse`) and adopters.
