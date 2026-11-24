package HelpCards_TextWrapper {
    private import SoftwareItemMetadata::*;

    part def TextWrapper {
        doc /* Wraps generated text to a width with a prefix on the first line and a hanging indent on the others: it breaks only between words, counts the prefix toward the width, puts a word that is longer than the width on its own line, writes one line when the width is zero, and writes nothing for text without a word. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Presentation/TextWrapper.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Presentation/TextWrapperTests.cs");
            designFiles = ("docs/design/help-cards/presentation/text-wrapper.md");
            verificationFiles = ("docs/verification/help-cards/presentation/text-wrapper.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation/text-wrapper.yaml");
        }
    }
}
