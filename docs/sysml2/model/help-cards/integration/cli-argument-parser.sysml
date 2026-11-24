package HelpCards_CliArgumentParser {
    private import SoftwareItemMetadata::*;

    part def CliArgumentParser {
        doc /* Reads the arguments that follow the help verb, without a command-line library, into a request or the first usage error: words are one card path, options may stand anywhere, a value is the next argument only, every option but the tag and the source may be given once, and conflicting or incomplete combinations are reported in a fixed order. Supporting types: CliRequest, CliMode, and CliParseResult. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/CliArgumentParser.cs",
                "src/DemaConsulting.HelpCards/Integration/CliMode.cs",
                "src/DemaConsulting.HelpCards/Integration/CliRequest.cs",
                "src/DemaConsulting.HelpCards/Integration/CliParseResult.cs"
            );
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Integration/CliArgumentParserTests.cs");
            designFiles = ("docs/design/help-cards/integration/cli-argument-parser.md");
            verificationFiles = ("docs/verification/help-cards/integration/cli-argument-parser.md");
            requirementFiles = ("docs/reqstream/help-cards/integration/cli-argument-parser.yaml");
        }
    }
}