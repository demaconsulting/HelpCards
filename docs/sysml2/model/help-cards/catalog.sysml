package HelpCards_CatalogSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpCatalog::*;
    public import HelpCards_HelpCard::*;
    public import HelpCards_HelpProblem::*;
    public import HelpCards_CatalogData::*;
    public import HelpCards_CatalogScope::*;
    public import HelpCards_PathResolver::*;
    public import HelpCards_ExampleResolver::*;
    public import HelpCards_SearchIndex::*;

    part def CatalogSubsystem {
        doc /* The subsystem that holds the result of composition and gives it to callers: HelpCatalog creates a catalog and exposes the cards, lookups, and filtered views; HelpCard and its supporting types are the immutable model; HelpProblem with HelpSeverity and HelpCatalogException is the problem model; CatalogData is the frozen data that a catalog and all of its views share; CatalogScope decides which cards a view shows and lifts the children of hidden cards; PathResolver and ExampleResolver look up cards and examples; SearchIndex ranks cards for a query and suggests names close to a word that was not found. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Catalog/CatalogTests.cs");
            designFiles = ("docs/design/help-cards/catalog.md");
            verificationFiles = ("docs/verification/help-cards/catalog.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog.yaml");
        }

        part helpCatalog : HelpCatalog;
        part helpCard : HelpCard;
        part helpProblem : HelpProblem;
        part catalogData : CatalogData;
        part catalogScope : CatalogScope;
        part pathResolver : PathResolver;
        part exampleResolver : ExampleResolver;
        part searchIndex : SearchIndex;
    }
}
