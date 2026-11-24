package HelpCards_PresentationSubsystem {
    private import SoftwareItemMetadata::*;
    public import HelpCards_HelpRenderOptions::*;
    public import HelpCards_LinkFormatter::*;
    public import HelpCards_TextRenderer::*;
    public import HelpCards_JsonDocumentWriter::*;
    public import HelpCards_TextWrapper::*;
    public import HelpCards_HelpStrings::*;

    part def PresentationSubsystem {
        doc /* The subsystem that renders the catalog: HelpRenderOptions (with HelpFormat, HelpLinkStyle, and the per-call context) holds the settings of a rendering; LinkFormatter writes links, labels, and commands in the plain, command, and Markdown styles or through a callback; TextRenderer writes the text and Markdown documents; JsonDocumentWriter writes the documents of JSON contract version 1; TextWrapper wraps generated lines; HelpStrings holds the English chrome. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Presentation/PresentationTests.cs");
            designFiles = ("docs/design/help-cards/presentation.md");
            verificationFiles = ("docs/verification/help-cards/presentation.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation.yaml");
        }

        part helpRenderOptions : HelpRenderOptions;
        part linkFormatter : LinkFormatter;
        part textRenderer : TextRenderer;
        part jsonDocumentWriter : JsonDocumentWriter;
        part textWrapper : TextWrapper;
        part helpStrings : HelpStrings;
    }
}
