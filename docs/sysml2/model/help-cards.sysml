package HelpCards {
    private import SoftwareItemMetadata::*;
    public import HelpCards_LoadingSubsystem::*;
    // The problem model belongs to the Catalog subsystem; until that subsystem has its own facade the system owns it directly.
    public import HelpCards_HelpProblem::*;

    doc
    /*
     * HelpCards is a .NET library for authoring help and knowledge content as
     * Markdown cards with YAML front matter, embedded in assemblies and served as
     * command-line help for people and as searchable knowledge for AI agents.
     */

    part def HelpCardsSystem {
        doc /* The HelpCards system as a whole. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/HelpCardsTests.cs");
            designFiles = ("docs/design/help-cards.md");
            verificationFiles = ("docs/verification/help-cards.md");
            requirementFiles = ("docs/reqstream/help-cards.yaml");
        }

        part loading : LoadingSubsystem;
        part helpProblem : HelpProblem;
    }
}
