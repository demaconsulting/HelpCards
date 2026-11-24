package HelpCards_TestingSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpCardValidator::*;
    public import HelpCards_HelpExamples::*;
    public import HelpCards_ExampleCommandLine::*;
    public import HelpCards_HelpCardMarkdown::*;

    part def TestingSubsystem {
        doc /* The subsystem of test helpers for adopters: HelpCardValidator (with HelpValidationOptions and IHelpCardRule) validates sources and catalogs and returns problems or throws; HelpExamples enumerates the examples of a catalog; ExampleCommandLine reads the command line of an example into arguments with a safe grammar; HelpCardMarkdown reads the headings, sections, and bullets of a card body. The subsystem references no test framework. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Testing/TestingTests.cs");
            designFiles = ("docs/design/help-cards/testing.md");
            verificationFiles = ("docs/verification/help-cards/testing.md");
            requirementFiles = ("docs/reqstream/help-cards/testing.yaml");
        }

        part helpCardValidator : HelpCardValidator;
        part helpExamples : HelpExamples;
        part exampleCommandLine : ExampleCommandLine;
        part helpCardMarkdown : HelpCardMarkdown;
    }
}
