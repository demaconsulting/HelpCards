### HelpCatalogBuilder Verification Design

#### Verification Approach

The builder is tested directly in `HelpCatalogBuilderTests.cs`: configuration methods are called with valid and
invalid arguments, and the pipeline is run by composing in-memory sources and the embedded fixtures. The units it uses
(Loading, `ReferenceValidator`, `SiblingOrder`, `ProblemCollector`) are real, not mocked, because the pipeline is their
collaboration; the unit tests of those units cover them separately.

#### Test Environment

Standard xUnit v3 runner; in-memory files; no temporary files.

#### Acceptance Criteria

All tests in `HelpCatalogBuilderTests.cs` pass: invalid arguments throw the documented exception, a valid source
composes to data, and each defect gives exactly the documented problem at the documented file and line.

#### Test Scenarios

- **Configuration**: null source, second source, and source with imports; null culture, blank tool name, null version,
  invalid vocabulary tags, required tag not allowed, and invalid expected root; chaining returns the same builder;
  composing without a source is an argument error.
- **Valid source**: the cards, the source description with defaults, the built-in tokens from the builder (and the
  source name as default tool), and the translation selected by the culture.
- **Tree placement**: a missing parent is reported once and the cards below it are silent; the children of a file that
  failed to load are silent.
- **Tags**: an unknown tag at the line of the tag; a card without a required tag.
- **Expected roots**: missing and unexpected roots are named; a matching set passes; a root that failed to load counts
  as present; an unsupported schema suppresses the check.
- **Collect all**: three different defects in three files are reported in file order and no data is returned.
- **Overlay**: an overlay file is read and the card it extends is unchanged.
- **Diagnostics**: a link to an absent source leaves one informational problem in the result and in the data.
