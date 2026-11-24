package HelpCards_OverlayMerger {
    private import SoftwareItemMetadata::*;
    private import HelpCards_SourceRegistry::*;
    private import HelpCards_ProblemCollector::*;
    private import HelpCards_HelpProblem::*;

    part def OverlayMerger {
        doc /* Applies the overlays of every source to the cards they extend, sources in registration order and overlays by ordinal file path: title and summary replace, the body is appended after a blank line, see-also entries and examples are appended. The target [source:]id is resolved in the overlay's source and its imports; an optional overlay of an absent source is skipped (informational), any other overlay without a target is an error. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Composition/OverlayMerger.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/OverlayMergerTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/composition/overlay-merger.md");
            verificationFiles = ("docs/verification/help-cards/composition/overlay-merger.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/overlay-merger.yaml");
        }

        // The merger finds targets through the registry and reports problems.
        dependency findsTargets from OverlayMerger to SourceRegistry;
        dependency collectsProblems from OverlayMerger to ProblemCollector;
        dependency reportsProblems from OverlayMerger to HelpProblem;
    }
}