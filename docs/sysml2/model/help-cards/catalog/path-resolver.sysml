package HelpCards_PathResolver {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CatalogScope::*;

    part def PathResolver {
        doc /* Resolves a path of names and aliases, separated by slashes or white space and compared ignoring case after normalization, to a card; walks through hidden cards but reports a hidden target like an unknown segment; and, when a path fails, names the parent that was searched, the segment that matched nothing, and the visible names at that level. Supporting type: PathMatch. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/PathResolver.cs",
                "src/DemaConsulting.HelpCards/Catalog/PathMatch.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/PathResolverTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/path-resolver.md");
            verificationFiles = ("docs/verification/help-cards/catalog/path-resolver.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/path-resolver.yaml");
        }

        // The resolver decides visibility through the scope of the view.
        dependency asksScope from PathResolver to CatalogScope;
    }
}
