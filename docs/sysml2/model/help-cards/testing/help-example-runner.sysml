package HelpCards_HelpExampleRunner {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalog::*;
    private import HelpCards_HelpCard::*;
    private import HelpCards_HelpProblem::*;
    private import HelpCards_ExampleCommandLine::*;

    part def HelpExampleRunner {
        doc /* Saves the source examples of a card to a folder and runs its command examples one after another through a delegate of the caller (the library never starts a process or a shell), comparing each exit code and output with the exit and expect keys of the example and reporting every difference as a problem of a run report. The catalog overload gives every card with commands a fresh scratch folder and deletes it afterwards. Supporting types: HelpExampleInvocation, HelpExampleOutcome, and HelpExampleRunReport. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Testing/HelpExampleRunner.cs",
                "src/DemaConsulting.HelpCards/Testing/HelpExampleInvocation.cs",
                "src/DemaConsulting.HelpCards/Testing/HelpExampleOutcome.cs",
                "src/DemaConsulting.HelpCards/Testing/HelpExampleRunReport.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Testing/HelpExampleRunnerTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/TemporaryDirectory.cs"
            );
            designFiles = ("docs/design/help-cards/testing/help-example-runner.md");
            verificationFiles = ("docs/verification/help-cards/testing/help-example-runner.md");
            requirementFiles = ("docs/reqstream/help-cards/testing/help-example-runner.yaml");
        }

        // The runner reads the cards of a catalog and the examples of each card, splits every command with the
        // command line grammar, and reports the differences as problems.
        dependency readsCatalog from HelpExampleRunner to HelpCatalog;
        dependency readsCards from HelpExampleRunner to HelpCard;
        dependency splitsCommands from HelpExampleRunner to ExampleCommandLine;
        dependency reportsProblems from HelpExampleRunner to HelpProblem;
    }
}
