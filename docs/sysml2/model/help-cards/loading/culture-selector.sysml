package HelpCards_CultureSelector {
    private import SoftwareItemMetadata::*;

    part def CultureSelector {
        doc /* Chooses which culture variant of a file supplies the text for an explicitly requested culture: the fallback chain removes the last subtag each time, the invariant culture selects the neutral files only, and the choice is made per file. The ambient UI culture is never read. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Loading/CultureSelector.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Loading/CultureSelectorTests.cs");
            designFiles = ("docs/design/help-cards/loading/culture-selector.md");
            verificationFiles = ("docs/verification/help-cards/loading/culture-selector.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/culture-selector.yaml");
        }
    }
}
