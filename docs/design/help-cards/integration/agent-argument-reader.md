### AgentArgumentReader Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`AgentArgumentReader` turns the JSON text of a call into checked arguments, or into the message that tells the agent
what to send instead. It is the only place that knows the argument schema.

#### Data Model

The class is static and stateless. Its results are `AgentArguments` (topic, example, query, source, tag, and a result
maximum, each possibly absent) and `AgentReadResult` (the arguments or the message).

#### Key Methods

- **`Read(argumentsJson)`** returns empty arguments for null, empty, or white-space text, JSON `null`, and an empty
  object. Invalid JSON gives `The arguments are not valid JSON ({reason}). Send an object such as {"topic":"svg"}.`,
  with the reason of the parser without its position; JSON that is not an object gives
  `The arguments must be a JSON object.` A repeated property counts once, the last. The first unknown property in
  ordinal order gives `Unknown argument 'x'. Valid arguments: topic, example, query, source, tag, maxResults.` before
  the types are checked. A text property that is not a string, or a `maxResults` that is not an integer from 1 to 20,
  gives a message that names it. A `null`, empty, or white-space text property is absent; text is trimmed.

#### Error Handling

Nothing in the text can make the reader throw `JsonException`: it is caught and reported. Other exceptions of the JSON
parser, for example for text that is not valid UTF-16, pass to the boundary of the tool.

#### Dependencies

`System.Text.Json` of the base class library.

#### Callers

`HelpAgentTool`.
