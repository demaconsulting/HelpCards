### HelpStrings Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`HelpStrings` holds every word that a rendering writes around authored content: the headings `Cards`, `Examples`, and
`See also`, the heading of the cards below with examples, the labels `Also known as` and `Tags`, the hint sentences for
printing examples, and the sentences for an empty list. Chrome is English in this release and is never localized; this
unit is the seam where localization would be added.

#### Data Model

The class is static. The headings and labels are constants; the hints and sentences are functions of their arguments
only, so they cannot depend on the culture of the machine. JSON keys are not chrome and are not defined here.

#### Key Methods

- **`Heading(title)`** and **`ExamplesTitle(cardTitle)`** write the second-level heading and the title of an examples
  document.
- **`PrintExamplesHint(command)`** and **`PrintOneExampleHint(command)`** write the hint sentences with the command.
- **`NoResults(query)`** writes the sentence for a search without results.
- **`Label(label, markdown)`** writes a label with its colon, in bold for Markdown.

#### Error Handling

None: the unit has no failure cases.

#### Dependencies

None.

#### Callers

`TextRenderer`.
