package HelpCards_HelpAgentTool {
    private import SoftwareItemMetadata::*;
    private import HelpCards_AgentArgumentReader::*;
    private import HelpCards_AgentDispatcher::*;

    part def HelpAgentTool {
        doc /* The knowledge tool that gives an AI agent one way to read the help of a whole catalog, independent of any agent framework: it has a name, a generated or given description, and the exact one-line JSON schema of its arguments; Invoke reads the arguments, answers, cuts the answer at the output limit, and turns the recoverable exceptions into a message that names only the exception type. Supporting types: HelpAgentToolOptions and the static factory HelpAgentTools. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/HelpAgentTool.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpAgentToolOptions.cs",
                "src/DemaConsulting.HelpCards/Integration/HelpAgentTools.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Integration/HelpAgentToolTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CountingFileProvider.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/integration/help-agent-tool.md");
            verificationFiles = ("docs/verification/help-cards/integration/help-agent-tool.md");
            requirementFiles = ("docs/reqstream/help-cards/integration/help-agent-tool.yaml");
        }

        // The tool reads its arguments with the reader and answers through the dispatcher.
        dependency readsArgumentsWith from HelpAgentTool to AgentArgumentReader;
        dependency answersThrough from HelpAgentTool to AgentDispatcher;
    }
}