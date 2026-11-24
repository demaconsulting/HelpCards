package HelpCards_TextRenderer {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpRenderOptions::*;
    private import HelpCards_LinkFormatter::*;
    private import HelpCards_TextWrapper::*;
    private import HelpCards_HelpStrings::*;
    private import HelpCards_HelpCard::*;

    part def TextRenderer {
        doc /* Renders cards, indexes, example documents, search results, and card lists as text and Markdown: the card layout with sections that are left out when empty, the examples as bullets or fenced sections, the hint for printing examples in the command style, the examples document with the cards below listed by name, one example as exactly its content, and the two-column lists. Markdown is the same layout without wrapping and with bold labels. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Presentation/TextRenderer.cs",
                "src/DemaConsulting.HelpCards/Presentation/TextRenderer.Examples.cs",
                "src/DemaConsulting.HelpCards/Presentation/TextRenderer.Lists.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Presentation/TextRendererTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Presentation/TextRendererTests.Examples.cs",
                "test/DemaConsulting.HelpCards.Tests/Presentation/TextRendererTests.Lists.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/GoldenFiles.cs"
            );
            designFiles = ("docs/design/help-cards/presentation/text-renderer.md");
            verificationFiles = ("docs/verification/help-cards/presentation/text-renderer.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation/text-renderer.yaml");
        }

        // The renderer takes its settings from the context, writes links through the formatter, wraps with the wrapper,
        // and takes its headings and hints from the strings.
        dependency readsContext from TextRenderer to HelpRenderOptions;
        dependency writesLinks from TextRenderer to LinkFormatter;
        dependency wrapsLines from TextRenderer to TextWrapper;
        dependency writesChrome from TextRenderer to HelpStrings;
        dependency rendersCards from TextRenderer to HelpCard;
    }
}
