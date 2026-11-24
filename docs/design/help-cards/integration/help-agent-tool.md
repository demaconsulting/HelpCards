### HelpAgentTool Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`HelpAgentTool` is the knowledge tool that gives an AI agent one way to read the help of a whole catalog. It is
independent of any agent framework: a host registers the name, description, and schema, and passes the JSON arguments
of each call to `Invoke`.

#### Data Model

An immutable object holding the name, the description, the schema, the output limit, and a function that answers checked
arguments (the `AgentDispatcher` of the catalog). `HelpAgentToolOptions` holds the name (`help`), the description, the
result limit (8), the output limit (16000), the index depth (2), and the product name. `HelpAgentTools` is the static
factory. The schema is a constant of the class and is the exact one-line text of the contract.

#### Key Methods

- **`HelpAgentTools.Create(catalog, options)`** checks the options (name pattern `^[a-z][a-z0-9_]{0,63}$`, results 1 to
  20, output 1000 to 200000, depth 0 to 8, descriptions and tool names that are not white space), generates the
  description when none is given, and creates the tool. The generated description names the product, lists every source
  with its summary, and gives the three ways to call the tool: no arguments, `topic` or `example`, and `query`.
- **`Invoke(argumentsJson)`** reads the arguments, asks the dispatcher for the answer, and cuts it to the output limit.
  An answer over the limit is cut at the last line feed at or before the limit, or at the limit when there is none,
  without splitting a surrogate pair, and
  `\n\n[Truncated: {n} characters omitted. Ask for a narrower topic or an example.]` is appended. A
  `HelpCatalogException`, `ArgumentException`, `InvalidOperationException`, `KeyNotFoundException`, `FormatException`,
  or `JsonException` becomes `The help tool could not answer ({ExceptionTypeName}).` and never the text of the
  exception.

#### Error Handling

Programming mistakes in the options throw `ArgumentException`, `ArgumentOutOfRangeException`, or
`ArgumentNullException` when the tool is created. `Invoke` does not throw for any text; it does not catch out-of-memory,
cancellation, or other conditions of the process.

#### Dependencies

`AgentArgumentReader`, `AgentDispatcher`, and `HelpCatalog`.

#### Callers

Hosts that register the tool with an agent framework.
