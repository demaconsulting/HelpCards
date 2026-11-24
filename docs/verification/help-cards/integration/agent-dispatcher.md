### AgentDispatcher Verification Design

#### Verification Approach

The dispatcher is tested through the tool in `AgentDispatcherTests.cs`, over catalogs created from the embedded
`simple`, `big`, `lib-a`, and `lib-b` fixtures and from in-memory sources, including two mounted sources that have the
same card and example names. Answers are compared with exact expected text. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures; no sleeps or timers.

#### Acceptance Criteria

All tests in `AgentDispatcherTests.cs` pass; every miss says what to send instead.

#### Test Scenarios

- **Index**: the configured depth, one block for each source, restriction by source and tag, unknown names, and a tag
  in a catalog without tags.
- **Topic**: the Markdown card with the content of its examples, a path, an alias, any case, an unqualified local id, an
  ambiguous local id, and an unknown topic with and without suggestions.
- **Example**: one example with its fence, a fence that is longer than the backticks in the content, the topic that
  limits the search, an unknown topic, a missing example with suggestions, and several matches.
- **Query**: the best cards, the limits of the call and of the options, restriction by source and tag, and no match.
- **Conflicts**: a query that is ignored because of a topic, an example, or both.
- **Errors**: wrong arguments answered through the tool.
