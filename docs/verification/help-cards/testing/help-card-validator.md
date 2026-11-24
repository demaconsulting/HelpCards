### HelpCardValidator Verification Design

#### Verification Approach

The validator, its options, and the comparison with a directory are tested directly in `HelpCardValidatorTests.cs` and
its partial files: entry points and assertions in the main file, one scenario per quality rule in the `Rules` file, the
options in the `Options` file, and the directory comparison in the `Resources` file. Defective sources are in-memory
dictionaries; the valid source is the embedded `simple` fixture. Rules of a product are small test classes. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures; in-memory sources; a unique temporary directory per directory test.

#### Acceptance Criteria

All tests in the four test files pass.

#### Test Scenarios

- **Entry points**: a valid source has no problems; a source, a callback, and a catalog agree; a defective source
  returns its errors in order without throwing; a summary that tries to span lines is an error of the build; the tag
  vocabulary of a callback is applied.
- **Order**: errors come first and each severity is ordered by source, file, line, and rule; a first source with a later
  file name is listed before a second source.
- **Summary and size**: summaries at and over the limits for roots and other cards; a card over the card limit; a root
  with children over the index limit only.
- **Body rules**: the reserved headings with their file lines and not inside a fence; a fence without a language and
  one never closed with their lines; long lines only when a limit is set.
- **Tokens**: a defined token that no file uses; a token used in a title; built-in tokens are never reported; a data
  file with invalid UTF-8 is reported as a `source-read` error at its path instead of throwing.
- **Commands**: a pipe and another tool name are reported with the tool name of the builder; the name of the source
  is used without a tool name; other kinds are not checked.
- **Product rules**: a rule adds its problems, an error of a rule fails the assertion, and a rule that returns nothing
  is accepted.
- **External sources**: absent sources are errors only with the option, with the file and the source name.
- **Assertion**: a valid source passes; errors throw one exception with all of them and the number of further ones;
  warnings fail only with the option.
- **Options**: defaults, stored values with a copy of the rules, limits that are not positive, and null rules.
- **Directory**: the same files; files on one side only in path order; invalid arguments.
- **Arguments**: null arguments and a builder without a source.
- **Imports**: a source that imports a library is composed after it without errors, a callback that leaves the import
  out reports import-missing, and a library that is reached twice is composed once.
