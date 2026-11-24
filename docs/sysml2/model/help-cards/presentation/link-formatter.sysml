package HelpCards_LinkFormatter {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpRenderOptions::*;
    private import HelpCards_HelpCard::*;

    part def LinkFormatter {
        doc /* Writes links, card labels, and commands in the plain, command, and Markdown styles, or through the callback of the caller, and writes the body of a card from its parsed pieces with every visible link formatted. The short example form applies only to an example of the card that holds the link. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Presentation/LinkFormatter.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Presentation/LinkFormatterTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/presentation/link-formatter.md");
            verificationFiles = ("docs/verification/help-cards/presentation/link-formatter.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation/link-formatter.yaml");
        }

        // The formatter reads the options through the per-call context and the body pieces of the card.
        dependency readsContext from LinkFormatter to HelpRenderOptions;
        dependency readsBodyPieces from LinkFormatter to HelpCard;
    }
}
