package HelpCards_CatalogScope {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CatalogData::*;

    part def CatalogScope {
        doc /* The set of cards a view shows and the tree those cards form when hidden cards are lifted out: it is created over the frozen data, restricted by intersection to make a view, and computes the visible cards, the lifted roots, the visible children, and the nearest visible ancestor once. The name differs from the SysML2 view CatalogView on purpose. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Catalog/CatalogScope.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/CatalogScopeTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/catalog-scope.md");
            verificationFiles = ("docs/verification/help-cards/catalog/catalog-scope.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/catalog-scope.yaml");
        }

        // A scope is a view over the frozen data.
        dependency viewsData from CatalogScope to CatalogData;
    }
}
