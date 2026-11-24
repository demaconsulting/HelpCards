package HelpCards_HelpCatalogBuilder {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpSource::*;
    private import HelpCards_CardReader::*;
    private import HelpCards_TokenResolver::*;
    private import HelpCards_ReferenceValidator::*;
    private import HelpCards_SiblingOrder::*;
    private import HelpCards_ProblemCollector::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_HelpProblem::*;

    part def HelpCatalogBuilder {
        doc /* Receives the configuration of a catalog (the single source, the culture, the tool name and version, the tag vocabulary, and the expected roots) and runs the one composition pipeline: it reads the source through Loading, places every card below its parent, orders siblings, checks tags, resolves references, checks uniqueness and expected roots, and freezes the data when no error exists. Defects never throw; they are collected as problems. The builder has no public constructor. Supporting types: CompositionResult. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Composition/HelpCatalogBuilder.cs",
                "src/DemaConsulting.HelpCards/Composition/HelpCatalogBuilder.Pipeline.cs",
                "src/DemaConsulting.HelpCards/Composition/HelpCatalogBuilder.Tree.cs",
                "src/DemaConsulting.HelpCards/Composition/CompositionResult.cs"
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

        // The builder reads the source through Loading and delegates references, order, and problem collection.
        dependency readsSource from HelpCatalogBuilder to HelpSource;
        dependency readsCards from HelpCatalogBuilder to CardReader;
        dependency buildsTokenScope from HelpCatalogBuilder to TokenResolver;
        dependency resolvesReferences from HelpCatalogBuilder to ReferenceValidator;
        dependency ordersSiblings from HelpCatalogBuilder to SiblingOrder;
        dependency collectsProblems from HelpCatalogBuilder to ProblemCollector;
        dependency freezesData from HelpCatalogBuilder to CatalogData;
        dependency reportsProblems from HelpCatalogBuilder to HelpProblem;
    }
}
