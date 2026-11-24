### LinkFormatter Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`LinkFormatter` is the one place where links are written. It turns a visible link of a body, the label of a card in a
list, and the command that prints a card into text in the plain, command, or Markdown style, or into whatever the
callback of the caller returns.

#### Data Model

The class is static and stateless. It reads the options through the `RenderContext` it is given and the link through
`HelpLink`, which carries the global ids, the title, the display text, and the source.

| Link | Plain | Command | Markdown |
| --- | --- | --- | --- |
| Card | the card `a/b` | `tool help a b` | `[Title](a/b)` |
| Example | the example `x` of the card `a/b` | `tool help a b --example x` | `[x of Title](a/b#x)` |
| Example of the containing card | the example `x` | `--example x` | `[x](#x)` |
| External card | the card `s/a/b` | `tool help s a b` | `[text](s/a/b)` |

#### Key Methods

- **`FormatBody(context, card)`** walks the body pieces of the card in order, copies text, and replaces each visible
  link with `FormatLink`.
- **`FormatLink(context, link, containingCardId)`** calls the callback when one is set; otherwise it writes the style of
  the options. The short form applies only to the `[[#x]]` form whose example belongs to the containing card.
- **`FormatLabel(context, id, title, sourceName)`** writes the label of a card in a list: the id, the command, a
  Markdown link, or what the callback writes for a link to the card.
- **`Command(context, id)`** fills in the command template with the tool name and the id with slashes replaced by
  spaces.

#### Error Handling

Null arguments throw `ArgumentNullException`. A callback that returns null is treated as empty text.

#### Dependencies

`RenderContext`, `HelpRenderOptions`, `HelpCard` (its body pieces), and `HelpLink`.

#### Callers

`TextRenderer`, `JsonDocumentWriter` (for the command and the body), and `HelpCatalog.FormatBody`.
