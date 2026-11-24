package HelpCards {
    private import SoftwareItemMetadata::*;
    private import OtsDependencies::*;
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
     * command-line help for people and as searchable knowledge for AI agents. The NuGet package also ships the build
     * files build/DemaConsulting.HelpCards.props and build/DemaConsulting.HelpCards.targets, which embed the help
     * cards of a referencing project; they are part of this system and have no unit of their own.
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

        // Off-the-shelf tools of the build, test, and compliance pipeline; there are no runtime dependencies.
        ref part xUnit : XUnit;
        ref part reqStream : ReqStream;
        ref part apiMark : ApiMark;
        ref part buildMark : BuildMark;
        ref part versionMark : VersionMark;
        ref part sarifMark : SarifMark;
        ref part sonarMark : SonarMark;
        ref part sysml2Tools : SysML2Tools;
        ref part reviewMark : ReviewMark;
        ref part pandoc : Pandoc;
        ref part weasyPrint : WeasyPrint;
        ref part fileAssert : FileAssert;
    }
}
