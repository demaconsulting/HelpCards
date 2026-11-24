package HelpCards_HelpCli {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CliDispatcher::*;

    part def HelpCli {
        doc /* The command-line helper that a tool adds in front of its own argument parser: it is created without creating the catalog, answers a command line only when its first argument is the help verb, offers a constant footer sentence for the usage text, and runs a help command whose verb was removed. Supporting types: HelpCliOptions (tool name, version, verb, catalog factory, usage lines, preamble, sections, fallback resolver, writers, command template, default width, language option, and width provider), HelpCliSection, and HelpCliAnswer. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/HelpCli.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliOptions.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliSection.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliAnswer.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Integration/HelpCliTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CliHarness.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/integration/help-cli.md");
            verificationFiles = ("docs/verification/help-cards/integration/help-cli.md");
            requirementFiles = ("docs/reqstream/help-cards/integration/help-cli.yaml");
        }

        // The helper hands every help command to the dispatcher.
        dependency runsThrough from HelpCli to CliDispatcher;
    }
}