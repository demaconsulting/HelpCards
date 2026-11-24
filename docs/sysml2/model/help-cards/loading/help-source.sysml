package HelpCards_HelpSource {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpFileProvider::*;
    private import HelpCards_TokenResolver::*;

    part def HelpSource {
        doc /* The public immutable value that describes where the help cards of one library or tool come from: created from the embedded resources of an assembly, a directory, or in-memory files; carrying an optional declared name, the values of the tokens its cards use, and the sources it imports. Creating a source and every With method perform no I/O; files are listed and read only when a catalog is built. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Loading/HelpSource.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Loading/HelpSourceTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CountingFileProvider.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/TemporaryDirectory.cs"
            );
            designFiles = ("docs/design/help-cards/loading/help-source.md");
            verificationFiles = ("docs/verification/help-cards/loading/help-source.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/help-source.yaml");
        }

        // A source delegates listing and reading to its provider and validates token names with the resolver's grammar.
        dependency usesProvider from HelpSource to HelpFileProvider;
        dependency usesTokenNames from HelpSource to TokenResolver;
    }
}
