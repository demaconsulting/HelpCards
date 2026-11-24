package HelpCards_HelpRenderOptions {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalog::*;
    private import HelpCards_HelpCard::*;

    part def HelpRenderOptions {
        doc /* The public options of a rendering (format, link style, command template, link callback, wrapping width, tags, example content, and JSON envelope callback) with their defaults and checks, the public enumerations HelpFormat and HelpLinkStyle, and the internal RenderContext that resolves the options against a catalog view for one call: tool name, wrapping width, depth-first listing, cards with examples, and the card of the rendering view. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Presentation/HelpRenderOptions.cs",
                "src/DemaConsulting.HelpCards/Presentation/HelpFormat.cs",
                "src/DemaConsulting.HelpCards/Presentation/HelpLinkStyle.cs",
                "src/DemaConsulting.HelpCards/Presentation/RenderContext.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Presentation/HelpRenderOptionsTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/presentation/help-render-options.md");
            verificationFiles = ("docs/verification/help-cards/presentation/help-render-options.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation/help-render-options.yaml");
        }

        // The context reads the catalog view it renders and the cards of that view.
        dependency readsCatalog from HelpRenderOptions to HelpCatalog;
        dependency readsCards from HelpRenderOptions to HelpCard;
    }
}
