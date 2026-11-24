### HelpExampleRunner Verification Design

#### Verification Approach

The runner is tested directly in `HelpExampleRunnerTests.cs` with fake delegates that return crafted outcomes, over the
catalog of the embedded `runnable` fixture and in-memory cards, with unique temporary folders. No test starts a
process, and no mock framework is used. One test builds a card whose example has an escaping file name through the
internal constructor of `HelpExample`, because the loader would never produce one.

#### Test Environment

Standard xUnit v3 runner; the embedded `runnable` fixture; the `TemporaryDirectory` helper of the test support folder;
the real temporary folder of the machine for the scratch folders of the catalog overload, which the tests check for
deletion.

#### Acceptance Criteria

All tests in `HelpExampleRunnerTests.cs` pass: files are written exactly as specified, commands run in order with the
specified arguments and working directory, every difference is reported with the specified rule, file, and message
content, scratch folders are deleted on every path, and arguments are checked.

#### Test Scenarios

- **Materialize**: source examples with a folder and an inline text, LF, one trailing line feed, no byte order mark;
  snippets and commands not written; the same name written twice (last wins, both names returned); a name that escapes
  the folder (an argument error, and nothing written outside); a card with only commands still creates the folder.
- **Run order**: the commands of a card in order with the arguments after the tool name and the folder as working
  directory, with the saved files present, and a working directory that exists even for a card with only commands; a
  catalog run with one fresh folder for each card, the files of the card saved there, and every folder deleted; a
  catalog without commands runs nothing.
- **Exit code**: a mismatch is one `example-exit-code` problem at the card file, line 0, naming the example, the card,
  both codes, and the output.
- **Output**: one `example-output` problem for every missing fragment, CRLF accepted, the comparison case-sensitive, the
  output cut at 400 characters in the message, and a null output treated as empty (for a command with and without an
  expected output).
- **Parse**: a command with a pipe is an `example-parse` problem and is not run; the next command still runs; the tool
  name defaults to the one of the catalog, then the first source, and an explicit name wins.
- **Report**: `AssertSucceeded` throws a catalog exception with the same problems for a failed run and does nothing for
  a good one.
- **Cleanup**: an exception of the delegate propagates and the scratch folder is gone.
- **Arguments**: null and blank arguments of the three methods.
