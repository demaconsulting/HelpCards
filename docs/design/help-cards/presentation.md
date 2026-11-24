## Presentation Subsystem Design

![Presentation Structure](PresentationView.svg)

### Overview

The Presentation subsystem turns the immutable catalog into text, Markdown, and JSON: a card, the index, the examples
of a card, one example, search results, and a list of cards, with three link styles. Its public face is
`HelpRenderOptions`, with `HelpFormat` and `HelpLinkStyle`; the `Render` members that call it are a partial part of
`HelpCatalog` in the Catalog subsystem. Rendering is a pure function of the catalog view and the options: nothing is
cached, nothing is read from disk, and the output is byte-identical on every machine.

The subsystem contains these units:

- **HelpRenderOptions** (public) holds the options of a call with their defaults and checks. With it go the public
  enumerations `HelpFormat` and `HelpLinkStyle` and the internal `RenderContext`, which resolves the options against a
  catalog view for one call and offers the walks over the visible tree that several renderers share.
- **LinkFormatter** (internal) writes links, card labels, and commands in the plain, command, and Markdown styles or
  through the callback of the caller, and writes a body from its parsed pieces.
- **TextRenderer** (internal) renders the text and Markdown documents. It is a partial class: one part for the card,
  one for the example documents, and one for the index, card lists, and search results.
- **JsonDocumentWriter** (internal) writes the documents of JSON contract version 1 and the error document. It is a
  partial class with the same split as the text renderer.
- **TextWrapper** (internal) wraps generated lines to a width with a hanging indent.
- **HelpStrings** (internal) holds the English headings, labels, and hint sentences.

### Interfaces

| Interface | Direction | Purpose |
| --- | --- | --- |
| `HelpRenderOptions` | Inbound (adopters) | Choose format, link style, template, width, and callbacks |
| `HelpCatalog.Render*`, `FormatBody` | Inbound (adopters) | Render a card, index, examples, or list |
| `HelpCard.BodyPieces`, `HelpCatalog.Scope` | Outbound (Catalog) | Read body pieces and the visible tree |
| `JsonDocumentWriter.WriteError` | Inbound (`HelpCli`) | Write the error document; no public member exists |
| Internal `Render*` overloads | Inbound (`HelpCli`) | Add usage lines and list filter |

The Presentation subsystem depends only on the Catalog subsystem and the Base Class Library (`System.Text.Json` for
JSON). The Catalog subsystem calls it only from the partial `HelpCatalog.Render.cs`; the plain-style body of a card
(`HelpCard.Body`) is written by the card itself and the formatter reproduces it, which a test checks for every card.

### Design

**One context per call.** Every `Render` member creates a `RenderContext` from the catalog view and the options. The
members that take one card (`Render`, `RenderExamples`, `RenderExample`, and `FormatBody`) map the card to the card of
that view (so a card of another view is rendered with the rules of this one and a hidden card is rejected).
`RenderCardList` maps every card of its list the same way. `RenderSearch` does not map or validate its results: it
labels them as given, so a caller passes results found in the same view. The context is then handed to a renderer. The
context holds the tool name (the one given to the builder, else the name of the first source), the effective wrapping
width (zero for Markdown), and the tree walks.

**Layout.** The text layout is fixed: title, summary, aliases, tags, body, then the sections Cards, Examples, and See
also, each left out when empty. Markdown is the same layout without wrapping and with bold labels. Examples are listed
as one bullet each, or as sections with fenced content when the options ask for the content; the fence is one backtick
longer than the longest run in the content, and a command that states its expected exit code or output is followed by
the lines `Expected exit code: N` (only when not 0) and `Expected output contains: fragment`. The command link style
adds one hint sentence after the examples list. The index is a two-column list with labels indented two spaces per level
and padded to the widest.

**Links.** The formatter takes the parsed pieces of the body and writes each visible link: plain words, a command, or a
Markdown link, or whatever the callback returns. A link to an example of the card that holds the link is written in a
short form only in the plain style; the command and Markdown styles name the card. Links to hidden targets were already
turned into text by the catalog view, so the formatter and the callback see visible targets only.

**Wrapping.** Only generated lines are wrapped, at the configured width, never inside a word, with the prefix counted
and a hanging indent; the authored body is copied as it is.

**JSON.** The writer produces one document per call with `Utf8JsonWriter`, in the key order of the contract, with the
envelope `helpSchemaVersion`, `ok`, and `kind` or the envelope of the caller. Optional values are left out, arrays are
always written, and every character outside printable ASCII is replaced by a `\uXXXX` escape after the document is
written, so the rule holds for the default envelope and for a callback envelope alike.

**Failure model.** A null argument throws `ArgumentNullException`; a single card that the view does not show, an unknown
example, a negative depth, an invalid option, and an unknown error code throw `ArgumentException` or a subclass. Options
are checked where they are set. Rendering never throws for the content of cards.

The unit design documents describe each unit in detail; the verification design describes how the subsystem is tested
through the options and the `Render` members.
