package HelpCards {
    private import SoftwareItemMetadata::*;
    public import HelpCards_LoadingSubsystem::*;
    public import HelpCards_CompositionSubsystem::*;
    public import HelpCards_CatalogSubsystem::*;
    public import HelpCards_PresentationSubsystem::*;
    public import HelpCards_IntegrationSubsystem::*;
    public import HelpCards_TestingSubsystem::*;

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
        part composition : CompositionSubsystem;
        part catalog : CatalogSubsystem;
        part presentation : PresentationSubsystem;
        part integration : IntegrationSubsystem;
        part testing : TestingSubsystem;
    }
}
