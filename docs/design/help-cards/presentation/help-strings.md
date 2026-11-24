### HelpStrings Design

![Presentation Structure](PresentationView.svg)

#### Purpose

`HelpStrings` holds every word that a rendering writes around authored content: the headings `Cards`, `Examples`, and
`See also`, the heading of the cards below with examples, the labels `Also known as` and `Tags`, the hint sentences for
printing examples, the lines that state the expected exit code and output of a command example, and the sentences for an
empty list. The literal heading markers (#, ##, and ###) are layout
and stay with TextRenderer; this unit holds the words. Chrome is English in this release and is never localized; this
unit is the seam where localization would be added.

#### Data Model

The class is static. The headings and labels are constants (CardsHeading, ExamplesHeading, SeeAlsoHeading,
ExamplesBelowHeading, AliasesLabel, TagsLabel, ExampleNamesLabel for the example names of a descendant, NoOwnExamples
for a card with no examples of its own, and NoCards for an empty catalog); the hints and sentences are functions of
their arguments only, so they cannot depend on the culture of the machine. JSON keys are not chrome and are not defined
here.

#### Key Methods

- **`Heading(title)`** and **`ExamplesTitle(cardTitle)`** write the second-level heading and the title of an examples
  document.
- **`PrintExamplesHint(command)`** and **`PrintOneExampleHint(command)`** write the hint sentences with the command.
- **`ExpectedExitCode(exitCode)`** and **`ExpectedOutputContains(fragment)`** write the lines that follow a command
  example with an expected result.
- **`NoResults(query)`** writes the sentence for a search without results.
- **`Label(label, markdown)`** writes a label with its colon, in bold for Markdown.

#### Error Handling

None: the unit has no failure cases.

#### Dependencies

None.

#### Callers

`TextRenderer`.
