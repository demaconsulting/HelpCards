package HelpCards_CardReader {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpSource::*;
    private import HelpCards_FrontMatterDecoder::*;
    private import HelpCards_TokenResolver::*;
    private import HelpCards_CultureSelector::*;
    private import HelpCards_BodyParser::*;
    private import HelpCards_HelpProblem::*;

    part def CardReader {
        doc /* Reads one help source completely: classifies its files (cards, overlays, translations, _source.md, and data files), decodes them, checks the key matrix and the value grammars, replaces tokens, reads examples (with the expected exit code and output of a command), parses bodies, applies the translation the culture chain selects for each file, and returns definitions together with every problem found. Supporting types: SourceDefinition, CardDefinition, OverlayDefinition, TranslationDefinition, ExampleDefinition, ReferenceDefinition, and LocatedValue. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Loading/CardReader.cs",
                "src/DemaConsulting.HelpCards/Loading/CardReader.Files.cs",
                "src/DemaConsulting.HelpCards/Loading/CardReader.Values.cs",
                "src/DemaConsulting.HelpCards/Loading/CardReader.Examples.cs",
                "src/DemaConsulting.HelpCards/Loading/CardReader.Cards.cs",
                "src/DemaConsulting.HelpCards/Loading/CardReader.Source.cs",
                "src/DemaConsulting.HelpCards/Loading/SourceDefinition.cs",
                "src/DemaConsulting.HelpCards/Loading/CardDefinition.cs",
                "src/DemaConsulting.HelpCards/Loading/OverlayDefinition.cs",
                "src/DemaConsulting.HelpCards/Loading/TranslationDefinition.cs",
                "src/DemaConsulting.HelpCards/Loading/ExampleDefinition.cs",
                "src/DemaConsulting.HelpCards/Loading/ReferenceDefinition.cs",
                "src/DemaConsulting.HelpCards/Loading/LocatedValue.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Loading/CardReaderTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Loading/CardReaderTests.Examples.cs",
                "test/DemaConsulting.HelpCards.Tests/Loading/CardReaderTests.Cultures.cs",
                "test/DemaConsulting.HelpCards.Tests/Loading/CardReaderTests.Source.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CountingFileProvider.cs"
            );
            designFiles = ("docs/design/help-cards/loading/card-reader.md");
            verificationFiles = ("docs/verification/help-cards/loading/card-reader.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/card-reader.yaml");
        }

        // The reader lists and reads through the files of the source and delegates every kind of text analysis.
        dependency readsSource from CardReader to HelpSource;
        dependency decodesFrontMatter from CardReader to FrontMatterDecoder;
        dependency replacesTokens from CardReader to TokenResolver;
        dependency selectsCultures from CardReader to CultureSelector;
        dependency parsesBodies from CardReader to BodyParser;
        dependency reportsProblems from CardReader to HelpProblem;
    }
}
