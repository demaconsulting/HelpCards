package HelpCards_SourceRegistry {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpSource::*;
    private import HelpCards_CardReader::*;
    private import HelpCards_TokenResolver::*;
    private import HelpCards_HelpProblem::*;

    part def SourceRegistry {
        doc /* Registers the sources of a catalog, reads each one once in registration order through Loading, decides its name (a duplicate name excludes the later source), resolves its imports (matched by files or by declared name; missing, later, and self imports are reported), builds its token scope from its own tokens, the tokens of the sources it imports, and the built-ins, and looks up the card a reference names as seen from a source: own cards before imported cards, ambiguous when both have the id, absent when a qualified source is not registered, and suppressed when the defect is already reported. Supporting types: SourceEntry, SourceRegistration, LookupKind, and LookupResult. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Composition/SourceRegistry.cs",
                "src/DemaConsulting.HelpCards/Composition/SourceEntry.cs",
                "src/DemaConsulting.HelpCards/Composition/SourceRegistration.cs",
                "src/DemaConsulting.HelpCards/Composition/LookupKind.cs",
                "src/DemaConsulting.HelpCards/Composition/LookupResult.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/SourceRegistryTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CountingFileProvider.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/composition/source-registry.md");
            verificationFiles = ("docs/verification/help-cards/composition/source-registry.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/source-registry.yaml");
        }

        // The registry reads sources through Loading, builds token scopes, and reports problems.
        dependency readsSources from SourceRegistry to HelpSource;
        dependency readsCards from SourceRegistry to CardReader;
        dependency buildsTokenScope from SourceRegistry to TokenResolver;
        dependency reportsProblems from SourceRegistry to HelpProblem;
    }
}