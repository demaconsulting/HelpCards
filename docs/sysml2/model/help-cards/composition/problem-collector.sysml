package HelpCards_ProblemCollector {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpProblem::*;

    part def ProblemCollector {
        doc /* Collects the problems of one build and hands them out in the one deterministic display order (source registration index, file, line, rule), keeping an identical problem once. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Composition/ProblemCollector.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Composition/ProblemCollectorTests.cs");
            designFiles = ("docs/design/help-cards/composition/problem-collector.md");
            verificationFiles = ("docs/verification/help-cards/composition/problem-collector.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/problem-collector.yaml");
        }

        // The collector stores and orders problems.
        dependency collectsProblems from ProblemCollector to HelpProblem;
    }
}
