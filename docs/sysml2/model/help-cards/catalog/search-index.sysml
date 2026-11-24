package HelpCards_SearchIndex {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_HelpCard::*;

    part def SearchIndex {
        doc /* Ranks the visible cards of a catalog for a query and finds the names closest to a word that was not found: it splits text into lower-case tokens with an English plural rule, scores each query token by the best of six fields with exact and prefix weights, adds the bonuses for all words and for the whole query, orders results by score, depth, and id, and suggests candidates by case-insensitive Levenshtein distance. The class is partial: the index and the scoring, and the suggestions. It is built once per frozen data on first use and then only read. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/SearchIndex.cs",
                "src/DemaConsulting.HelpCards/Catalog/SearchIndex.Suggest.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/SearchIndexTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/GoldenFiles.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/RankingReport.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/search-index.md");
            verificationFiles = ("docs/verification/help-cards/catalog/search-index.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/search-index.yaml");
        }

        // The index is built from the frozen data and scores the cards of the data.
        dependency readsData from SearchIndex to CatalogData;
        dependency scoresCards from SearchIndex to HelpCard;
    }
}
