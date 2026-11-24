package HelpCards_Mounter {
    private import SoftwareItemMetadata::*;
    private import HelpCards_SourceRegistry::*;
    private import HelpCards_SiblingOrder::*;
    private import HelpCards_ProblemCollector::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_HelpProblem::*;

    part def Mounter {
        doc /* Decides where the cards of each source live: a flat source keeps its local ids, a mounted source gets a generated root card (the mount) and global ids mount/localId. It claims global ids in registration order and reports a mount that is not a segment (mount-invalid) and an id, mount, or root name or alias that two sources use (collision, naming both sources and files), leaving the later holder out. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Composition/Mounter.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/MounterTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/composition/mounter.md");
            verificationFiles = ("docs/verification/help-cards/composition/mounter.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/mounter.yaml");
        }

        // The mounter works on registered sources, names roots, collects problems, and creates root cards.
        dependency usesRegistry from Mounter to SourceRegistry;
        dependency namesRoots from Mounter to SiblingOrder;
        dependency collectsProblems from Mounter to ProblemCollector;
        dependency createsRootNodes from Mounter to CatalogData;
        dependency reportsProblems from Mounter to HelpProblem;
    }
}