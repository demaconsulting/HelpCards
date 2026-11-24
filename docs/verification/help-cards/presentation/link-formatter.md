### LinkFormatter Verification Design

#### Verification Approach

The formatter is tested directly in `LinkFormatterTests.cs` with links built by hand for each kind of target and with
the `simple` fixture for whole bodies. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded `simple` fixture; a catalog whose tool is named `tool`.

#### Acceptance Criteria

All tests in `LinkFormatterTests.cs` pass.

#### Test Scenarios

- **Plain, command, and Markdown styles**: a card, an example, an example of the containing card, and an external card
  in each style.
- **Short form**: only the plain style writes the short form, and only for an example of the containing card; the
  command style writes the full command (also with a custom command template), the Markdown style a link that names
  the card, and an example of another card than the containing one is written in the full form in every style.
- **Callback**: the callback replaces the style and receives the link it is asked to write.
- **Command template**: the default and a custom template with the tool and the path.
- **Labels**: the id, the command, the Markdown link, and the callback text.
- **Bodies**: the command style over the `simple` guide card, and the plain style equal to the body of every card of the
  fixture.
- **Arguments**: null arguments throw.
