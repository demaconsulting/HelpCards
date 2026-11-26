package HelpCards_HelpCli {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CliDispatcher::*;
    private import HelpCards_HelpCatalogBuilder::*;

    part def HelpCli {
        doc /* The command-line helper that a tool adds in front of its own argument parser: it is created without creating the catalog, answers a command line only when its first argument is the help verb, offers a constant footer sentence for the usage text, and runs a help command whose verb was removed. The tool name and the version are set once, in HelpCliOptions: with the catalog configuration action the helper sets them (and the culture) on the catalog builder, and GetCatalogConfiguration gives the same configuration to the tests of the tool. Supporting types: HelpCliOptions (tool name, version, verb, catalog configuration or factory, usage lines, preamble, sections, fallback resolver, writers and writer providers, command template, default width, language option, and width provider), HelpCliSection, HelpCliAnswer, HelpCliError (the data of a failure for the JSON error callback), and HelpCliExitCodes (the exit codes of success, usage errors, and internal errors). The options also carry the contract settings of a tool with a JSON contract of its own: the JSON envelope callback, the JSON error callback, and the exit codes. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/HelpCli.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliOptions.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliSection.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliAnswer.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliError.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpCliExitCodes.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Integration/HelpCliTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Integration/HelpCliTests.Contract.cs",
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

        // The catalog configuration of the options works on the public builder of the Composition subsystem.
        dependency configuresBuilder from HelpCli to HelpCatalogBuilder;
    }
}