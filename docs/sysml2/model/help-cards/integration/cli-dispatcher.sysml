package HelpCards_CliDispatcher {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CliArgumentParser::*;
    private import HelpCards_HelpCatalog::*;

    part def CliDispatcher {
        doc /* Runs one help command: it reads the arguments, offers a card command to the fallback resolver of the tool, creates the catalog with the factory of the tool only then, selects the answer of the mode (index, card, examples, example, search, or tags) with the command link style, and writes it with the exit code of the contract: 0 for success, 2 for wrong arguments and unknown names, 70 for a defective catalog. The class is partial: the flow, the answers of the modes, and the lookups. Supporting types: CliOutcome and CliFailure. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/CliDispatcher.cs",
                "src/DemaConsulting.HelpCards/Integration/CliDispatcher.Modes.cs",
                "src/DemaConsulting.HelpCards/Integration/CliDispatcher.Lookup.cs",
                "src/DemaConsulting.HelpCards/Integration/CliOutcome.cs",
                "src/DemaConsulting.HelpCards/Integration/CliFailure.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Integration/CliDispatcherTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Integration/CliDispatcherTests.Modes.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CliHarness.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/GoldenFiles.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/integration/cli-dispatcher.md");
            verificationFiles = ("docs/verification/help-cards/integration/cli-dispatcher.md");
            requirementFiles = ("docs/reqstream/help-cards/integration/cli-dispatcher.yaml");
        }

        // The dispatcher reads its request from the parser and answers from the catalog; the units of the Presentation
        // subsystem are named by qualified name because they import the Catalog package.
        dependency readsRequestFrom from CliDispatcher to CliArgumentParser;
        dependency answersFrom from CliDispatcher to HelpCatalog;
        dependency writesErrorsThrough from CliDispatcher to HelpCards_JsonDocumentWriter::JsonDocumentWriter;
        dependency readsOptions from CliDispatcher to HelpCards_HelpRenderOptions::HelpRenderOptions;
    }
}