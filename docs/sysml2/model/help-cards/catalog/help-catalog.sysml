package HelpCards_HelpCatalog {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalogBuilder::*;
    private import HelpCards_HelpCard::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_CatalogScope::*;
    private import HelpCards_PathResolver::*;
    private import HelpCards_ExampleResolver::*;
    private import HelpCards_HelpProblem::*;

    part def HelpCatalog {
        doc /* The public face of the library: creates an immutable, thread-safe catalog from a configuration callback (throwing one exception with every error when a source is defective), exposes the cards, roots, sources, tags, and diagnostics, looks up cards by path and examples by name, and makes filtered views by tag, source, predicate, and id that share the frozen data. The class is partial; the Presentation subsystem adds the Render members. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/HelpCatalog.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpCatalog.Views.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCatalogTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCatalogTests.Views.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CountingFileProvider.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/help-catalog.md");
            verificationFiles = ("docs/verification/help-cards/catalog/help-catalog.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/help-catalog.yaml");
        }

        // The catalog creates its data through the builder and answers every question through scopes and resolvers.
        dependency createsThrough from HelpCatalog to HelpCatalogBuilder;
        dependency wrapsCards from HelpCatalog to HelpCard;
        dependency readsData from HelpCatalog to CatalogData;
        dependency viewsThrough from HelpCatalog to CatalogScope;
        dependency resolvesPaths from HelpCatalog to PathResolver;
        dependency findsExamples from HelpCatalog to ExampleResolver;
        dependency throwsProblems from HelpCatalog to HelpProblem;
    }
}
