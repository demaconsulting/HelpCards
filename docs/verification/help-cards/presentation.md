## Presentation Subsystem Verification Design

This document describes how the Presentation subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `PresentationTests.cs` that create catalogs with the builder of the
Composition subsystem from the embedded `simple` fixture and from in-memory sources, and then use only the public face
of the Presentation subsystem: `HelpRenderOptions` and the `Render` members of `HelpCatalog`. Output is compared
exactly with golden files of the `render` fixture; the golden files are the recorded output of the `simple` fixture in
every format and link style, each followed by one line feed. Nothing is mocked. The contract documents of the
specification are checked verbatim at unit level.

### Test Environment

- xUnit v3 under the .NET SDK, run by `build.ps1` and CI on every supported target framework.
- The golden files are embedded in the test assembly by the packaged MSBuild targets, with the other fixtures, and use
  LF line endings only.
- The determinism scenario changes the current culture of the test thread and restores it; there are no sleeps and no
  timing assertions.

### Acceptance Criteria

The subsystem passes when every scenario below passes on every target framework. Every rendering must equal its golden
file exactly, every JSON document must be one line of ASCII with the contract version and no null, wrapping must touch
only generated lines, and the output must not depend on the culture of the machine.

### Test Scenarios

- **Text golden files**: the guide card and the index of the `simple` fixture as text.
- **Markdown**: the guide card as Markdown equals the golden file and does not change when a narrow width is set.
- **JSON golden files**: the card, the index, and the examples document as JSON.
- **Link styles**: the command and the Markdown link style golden files, the JSON command, and a custom command
  template; a link to an example of the containing card is the full command in the command style, a link that names the
  card in the Markdown style, and short in the plain style, and the JSON body and its links agree.
- **Examples**: the examples document with the own examples in full and the descendants only listed, the card with the
  content of its examples in fences, and one example alone as exactly its content.
- **Expected results**: the examples document of the `runnable` fixture shows the exit code and the expected output
  after the fence, one example alone is exactly its content, and the JSON examples document appends `exitCode` and
  `expect`.
- **Lists**: the index down to the roots, a card list, search results, and the sentence for no results.
- **Wrapping**: a narrow width wraps the generated lines and leaves the authored body unchanged.
- **Options**: the defaults, and the argument errors for a width, a format, and a command template.
- **JSON contract**: a card of the contract source with commands, and a replaced top-level object.
- **Determinism**: two renderings under two cultures are equal, use LF only, and have no trailing line feed.
