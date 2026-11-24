package HelpCards_ReferenceValidator {
    private import SoftwareItemMetadata::*;
    private import HelpCards_CardReader::*;
    private import HelpCards_SourceRegistry::*;
    private import HelpCards_SiblingOrder::*;
    private import HelpCards_ExampleResolver::*;
    private import HelpCards_CatalogData::*;
    private import HelpCards_HelpProblem::*;

    part def ReferenceValidator {
        doc /* Checks the final tree of a catalog: resolves card links, example links, external links, and see-also entries to global ids in the scope of the source that wrote them (its own cards and those of the sources it imports), finds the card that owns each linked example, lets a link to a source that is not present degrade to its display text, and checks that example names within a scope and names and aliases among siblings are unique. References to files that failed to load are not reported again. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Composition/ReferenceValidator.cs");
            testFiles = (
                "test/DemaConsulting.HelpCards.Tests/Composition/ReferenceValidatorTests.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/CatalogBuilding.cs",
                "test/DemaConsulting.HelpCards.Tests/TestSupport/SourceReading.cs"
            );
            designFiles = ("docs/design/help-cards/composition/reference-validator.md");
            verificationFiles = ("docs/verification/help-cards/composition/reference-validator.md");
            requirementFiles = ("docs/reqstream/help-cards/composition/reference-validator.yaml");
        }

        // The validator works on loader definitions and searches examples with the shared example walk.
        dependency readsDefinitions from ReferenceValidator to CardReader;
        dependency looksUpCards from ReferenceValidator to SourceRegistry;
        dependency namesCards from ReferenceValidator to SiblingOrder;
        dependency searchesExamples from ReferenceValidator to ExampleResolver;
        dependency resolvesIntoNodes from ReferenceValidator to CatalogData;
        dependency reportsProblems from ReferenceValidator to HelpProblem;
    }
}
