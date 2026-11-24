package HelpCards_HelpCatalogBuilder {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpSource::*;
    private import HelpCards_SourceRegistry::*;
    private import HelpCards_Mounter::*;
    private import HelpCards_OverlayMerger::*;
    private import HelpCards_ReferenceValidator::*;
    private import HelpCards_SiblingOrder::*;
    private import HelpCards_ProblemCollector::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_HelpProblem::*;

    part def HelpCatalogBuilder {
        doc /* Receives the configuration of a catalog (the sources, flat or mounted, the culture, the tool name and version, the tag vocabulary, and the expected roots) and runs the one composition pipeline: it reads the sources, claims global ids, places every card below its parent, orders siblings, checks tags, applies overlays, resolves references, checks uniqueness and expected roots, and freezes the data when no error exists. Defects never throw; they are collected as problems. The builder has no public constructor. Supporting types: CompositionResult, SourceTree, and CardBuild. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Composition/HelpCatalogBuilder.cs",
                "src/DemaConsulting.HelpCards/Composition/HelpCatalogBuilder.Pipeline.cs",
                "src/DemaConsulting.HelpCards/Composition/HelpCatalogBuilder.Tree.cs",
                "src/DemaConsulting.HelpCards/Composition/CompositionResult.cs",
                "src/DemaConsulting.HelpCards/Composition/SourceTree.cs",
                "src/DemaConsulting.HelpCards/Composition/CardBuild.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/HelpCatalogBuilderTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/composition/help-catalog-builder.md");
            verificationFiles = ("docs/verification/help-cards/composition/help-catalog-builder.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/help-catalog-builder.yaml");
        }

        // The builder registers sources through the registry and delegates mounting, overlays, references, order, and problem collection.
        dependency registersSources from HelpCatalogBuilder to HelpSource;
        dependency readsSources from HelpCatalogBuilder to SourceRegistry;
        dependency mountsSources from HelpCatalogBuilder to Mounter;
        dependency appliesOverlays from HelpCatalogBuilder to OverlayMerger;
        dependency resolvesReferences from HelpCatalogBuilder to ReferenceValidator;
        dependency ordersSiblings from HelpCatalogBuilder to SiblingOrder;
        dependency collectsProblems from HelpCatalogBuilder to ProblemCollector;
        dependency freezesData from HelpCatalogBuilder to CatalogData;
        dependency reportsProblems from HelpCatalogBuilder to HelpProblem;
    }
}
