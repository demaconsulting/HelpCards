package HelpCards_HelpCardValidator {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpCatalogBuilder::*;
    private import HelpCards_HelpCatalog::*;
    private import HelpCards_HelpCard::*;
    private import HelpCards_HelpProblem::*;
    private import HelpCards_HelpSource::*;
    private import HelpCards_TokenResolver::*;
    private import HelpCards_ExampleCommandLine::*;
    private import HelpCards_HelpCardMarkdown::*;

    part def HelpCardValidator {
        doc /* Validates a help source, the configuration of a catalog, or a catalog and returns every problem as a list: the errors that creating the catalog would raise (composed through the builder without an exception), then the quality warnings (summary-length, card-size, heading-reserved, fence-language, fence-unclosed, unused-token, line-length, command-parse), the problems of the rules of the product, and, on request, an error for each absent external source; AssertValid throws one catalog exception; CompareToDirectory reports files that are embedded or on disk only. Supporting types: HelpValidationOptions (the limits and switches) and IHelpCardRule (the rule of a product). */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Testing/HelpCardValidator.cs",
                "src/DemaConsulting.HelpCards/Testing/HelpCardValidator.Rules.cs",
                "src/DemaConsulting.HelpCards/Testing/HelpCardValidator.Resources.cs",
                "src/DemaConsulting.HelpCards/Testing/HelpValidationOptions.cs",
                "src/DemaConsulting.HelpCards/Testing/IHelpCardRule.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Testing/HelpCardValidatorTests.cs",
                "test/DemaConsulting.HelpCards.Tests/Testing/HelpCardValidatorTests.Rules.cs",
                "test/DemaConsulting.HelpCards.Tests/Testing/HelpCardValidatorTests.Options.cs",
                "test/DemaConsulting.HelpCards.Tests/Testing/HelpCardValidatorTests.Resources.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/TemporaryDirectory.cs"
            );
            designFiles = ("docs/design/help-cards/testing/help-card-validator.md");
            verificationFiles = ("docs/verification/help-cards/testing/help-card-validator.md");
            requirementFiles = ("docs/reqstream/help-cards/testing/help-card-validator.yaml");
        }

        // The validator composes through the builder, wraps the data in a catalog, and reads the cards, problems, and sources.
        dependency composesWithBuilder from HelpCardValidator to HelpCatalogBuilder;
        dependency inspectsCatalog from HelpCardValidator to HelpCatalog;
        dependency inspectsCards from HelpCardValidator to HelpCard;
        dependency reportsProblems from HelpCardValidator to HelpProblem;
        dependency readsSourceFiles from HelpCardValidator to HelpSource;
        dependency findsUsedTokens from HelpCardValidator to TokenResolver;
        dependency parsesCommands from HelpCardValidator to ExampleCommandLine;
        dependency classifiesBodyLines from HelpCardValidator to HelpCardMarkdown;
    }
}
