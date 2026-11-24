package HelpCards_SiblingOrder {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CardReader::*;
    private import HelpCards_HelpProblem::*;

    part def SiblingOrder {
        doc /* The one comparator for the order of the cards that share a parent and of the roots of a source: first the cards named in the children list in the order listed, then by ascending order value (cards without one last), then by name in ordinal order. It reports repeated and unknown children entries. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Composition/SiblingOrder.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/SiblingOrderTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/composition/sibling-order.md");
            verificationFiles = ("docs/verification/help-cards/composition/sibling-order.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/sibling-order.yaml");
        }

        // The comparator orders loader definitions and reports list problems.
        dependency ordersDefinitions from SiblingOrder to CardReader;
        dependency reportsProblems from SiblingOrder to HelpProblem;
    }
}
