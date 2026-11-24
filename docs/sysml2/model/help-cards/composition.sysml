package HelpCards_CompositionSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpCatalogBuilder::*;
    public import HelpCards_ReferenceValidator::*;
    public import HelpCards_SiblingOrder::*;
    public import HelpCards_ProblemCollector::*;

    part def CompositionSubsystem {
        doc /* The subsystem that turns the definitions read by Loading into one frozen tree: HelpCatalogBuilder is configured by the adopter and runs the one composition pipeline (read, place, order, check tags, resolve references, check uniqueness, freeze); ReferenceValidator resolves every link and see-also entry over the final tree and checks names; SiblingOrder is the one comparator for the order of siblings; ProblemCollector collects every problem in one deterministic order. This release composes one source and does not apply overlays. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Composition/CompositionTests.cs");
            designFiles = ("docs/design/help-cards/composition.md");
            verificationFiles = ("docs/verification/help-cards/composition.md");
            requirementFiles = ("docs/reqstream/help-cards/composition.yaml");
        }

        part helpCatalogBuilder : HelpCatalogBuilder;
        part referenceValidator : ReferenceValidator;
        part siblingOrder : SiblingOrder;
        part problemCollector : ProblemCollector;
    }
}
