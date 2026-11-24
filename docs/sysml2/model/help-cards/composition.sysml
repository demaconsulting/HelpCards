package HelpCards_CompositionSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpCatalogBuilder::*;
    public import HelpCards_SourceRegistry::*;
    public import HelpCards_Mounter::*;
    public import HelpCards_OverlayMerger::*;
    public import HelpCards_ReferenceValidator::*;
    public import HelpCards_SiblingOrder::*;
    public import HelpCards_ProblemCollector::*;

    part def CompositionSubsystem {
        doc /* The subsystem that turns the definitions read by Loading into one frozen tree: HelpCatalogBuilder is configured by the adopter and runs the one composition pipeline (read, place, order, check tags, apply overlays, resolve references, check uniqueness, freeze); SourceRegistry registers and reads the sources, resolves imports, and looks up references; Mounter gives cards their global ids, creates mount roots, and finds clashes; OverlayMerger applies overlays; ReferenceValidator resolves every link and see-also entry over the final tree and checks names; SiblingOrder is the one comparator for the order of siblings; ProblemCollector collects every problem in one deterministic order. Several sources, flat or mounted, compose into one catalog. */

        metadata info : SoftwareInfo {
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/CompositionTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Composition/CompositionMultiSourceTests.cs"
            );
            designFiles = ("docs/design/help-cards/composition.md");
            verificationFiles = ("docs/verification/help-cards/composition.md");
            requirementFiles = ("docs/reqstream/help-cards/composition.yaml");
        }

        part helpCatalogBuilder : HelpCatalogBuilder;
        part sourceRegistry : SourceRegistry;
        part mounter : Mounter;
        part overlayMerger : OverlayMerger;
        part referenceValidator : ReferenceValidator;
        part siblingOrder : SiblingOrder;
        part problemCollector : ProblemCollector;
    }
}
