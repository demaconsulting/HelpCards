### TextRenderer Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`TextRenderer` writes every text and Markdown document: a card, the index, the examples of a card, one example,
search results, and a list of cards. Markdown is the same layout as text without wrapping and with bold labels.

#### Data Model

The class is static, stateless, and partial: `TextRenderer.cs` holds the card document and the shared helpers,
`TextRenderer.Examples.cs` holds the example documents and the fence, and `TextRenderer.Lists.cs` holds the index, the
card list, and the search results. The document being written is a `StringBuilder` in which every line ends with a line
feed; the last line feed is removed when the document is finished.

#### Key Methods

- **`RenderCard`** writes `# {Title}`, the wrapped summary, the aliases and tags lines, the body with its links
  formatted, and the sections `## Cards`, `## Examples`, and `## See also`, each only when it has entries. Examples are
  one bullet each, `- name [kind] file: description`, or `### name [kind] file` sections with the description and the
  fenced content when the options ask for it. With the command style, the hint sentence follows the examples.
- **`RenderExamples`** writes `# {Title}: examples`, each own example in full under `## name [kind] file`, the hint for
  printing one alone for commands, and `## Cards below with examples` with a bullet and the example names for each
  descendant that has examples; the content of those examples is not printed.
- **`RenderExample`** returns exactly the content of the example.
- **`RenderIndex`**, **`RenderCardList`**, and **`RenderSearch`** write two columns: the label (indented two spaces per
  level in the index), padded to the widest label, two spaces, and the summary wrapped at the hanging indent. An empty
  list writes one sentence.
- **`CreateFence`** returns a fence of at least three backticks and one more than the longest run in the content.

#### Error Handling

Null arguments throw `ArgumentNullException`; an unknown example throws `ArgumentException` through the context.
Rendering never fails for the content of a card.

#### Dependencies

`RenderContext`, `LinkFormatter`, `TextWrapper`, `HelpStrings`, `HelpCard`, `HelpExample`, and `HelpSearchResult`.

#### Callers

The `Render*` members of `HelpCatalog` when the format is `Text` or `Markdown`.
