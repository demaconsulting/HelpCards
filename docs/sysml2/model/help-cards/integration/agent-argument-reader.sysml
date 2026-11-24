package HelpCards_AgentArgumentReader {
    private import SoftwareItemMetadata::*;

    part def AgentArgumentReader {
        doc /* Reads the JSON arguments of a call of the knowledge tool and checks them against its schema: nothing, an empty object, or JSON null asks for the index; text that is not JSON, JSON that is not an object, an unknown property, and a value of the wrong type are each answered with a plain-text message that says what to send instead; a null, empty, or white-space property is absent. Supporting types: AgentArguments and AgentReadResult. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/AgentArgumentReader.cs",
                "src/DemaConsulting.HelpCards/Integration/AgentArguments.cs",
                "src/DemaConsulting.HelpCards/Integration/AgentReadResult.cs"
            );
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Integration/AgentArgumentReaderTests.cs");
            designFiles = ("docs/design/help-cards/integration/agent-argument-reader.md");
            verificationFiles = ("docs/verification/help-cards/integration/agent-argument-reader.md");
            requirementFiles = ("docs/reqstream/help-cards/integration/agent-argument-reader.yaml");
        }
    }
}