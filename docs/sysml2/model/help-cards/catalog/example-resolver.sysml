package HelpCards_ExampleResolver {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CatalogScope::*;
    private import HelpCards_PathResolver::*;

    part def ExampleResolver {
        doc /* Finds examples by name: the first example in a card and the cards below it, searched depth first with the children the caller supplies (used by composition over the final tree and by catalogs over a view), and every example in a view, optionally limited to the subtree of a card given as a path or id. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Catalog/ExampleResolver.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/ExampleResolverTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/example-resolver.md");
            verificationFiles = ("docs/verification/help-cards/catalog/example-resolver.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/example-resolver.yaml");
        }

        // The resolver walks the visible tree and finds limiting cards by path.
        dependency asksScope from ExampleResolver to CatalogScope;
        dependency findsLimitCards from ExampleResolver to PathResolver;
    }
}
