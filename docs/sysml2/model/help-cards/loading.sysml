package HelpCards_LoadingSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpSource::*;
    public import HelpCards_HelpFileProvider::*;
    public import HelpCards_FrontMatterDecoder::*;
    public import HelpCards_CardReader::*;
    public import HelpCards_TokenResolver::*;
    public import HelpCards_CultureSelector::*;
    public import HelpCards_BodyParser::*;

    part def LoadingSubsystem {
        doc /* The subsystem that turns the files of one help source into definitions: HelpSource describes where the cards come from (assembly resources, a directory, or memory) without any I/O; HelpFileProvider lists and reads the files; FrontMatterDecoder decodes the restricted subset of YAML front matter; CardReader classifies the files and reads cards, overlays, translations, examples, and the source description; TokenResolver replaces tokens in the scope of a source; CultureSelector chooses the translation of each file; BodyParser splits bodies into text and links. Every defect is collected as a problem. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Loading/LoadingTests.cs");
            designFiles = ("docs/design/help-cards/loading.md");
            verificationFiles = ("docs/verification/help-cards/loading.md");
            requirementFiles = ("docs/reqstream/help-cards/loading.yaml");
        }

        part helpSource : HelpSource;
        part helpFileProvider : HelpFileProvider;
        part frontMatterDecoder : FrontMatterDecoder;
        part cardReader : CardReader;
        part tokenResolver : TokenResolver;
        part cultureSelector : CultureSelector;
        part bodyParser : BodyParser;
    }
}
