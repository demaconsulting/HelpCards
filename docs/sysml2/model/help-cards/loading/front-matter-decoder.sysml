package HelpCards_FrontMatterDecoder {
    private import SoftwareItemMetadata::*;
    private import HelpCards_HelpProblem::*;

    part def FrontMatterDecoder {
        doc /* Decodes the restricted subset of YAML front matter that cards use (plain and quoted scalars, full-line comments, lists of scalars, lists of maps of scalars, and a literal block scalar only for the content and the expected output of an example) and splits off the Markdown body; every construct outside the subset is rejected with a problem that names the file and the line. Supporting types: FrontMatterDocument, FrontMatterEntry, FrontMatterItem, and FrontMatterScalar. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.HelpCards/Loading/FrontMatterDecoder.cs",
                "src/DemaConsulting.HelpCards/Loading/FrontMatterDecoder.Scalars.cs",
                "src/DemaConsulting.HelpCards/Loading/FrontMatterDocument.cs",
                "src/DemaConsulting.HelpCards/Loading/FrontMatterEntry.cs",
                "src/DemaConsulting.HelpCards/Loading/FrontMatterItem.cs",
                "src/DemaConsulting.HelpCards/Loading/FrontMatterScalar.cs"
            );
            testFiles = ("test/DemaConsulting.HelpCards.Tests/Loading/FrontMatterDecoderTests.cs");
            designFiles = ("docs/design/help-cards/loading/front-matter-decoder.md");
            verificationFiles = ("docs/verification/help-cards/loading/front-matter-decoder.md");
            requirementFiles = ("docs/reqstream/help-cards/loading/front-matter-decoder.yaml");
        }

        // A rejected file is reported as a problem with a rule, a file, and a line.
        dependency reportsProblems from FrontMatterDecoder to HelpProblem;
    }
}
