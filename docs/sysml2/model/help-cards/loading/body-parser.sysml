package HelpCards_BodyParser {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpProblem::*;

    part def BodyParser {
        doc /* Splits the Markdown body of a card into literal text parts and link parts ([[id]], [[id#example]], [[#example]], and the external forms [[source:id|text]]), recognizing links only outside fenced blocks and inline code spans and reporting text that starts like a link but is not one. Supporting types: BodyPart and ParsedLink. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Loading/BodyParser.cs",
                "src/DemaConsulting.HelpCards/Loading/BodyPart.cs",
                "src/DemaConsulting.HelpCards/Loading/ParsedLink.cs"
            );
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Loading/BodyParserTests.cs");
            designFiles = ("docs/design/help-cards/loading/body-parser.md");
            verificationFiles = ("docs/verification/help-cards/loading/body-parser.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/body-parser.yaml");
        }

        // Link typos are reported as problems.
        dependency reportsProblems from BodyParser to HelpProblem;
    }
}
