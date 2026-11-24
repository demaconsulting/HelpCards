package HelpCards_CatalogData {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpProblem::*;

    part def CatalogData {
        doc /* The immutable result of composition, shared by a catalog and all of its views: every card node in depth-first order with the resolved body and see-also ids, the root ids, the sources, the informational diagnostics, the distinct tags, the culture, the optional tool name and version, and the optional closed tag vocabulary. Supporting types: CardNode, ResolvedPart, and ResolvedLink. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/CatalogData.cs",
                "src/DemaConsulting.HelpCards/Catalog/CardNode.cs",
                "src/DemaConsulting.HelpCards/Catalog/ResolvedPart.cs",
                "src/DemaConsulting.HelpCards/Catalog/ResolvedLink.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/CatalogDataTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/catalog-data.md");
            verificationFiles = ("docs/verification/help-cards/catalog/catalog-data.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/catalog-data.yaml");
        }

        // The data holds the informational problems of composition.
        dependency holdsDiagnostics from CatalogData to HelpProblem;
    }
}
