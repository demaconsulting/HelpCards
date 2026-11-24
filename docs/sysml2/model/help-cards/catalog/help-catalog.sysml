package HelpCards_HelpCatalog {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalogBuilder::*;
    private import HelpCards_HelpCard::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_CatalogScope::*;
    private import HelpCards_PathResolver::*;
    private import HelpCards_ExampleResolver::*;
    private import HelpCards_SearchIndex::*;
    private import HelpCards_HelpProblem::*;

    part def HelpCatalog {
        doc /* The public face of the library: creates an immutable, thread-safe catalog from a configuration callback (throwing one exception with every error when a source is defective), exposes the cards, roots, sources, tags, and diagnostics, looks up cards by path and examples by name, makes filtered views by tag, source, predicate, and id that share the frozen data, searches the visible cards and suggests close names, and renders cards, indexes, examples, search results, and card lists through the Presentation subsystem. The class is partial: creation and lookup, views, search and suggestions, and the Render members. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/HelpCatalog.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpCatalog.Views.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpCatalog.Search.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpSearchOptions.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpCatalog.Render.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCatalogTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCatalogTests.Views.cs",
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCatalogTests.Search.cs",
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCatalogTests.Render.cs",
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
        dependency searchesThrough from HelpCatalog to SearchIndex;
        dependency throwsProblems from HelpCatalog to HelpProblem;

        // The Render members read the options and call the renderers of the Presentation subsystem; the units are
        // named by qualified name because the Presentation units import this package.
        dependency rendersThrough from HelpCatalog to HelpCards_TextRenderer::TextRenderer;
        dependency rendersJsonThrough from HelpCatalog to HelpCards_JsonDocumentWriter::JsonDocumentWriter;
        dependency formatsLinksThrough from HelpCatalog to HelpCards_LinkFormatter::LinkFormatter;
        dependency readsOptions from HelpCatalog to HelpCards_HelpRenderOptions::HelpRenderOptions;
    }
}
