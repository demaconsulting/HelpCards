package HelpCards_HelpCardMarkdown {
    private import SoftwareItemMetadata::*;
    private import HelpCards_BodyParser::*;

    part def HelpCardMarkdown {
        doc /* Reads the headings, the sections, and the bullets of a card body for parity tests, ignoring fenced blocks, and classifies every line of a body as text or as part of a fence with the rule of the loader, which the validator reuses. Supporting types: MarkdownLine and MarkdownLineKind (internal). The class is static and stateless. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Testing/HelpCardMarkdown.cs",
                "src/DemaConsulting.HelpCards/Testing/MarkdownLine.cs",
                "src/DemaConsulting.HelpCards/Testing/MarkdownLineKind.cs"
            );
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Testing/HelpCardMarkdownTests.cs");
            designFiles = ("docs/design/help-cards/testing/help-card-markdown.md");
            verificationFiles = ("docs/verification/help-cards/testing/help-card-markdown.md");
            requirementFiles = ("docs/reqstream/help-cards/testing/help-card-markdown.yaml");
        }

        // The helper uses the fence rule of the body parser so that both agree on what is code.
        dependency sharesFenceRule from HelpCardMarkdown to BodyParser;
    }
}
