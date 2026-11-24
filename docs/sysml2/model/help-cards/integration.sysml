package HelpCards_IntegrationSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpCli::*;
    public import HelpCards_CliArgumentParser::*;
    public import HelpCards_CliDispatcher::*;
    public import HelpCards_HelpAgentTool::*;
    public import HelpCards_AgentArgumentReader::*;
    public import HelpCards_AgentDispatcher::*;

    part def IntegrationSubsystem {
        doc /* The subsystem that connects the catalog to its readers, provider-neutral for both: HelpCli is the command-line helper for people, which gates on the help verb and creates the catalog only when help is asked for; CliArgumentParser reads its arguments; CliDispatcher answers a help command with the exit codes of the contract; HelpAgentTool is the knowledge tool for AI agents; AgentArgumentReader reads and checks its JSON arguments; AgentDispatcher answers a call with the index, a card, an example, or search results. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Integration/IntegrationTests.cs");
            designFiles = ("docs/design/help-cards/integration.md");
            verificationFiles = ("docs/verification/help-cards/integration.md");
            requirementFiles = ("docs/reqstream/help-cards/integration.yaml");
        }

        part helpCli : HelpCli;
        part cliArgumentParser : CliArgumentParser;
        part cliDispatcher : CliDispatcher;
        part helpAgentTool : HelpAgentTool;
        part agentArgumentReader : AgentArgumentReader;
        part agentDispatcher : AgentDispatcher;
    }
}