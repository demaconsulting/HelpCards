package HelpCards_JsonDocumentWriter {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpRenderOptions::*;
    private import HelpCards_LinkFormatter::*;
    private import HelpCards_HelpCard::*;

    part def JsonDocumentWriter {
        doc /* Writes the documents of JSON contract version 1 (index, card, examples, example, search, cards, and error) on one line of ASCII with the keys in a fixed order, optional values left out (the exit code and expected output of an example only when stated), arrays always present, and no null; the top-level object can be replaced by a callback of the caller, and non-ASCII characters are escaped whichever envelope is used. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Presentation/JsonDocumentWriter.cs",
                "src/DemaConsulting.HelpCards/Presentation/JsonDocumentWriter.Cards.cs",
                "src/DemaConsulting.HelpCards/Presentation/JsonDocumentWriter.Lists.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Presentation/JsonDocumentWriterTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Presentation/JsonDocumentWriterTests.Contract.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/RenderSources.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/GoldenFiles.cs"
            );
            designFiles = ("docs/design/help-cards/presentation/json-document-writer.md");
            verificationFiles = ("docs/verification/help-cards/presentation/json-document-writer.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation/json-document-writer.yaml");
        }

        // The writer takes its settings from the context and writes the body and commands through the formatter.
        dependency readsContext from JsonDocumentWriter to HelpRenderOptions;
        dependency writesLinks from JsonDocumentWriter to LinkFormatter;
        dependency rendersCards from JsonDocumentWriter to HelpCard;
    }
}
