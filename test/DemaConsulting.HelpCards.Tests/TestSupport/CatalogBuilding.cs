using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     Helpers that create catalogs from the embedded fixtures and from in-memory files, shared by the tests.
/// </summary>
/// <remarks>
///     The embedded fixtures are compiled into the test assembly by the packaged MSBuild targets, so loading them
///     exercises the same path an adopter's embedded cards take. Error sources live in memory and are never checked
///     in as files, so deliberately defective cards never reach the repository lint gates.
/// </remarks>
internal static class CatalogBuilding
{
    /// <summary>
    ///     The resource prefix under which the fixtures of this test assembly are embedded.
    /// </summary>
    private const string FixturePrefix = "DemaConsulting.HelpCards.Tests.Help";

    /// <summary>
    ///     Creates a source over an embedded fixture.
    /// </summary>
    /// <param name="fixture">The name of the fixture folder, such as <c>simple</c>.</param>
    /// <param name="tokens">The tokens of the source, or <see langword="null"/> for none.</param>
    /// <returns>The source.</returns>
    public static HelpSource Fixture(string fixture, Dictionary<string, string>? tokens = null)
    {
        var source = HelpSource.FromAssembly(typeof(CatalogBuilding).Assembly, $"{FixturePrefix}/{fixture}/Help");
        return tokens is null ? source : source.WithTokens(tokens);
    }

    /// <summary>
    ///     Creates the source of the first fixture library, with its <c>limit</c> token.
    /// </summary>
    /// <returns>The source <c>lib-a</c>.</returns>
    public static HelpSource LibA()
    {
        return Fixture("lib-a", new Dictionary<string, string> { ["limit"] = "10" });
    }

    /// <summary>
    ///     Creates the source of the second fixture library, with its <c>palette</c> token.
    /// </summary>
    /// <returns>The source <c>lib-b</c>.</returns>
    public static HelpSource LibB()
    {
        return Fixture("lib-b", new Dictionary<string, string> { ["palette"] = "blue" });
    }

    /// <summary>
    ///     Creates the source of the fixture application, which imports a library.
    /// </summary>
    /// <param name="imported">The library source the application imports.</param>
    /// <returns>The source <c>app</c>.</returns>
    public static HelpSource App(HelpSource imported)
    {
        return Fixture("app").Imports(imported);
    }

    /// <summary>
    ///     Creates the source of the fixture tool, which imports its companion library.
    /// </summary>
    /// <param name="imported">The library source the tool imports.</param>
    /// <returns>The source <c>tool</c>.</returns>
    public static HelpSource Tool(HelpSource imported)
    {
        return Fixture("tool", new Dictionary<string, string> { ["schemaVersion"] = "2" }).Imports(imported);
    }

    /// <summary>
    ///     Creates a source from in-memory files with a declared name.
    /// </summary>
    /// <param name="name">The declared name.</param>
    /// <param name="files">The path and text of each file.</param>
    /// <returns>The source.</returns>
    public static HelpSource Named(string name, params (string Path, string Text)[] files)
    {
        return HelpSource.FromFiles(SourceReading.Files(files), name);
    }

    /// <summary>
    ///     Creates the catalog of the <c>simple</c> fixture with its <c>limit</c> token and tool version 2.5.
    /// </summary>
    /// <returns>The catalog.</returns>
    public static HelpCatalog Simple()
    {
        return Create(
            Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" }),
            builder => builder.ToolVersion("2.5"));
    }

    /// <summary>
    ///     Creates the catalog of the <c>runnable</c> fixture, whose command examples state their expected results, with
    ///     the tool name <c>demo</c>.
    /// </summary>
    /// <returns>The catalog.</returns>
    public static HelpCatalog Runnable()
    {
        return Create(Fixture("runnable"), builder => builder.ToolName("demo"));
    }

    /// <summary>
    ///     Creates a catalog from a source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="configure">Further configuration of the builder, or <see langword="null"/>.</param>
    /// <returns>The catalog.</returns>
    public static HelpCatalog Create(HelpSource source, Action<HelpCatalogBuilder>? configure = null)
    {
        return HelpCatalog.Create(builder =>
        {
            builder.AddSource(source);
            configure?.Invoke(builder);
        });
    }

    /// <summary>
    ///     Creates a catalog from in-memory files of a source named <c>test</c>.
    /// </summary>
    /// <param name="files">The path and text of each file.</param>
    /// <returns>The catalog.</returns>
    public static HelpCatalog FromFiles(params (string Path, string Text)[] files)
    {
        return Create(HelpSource.FromFiles(SourceReading.Files(files), "test"));
    }

    /// <summary>
    ///     Creates a catalog from a source named <c>test</c> that is made of valid cards, one per given id.
    /// </summary>
    /// <param name="ids">The local ids of the cards, such as <c>a</c> and <c>a/b</c>.</param>
    /// <returns>The catalog.</returns>
    public static HelpCatalog FromIds(params string[] ids)
    {
        return FromFiles(ids.Select(id => ($"{id}.md", SourceReading.Card(title: id, summary: $"The card {id}."))).ToArray());
    }

    /// <summary>
    ///     Composes in-memory files and returns every problem, whether or not composition succeeds.
    /// </summary>
    /// <param name="files">The path and text of each file.</param>
    /// <returns>The result of composition.</returns>
    public static CompositionResult Compose(params (string Path, string Text)[] files)
    {
        return Compose(HelpSource.FromFiles(SourceReading.Files(files), "test"));
    }

    /// <summary>
    ///     Composes a source and returns every problem, whether or not composition succeeds.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="configure">Further configuration of the builder, or <see langword="null"/>.</param>
    /// <returns>The result of composition.</returns>
    public static CompositionResult Compose(HelpSource source, Action<HelpCatalogBuilder>? configure = null)
    {
        var builder = new HelpCatalogBuilder();
        builder.AddSource(source);
        configure?.Invoke(builder);
        return builder.Compose();
    }

    /// <summary>
    ///     Composes the sources a callback adds and returns every problem, whether or not composition succeeds.
    /// </summary>
    /// <param name="configure">Configures the builder.</param>
    /// <returns>The result of composition.</returns>
    public static CompositionResult Compose(Action<HelpCatalogBuilder> configure)
    {
        var builder = new HelpCatalogBuilder();
        configure(builder);
        return builder.Compose();
    }

    /// <summary>
    ///     Gets the problems that make creation fail for in-memory files.
    /// </summary>
    /// <param name="files">The path and text of each file.</param>
    /// <returns>The errors in display order.</returns>
    public static IReadOnlyList<HelpProblem> Errors(params (string Path, string Text)[] files)
    {
        return Compose(files).Errors;
    }

    /// <summary>
    ///     Gets the culture the tests use for the translated fixture.
    /// </summary>
    /// <param name="name">The culture name.</param>
    /// <returns>The culture.</returns>
    public static CultureInfo Culture(string name)
    {
        return CultureInfo.GetCultureInfo(name);
    }
}
