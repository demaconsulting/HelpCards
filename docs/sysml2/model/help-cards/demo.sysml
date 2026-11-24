package HelpCards_Demo {
    private import SoftwareItemMetadata::*;

    part def Demo {
        doc /* Demonstration greeting class showing how a DEMA Consulting .NET library class should be structured, documented, and tested. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Demo.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/DemoTests.cs");
            designFiles = ("docs/design/help-cards/demo.md");
            verificationFiles = ("docs/verification/help-cards/demo.md");
            requirementFiles = ("docs/reqstream/help-cards/demo.yaml");
        }
    }
}
