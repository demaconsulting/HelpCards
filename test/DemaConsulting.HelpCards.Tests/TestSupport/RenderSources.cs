using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     In-memory sources for the rendering tests that reproduce the shapes of the JSON contract fixtures.
/// </summary>
/// <remarks>
///     The contract documents of the specification come from several small catalogs of an imaginary tool named
///     <c>reqstream</c>: the example of the <c>card</c> document needs a different command line than the example of
///     the <c>examples</c> document, so no single catalog could produce all of them. Each helper here builds the
///     smallest catalog that produces one document exactly.
/// </remarks>
internal static class RenderSources
{
    /// <summary>
    ///     The summary of the <c>reqstream</c> source.
    /// </summary>
    private const string SourceSummary = "Help cards of reqstream.";

    /// <summary>
    ///     Creates the catalog behind the <c>index</c> and <c>cards</c> documents: one card, a tool name, and a version.
    /// </summary>
    /// <returns>The catalog.</returns>
    public static HelpCatalog Index()
    {
        return Create(
            [
                ("requirements.md", RequirementsCard(string.Empty, "Write one file per section.\n")),
            ]);
    }

    /// <summary>
    ///     Creates the catalog behind the <c>card</c> document: a card with a child, a link, and an example.
    /// </summary>
    /// <returns>The catalog.</returns>
    public static HelpCatalog Card()
    {
        return Full("reqstream --requirements requirements.yaml --enforce");
    }

    /// <summary>
    ///     Creates the catalog behind the <c>examples</c> and <c>example</c> documents.
    /// </summary>
    /// <returns>The catalog.</returns>
    public static HelpCatalog Examples()
    {
        return Full("reqstream --enforce");
    }

    /// <summary>
    ///     Creates a catalog with the cards <c>requirements</c>, <c>requirements/tags</c>, and <c>tests</c>.
    /// </summary>
    /// <param name="checkCommand">The command line of the <c>check</c> example of the <c>requirements</c> card.</param>
    /// <returns>The catalog.</returns>
    private static HelpCatalog Full(string checkCommand)
    {
        var example = $"examples:\n  - name: check\n    kind: command\n    language: shell\n    content: {checkCommand}\n"
            + "    description: Check traceability.\n";
        return Create(
            [
                ("requirements.md", RequirementsCard(example, "Write one file per section.\n\nSee [[tests]].\n")),
                ("requirements/tags.md", TagsCard()),
                ("requirements/tags.yaml", "root-tags: [system]\n"),
                ("tests.md", SourceReading.Card(title: "Tests", summary: "How to name tests.")),
            ]);
    }

    /// <summary>
    ///     Creates the catalog of a tool named <c>reqstream</c> with version 1.4.0 from files.
    /// </summary>
    /// <param name="files">The files of the source below the source file.</param>
    /// <returns>The catalog.</returns>
    private static HelpCatalog Create(IEnumerable<(string Path, string Text)> files)
    {
        var all = new List<(string Path, string Text)>
        {
            ("_source.md", $"---\nname: reqstream\ntitle: reqstream\nsummary: {SourceSummary}\n---\nThe help of reqstream.\n"),
        };
        all.AddRange(files);
        return CatalogBuilding.Create(
            HelpSource.FromFiles(SourceReading.Files(all.ToArray()), "reqstream"),
            builder => builder.ToolName("reqstream").ToolVersion("1.4.0"));
    }

    /// <summary>
    ///     Writes the <c>requirements</c> card.
    /// </summary>
    /// <param name="examples">The <c>examples</c> front matter, or empty for none.</param>
    /// <param name="body">The body.</param>
    /// <returns>The text of the file.</returns>
    private static string RequirementsCard(string examples, string body)
    {
        return SourceReading.Card(
            title: "Requirements files",
            summary: "How to write requirements.",
            extraFrontMatter: $"aliases:\n  - req\ntags:\n  - authoring\n{examples}",
            body: body.TrimEnd('\n'));
    }

    /// <summary>
    ///     Writes the <c>requirements/tags</c> card with its source example.
    /// </summary>
    /// <returns>The text of the file.</returns>
    private static string TagsCard()
    {
        return SourceReading.Card(
            title: "Tags",
            summary: "Root tags.",
            extraFrontMatter:
                "examples:\n  - name: root-tag\n    kind: source\n    language: yaml\n    file: tags.yaml\n"
                + "    description: A root tag.\n",
            body: "Root tags mark requirements as system level.");
    }
}
