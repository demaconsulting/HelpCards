### AgentDispatcher Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`AgentDispatcher` answers one call of the knowledge tool from checked arguments. It chooses the answer and writes it as
plain text that is also valid Markdown, with a message for every miss that says what to send instead.

#### Data Model

A dispatcher holds the catalog and the options of the tool, and nothing else, so any number of calls may run at the same
time. The class is partial: `AgentDispatcher.cs` holds the choice of the answer and the index;
`AgentDispatcher.Lookup.cs` holds the lookups of topics, examples, and search results.

#### Key Methods

- **`Answer(arguments)`** applies the first rule that fits: an example, else a topic, else a query, else the index.
- **`IgnoredNotice(arguments)`** (static) gives the line `Ignored 'query' because '{argument}' was given.` when a query
  was given together with an example or a topic, and none otherwise. The tool appends it after it cuts the answer, so a
  long answer cannot cut it away. Of the other arguments, `source` and `tag` are silently not used when a topic or an
  example is given; the spec requires a notice for the query only.
- **Index** is a first line that says how to use the tool and the index in Markdown to the depth of the options; with
  several sources and no restriction, one block for each source with its title and summary. A `source` or `tag` argument
  restricts the index to a view; an unknown name is answered with the valid names, and a tag in a catalog that has no tag
  at all with `No cards have the tag '{tag}'; this help has no tags.`
- **Topic** resolves the exact path or global id with `Find`, else an unqualified local id when exactly one card has it.
  A card is rendered in Markdown with plain links and the content of its examples. An ambiguous local id lists the
  global ids; an unknown topic gets up to five suggestions and a pointer to the index.
- **Example** searches with `FindExamples`, limited to the card of the topic and the cards below it when a topic is
  given. One match is the description line with the kind, language, and file, and the content in a fence one backtick
  longer than the longest run in the content. Several matches list `card#name` values; none lists the closest example
  names, or the closest card names when no example name is close.
- **Query** searches the view that the source and tag arguments make, with the limit of the call or of the options, and
  lists the cards; no match points to the index.

#### Error Handling

Every wrong or empty request is an answer, not an exception. Null arguments throw `ArgumentNullException`.

#### Dependencies

`HelpCatalog` (lookup, views, search, suggestions, rendering) and `SearchIndex` for the closest example names.

#### Callers

`HelpAgentTool`.
