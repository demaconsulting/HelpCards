package HelpCards_HelpFileProvider {
    private import SoftwareItemMetadata::*;

    part def HelpFileProvider {
        doc /* The seam through which the loader lists and reads the files of one help source: an abstract class with three implementations (assembly resources under a prefix, a directory, and in-memory text), each listing relative paths with "/" in ordinal order and reading strict UTF-8 text without a byte order mark. Creating a provider does no I/O. Supporting types: AssemblyFileProvider, DirectoryFileProvider, and MemoryFileProvider. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Loading/HelpFileProvider.cs",
                "src/DemaConsulting.HelpCards/Loading/AssemblyFileProvider.cs",
                "src/DemaConsulting.HelpCards/Loading/DirectoryFileProvider.cs",
                "src/DemaConsulting.HelpCards/Loading/MemoryFileProvider.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Loading/HelpFileProviderTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/TemporaryDirectory.cs"
            );
            designFiles = ("docs/design/help-cards/loading/help-file-provider.md");
            verificationFiles = ("docs/verification/help-cards/loading/help-file-provider.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/help-file-provider.yaml");
        }
    }
}
