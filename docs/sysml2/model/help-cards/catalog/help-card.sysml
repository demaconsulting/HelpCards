package HelpCards_HelpCard {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_CatalogScope::*;
    private import ScalarValues::*;

    part def HelpCard {
        doc /* One card as a catalog view shows it: global and local id, name, nearest visible parent, source, title, summary, tags, aliases, body culture, visible children and see-also cards, examples (a command with its expected exit code and output), and extension keys. The body is the Markdown with every link in the plain style, computed once; the body pieces give renderers text and links to visible targets after the hiding rules of the view. Supporting types: HelpExample, HelpExampleReference, HelpLink, HelpSourceInfo, HelpPathResolution, HelpSearchResult, and BodyPiece. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Catalog/HelpCard.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpCard.Body.cs",
                "src/DemaConsulting.HelpCards/Catalog/BodyPiece.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpExample.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpExampleReference.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpLink.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpSourceInfo.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpPathResolution.cs",
                "src/DemaConsulting.HelpCards/Catalog/HelpSearchResult.cs"
            );
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Catalog/HelpCardTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/catalog/help-card.md");
            verificationFiles = ("docs/verification/help-cards/catalog/help-card.md");
            requirementFiles = ("docs/reqstream/help-cards/catalog/help-card.yaml");
        }

        attribute id : String;
        attribute localId : String;
        attribute name : String;
        attribute parentId : String [0..1];
        attribute sourceName : String;
        attribute title : String;
        attribute summary : String;
        attribute tags : String [0..*];
        attribute aliases : String [0..*];
        attribute culture : String;
        attribute childIds : String [0..*];
        attribute seeAlsoIds : String [0..*];
        attribute body : String;

        // A card reads its frozen node and asks its view which targets are visible.
        dependency readsNode from HelpCard to CatalogData;
        dependency asksView from HelpCard to CatalogScope;
    }
}
