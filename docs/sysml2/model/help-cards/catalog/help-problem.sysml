package HelpCards_HelpProblem {
    private import SoftwareItemMetadata::*;
    private import ScalarValues::*;

    enum def HelpSeverity {
        doc /* How serious a problem is: an informational note, a warning that only the validator reports, or an error that fails catalog creation. */

        enum info;
        enum warning;
        enum error;
    }

    part def HelpProblem {
        doc /* The immutable value with which every part of the library reports a finding about a help source: its severity, the rule that was broken, and where it was found (source, file, and line), with a message. Supporting types: HelpSeverity and the internal RuleIds constants. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/HelpProblem.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpSeverity.cs",
                "src/DemaConsulting.HelpCards/Catalog/RuleIds.cs"
            );
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Catalog/HelpProblemTests.cs");
            designFiles = ("docs/design/help-cards/catalog/help-problem.md");
            verificationFiles = ("docs/verification/help-cards/catalog/help-problem.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/help-problem.yaml");
        }

        attribute severity : HelpSeverity;
        attribute ruleId : String;
        attribute sourceName : String;
        attribute file : String;
        attribute line : Integer;
        attribute messageText : String;
    }
}
