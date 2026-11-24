### HelpRenderOptions Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`HelpRenderOptions` is the public options object of every rendering: it chooses the format, the link style, the command
template, the wrapping width, whether tags and example content are shown, and the two caller callbacks. With it go the
public enumerations `HelpFormat` (`Text`, `Markdown`, `Json`) and `HelpLinkStyle` (`Plain`, `Command`, `Markdown`) and
the internal `RenderContext`, which resolves the options against a catalog view for one call.

#### Data Model

| Member | Meaning |
| --- | --- |
| `Format`, `LinkStyle` | The output format and the link style; `Text` and `Plain` by default |
| `CommandTemplate` | The command that prints a card, with `{tool}` and `{path}`; null means `{tool} help {path}` |
| `LinkFormatter` | A callback that writes every link and overrides the link style |
| `Width` | The wrapping width; 78 by default, 0 for none, 20 to 500 otherwise |
| `IncludeTags`, `IncludeExampleContent` | Whether a card shows its tags (yes by default) and example content (no) |
| `JsonEnvelope` | A callback that writes the top-level JSON object around the fields of a document |

`RenderContext` holds the catalog view, the options (never null), the tool name, the version, and the derived values
`WrapWidth` (zero for Markdown), `IsMarkdown`, and `UsesCommands`. It is created once per call and never cached.

#### Key Methods

- **Property setters** check their value when they are set: an enumeration value that is not defined, a width outside
  zero and 20 to 500, and a command template of white space throw an argument exception.
- **`RenderContext.Create(catalog, options)`** (a catalog and options that may be null; returns the context) resolves
  the tool name: the name given to the builder, else the name of the first source, else `tool`.
- **`Own(card)`** (a non-null `HelpCard`; returns a `HelpCard`) returns the card of the catalog view with the id of the
  given card, so that a card of another view is rendered with the rules of this one, and throws for a card the view does
  not show.
- **`Flatten(maxDepth)`** (an `int`; returns a list of card and depth) lists the visible cards depth first with their
  depth, down to a depth. It does not validate the depth: a negative depth lists the roots only, and the check that
  rejects a negative depth lives in `HelpCatalog.RenderIndex`.
- **`DescendantsWithExamples(card)`** (a visible `HelpCard`) lists the cards below a card that have examples of their
  own.
- **`RequireExample(card, name)`** (a card and a non-null name; returns the `HelpExample`) finds an example in a card or
  the cards below it, or throws.

#### Error Handling

Invalid option values and a card the view does not show or an unknown example throw `ArgumentException` or a subclass;
null arguments throw `ArgumentNullException`.

#### Dependencies

`HelpCatalog` (its scope, tool name, children, and example search), `HelpCard`, and `HelpExampleReference` of the
Catalog subsystem.

#### Callers

Adopters set the options; every renderer and every `Render` member reads the context.
