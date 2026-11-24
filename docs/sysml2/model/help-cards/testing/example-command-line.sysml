package HelpCards_ExampleCommandLine {
    private import SoftwareItemMetadata::*;

    part def ExampleCommandLine {
        doc /* Reads the one-line content of a command example into the arguments after the tool name with a restricted grammar: words separated by spaces, single-quoted text, double-quoted text with two escapes, and an error that names the character and the column for every shell operator, expansion character, unclosed quote, or line break. It never starts a process. The class is static, stateless, and has no dependency inside the library. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Testing/ExampleCommandLine.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Testing/ExampleCommandLineTests.cs");
            designFiles = ("docs/design/help-cards/testing/example-command-line.md");
            verificationFiles = ("docs/verification/help-cards/testing/example-command-line.md");
            requirementFiles = ("docs/reqstream/help-cards/testing/example-command-line.yaml");
        }
    }
}
