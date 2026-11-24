package HelpCards_HelpStrings {
    private import SoftwareItemMetadata::*;

    part def HelpStrings {
        doc /* The English chrome of a rendering: the headings Cards, Examples, and See also, the heading of the cards below with examples, the labels Also known as and Tags in plain and bold form, the hint sentences for printing examples, and the sentences for an empty list. Chrome is never localized in this release and does not depend on the culture of the machine. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Presentation/HelpStrings.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Presentation/HelpStringsTests.cs");
            designFiles = ("docs/design/help-cards/presentation/help-strings.md");
            verificationFiles = ("docs/verification/help-cards/presentation/help-strings.md");
            requirementFiles = ("docs/reqstream/help-cards/presentation/help-strings.yaml");
        }
    }
}
