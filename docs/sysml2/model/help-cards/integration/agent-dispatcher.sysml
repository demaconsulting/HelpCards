package HelpCards_AgentDispatcher {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalog::*;
    private import HelpCards_SearchIndex::*;

    part def AgentDispatcher {
        doc /* Answers one call of the knowledge tool from checked arguments: an example, else a topic, else a query, else the index, with a line that names an ignored query. A topic is a card in Markdown with the content of its examples; an example is a description line and a fenced block; a query lists the best cards; every miss says what to send instead, with suggestions or the valid names. The class is partial: the choice of the answer and the index, and the lookups. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Integration/AgentDispatcher.cs",
                "src/DemaConsulting.HelpCards/Integration/AgentDispatcher.Lookup.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Integration/AgentDispatcherTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/integration/agent-dispatcher.md");
            verificationFiles = ("docs/verification/help-cards/integration/agent-dispatcher.md");
            requirementFiles = ("docs/reqstream/help-cards/integration/agent-dispatcher.yaml");
        }

        // The dispatcher answers from the catalog and suggests example names with the same rules as card names.
        dependency answersFrom from AgentDispatcher to HelpCatalog;
        dependency suggestsThrough from AgentDispatcher to SearchIndex;
    }
}