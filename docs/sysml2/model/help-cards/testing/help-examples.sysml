package HelpCards_HelpExamples {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalog::*;
    private import HelpCards_HelpCard::*;

    part def HelpExamples {
        doc /* Enumerates the examples of the visible cards of a catalog or view as card and example pairs in catalog order, optionally of some kinds, for theory-style tests of commands, source files, and snippets. The class is static and stateless. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Testing/HelpExamples.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Testing/HelpExamplesTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/testing/help-examples.md");
            verificationFiles = ("docs/verification/help-cards/testing/help-examples.md");
            requirementFiles = ("docs/reqstream/help-cards/testing/help-examples.yaml");
        }

        // The enumeration reads the cards of a catalog and the examples of each card.
        dependency readsCatalog from HelpExamples to HelpCatalog;
        dependency readsCards from HelpExamples to HelpCard;
    }
}
