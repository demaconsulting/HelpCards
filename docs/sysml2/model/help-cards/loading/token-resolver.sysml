package HelpCards_TokenResolver {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpProblem::*;

    part def TokenResolver {
        doc /* Replaces {{name}} tokens in the text of one source with the values in scope for it (its own tokens, the tokens of the sources it imports, and the built-in tool and version) and reports unknown tokens at their lines and redefined tokens when the scope is built. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.HelpCards/Loading/TokenResolver.cs");
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Loading/TokenResolverTests.cs");
            designFiles = ("docs/design/help-cards/loading/token-resolver.md");
            verificationFiles = ("docs/verification/help-cards/loading/token-resolver.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/token-resolver.yaml");
        }

        // Unknown and redefined tokens are reported as problems.
        dependency reportsProblems from TokenResolver to HelpProblem;
    }
}
