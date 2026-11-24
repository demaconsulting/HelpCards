using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Checks help sources and catalogs for defects and for quality, so that an adopter can fail its own tests
///     instead of shipping a broken card.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Validate(HelpSource, HelpValidationOptions?)"/> and its overloads return every problem as a
///         list and never throw for a defect of a card: the errors that creating a catalog would raise come
///         first, followed by the quality warnings (<c>summary-length</c>, <c>card-size</c>,
///         <c>heading-reserved</c>, <c>fence-language</c>, <c>fence-unclosed</c>, <c>unused-token</c>,
///         <c>line-length</c>, and <c>command-parse</c>), the problems of the rules in
///         <see cref="HelpValidationOptions.Rules"/>, and, when requested, an error for each absent external
///         source. Within a severity the order is source, file, line, and rule. When a source has errors there is
///         no catalog to inspect, so only the errors are returned.
///     </para>
///     <para>
///         <c>AssertValid</c> turns the result into a <see cref="HelpCatalogException"/> for use in a test. The
///         class uses no test framework. The validator reads files of the sources it is given and keeps no
///         state, so it is safe for concurrent use.
///     </para>
///     <para>
///         <b>Rules.</b> The errors are the rules of <c>HelpCatalog.Create</c> (<c>key-missing</c>, <c>link-target</c>,
///         <c>parent-missing</c> and the others), plus <c>external-source-missing</c> with
///         <see cref="HelpValidationOptions.RequireExternalSources"/>; the warnings are the quality rules above and
///         <c>resources-mismatch</c> from <see cref="CompareToDirectory"/>. Every rule id, with its severity and a one-line
///         meaning, is listed once in the table in the remarks of <see cref="HelpProblem"/>. A problem has line 0 when
///         no single line applies (for example <c>summary-length</c>, <c>card-size</c> and <c>parent-missing</c>); see
///         <see cref="HelpProblem"/>.
///     </para>
///     <para>
///         <b>The failure message.</b> The message of the exception that <c>AssertValid</c> throws is the text of the
///         first failing problem followed by <c>(and N more)</c>; print <see cref="HelpCatalogException.Problems"/> in
///         your test to show every defect, as the example does.
///     </para>
/// </remarks>
/// <example>
///     One source with two errors and one with a warning. <c>AssertValid</c> throws for the errors, and
///     <c>Validate</c> returns the findings without throwing, warnings included.
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
/// using DemaConsulting.HelpCards.Testing;
///
/// HelpSource broken = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["_source.md"] = "---\nname: demo\ntitle: Demo\nsummary: Demo cards.\n---\nBody.",
///     ["a.md"] = "---\ntitle: A\n---\nBody.",                                         // key-missing: no summary
///     ["b/c.md"] = "---\ntitle: C\nsummary: Needs a parent.\n---\nBody.",            // parent-missing: no b.md
/// }, name: "demo");
///
/// try
/// {
///     HelpCardValidator.AssertValid(broken);
/// }
/// catch (HelpCatalogException exception)
/// {
///     Console.WriteLine(exception.Message);
///     foreach (HelpProblem problem in exception.Problems)
///     {
///         Console.WriteLine($"{problem.Severity} {problem.RuleId} {problem.File} line {problem.Line}");
///     }
/// }
///
/// HelpSource wordy = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["_source.md"] = "---\nname: demo\ntitle: Demo\nsummary: Demo cards.\n---\nBody.",
///     ["a.md"] = "---\ntitle: A\nsummary: " + new string('x', 80) + "\n---\nBody.",   // longer than 60 for a root card
/// }, name: "demo");
///
/// foreach (HelpProblem warning in HelpCardValidator.Validate(wordy))
/// {
///     Console.WriteLine($"{warning.Severity} {warning.RuleId} {warning.File} line {warning.Line}");
/// }
/// </code>
///     <code language="text">
/// Help card 'a.md' of source 'demo', line 1: The required key 'summary' is missing. (and 1 more)
/// Error key-missing a.md line 1
/// Error parent-missing b/c.md line 0
/// Warning summary-length a.md line 0
/// </code>
/// </example>
public static partial class HelpCardValidator
{
    /// <summary>
    ///     Validates a help source together with the sources it imports.
    /// </summary>
    /// <remarks>
    ///     The sources are composed as a flat catalog: every source that the given source imports, directly or
    ///     transitively, followed by the source itself, in dependency order. A source that is reached more than
    ///     once is added once. Because the imports are added by the validator, a source with imports never reports
    ///     <c>import-missing</c> here; use <see cref="Validate(Action{HelpCatalogBuilder}, HelpValidationOptions?)"/>
    ///     to check a composition of the product, such as one with mounted sources.
    /// </remarks>
    /// <param name="source">The source. Must not be null.</param>
    /// <param name="options">The limits and switches, or <see langword="null"/> for the defaults.</param>
    /// <returns>Every problem found, errors first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static IReadOnlyList<HelpProblem> Validate(HelpSource source, HelpValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var ordered = new List<HelpSource>();
        CollectInDependencyOrder(source, new HashSet<HelpFileProvider>(), ordered);
        return Validate(
            builder =>
            {
                foreach (var item in ordered)
                {
                    builder.AddSource(item);
                }
            },
            options);
    }

    /// <summary>
    ///     Validates the catalog that a configuration callback describes.
    /// </summary>
    /// <remarks>
    ///     The callback is the one an adopter passes to <see cref="HelpCatalog.Create"/>, so the validator checks
    ///     exactly the composition the product creates, including its tool name, tag vocabulary, and expected roots.
    /// </remarks>
    /// <param name="configure">Configures the builder. Must not be null.</param>
    /// <param name="options">The limits and switches, or <see langword="null"/> for the defaults.</param>
    /// <returns>Every problem found, errors first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the builder is configured with an invalid argument.</exception>
    public static IReadOnlyList<HelpProblem> Validate(Action<HelpCatalogBuilder> configure, HelpValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var effective = options ?? new HelpValidationOptions();
        var builder = new HelpCatalogBuilder();
        configure(builder);
        var result = builder.Compose();
        if (result.Data is null)
        {
            // Without a catalog there is nothing to inspect; the errors are already in display order
            return result.Errors;
        }

        var catalog = HelpCatalog.FromData(result.Data);
        var problems = result.Errors.Concat(CheckCatalog(catalog, effective)).ToList();
        problems.AddRange(CheckTokens(builder.Sources, catalog.Sources));
        return Order(problems, catalog.Sources);
    }

    /// <summary>
    ///     Validates a catalog that was already created.
    /// </summary>
    /// <remarks>
    ///     A catalog does not keep the tokens of its sources, so <c>unused-token</c> is only reported by the
    ///     overloads that receive a source or a configuration callback. For a view, only the visible cards are
    ///     checked.
    /// </remarks>
    /// <param name="catalog">The catalog or view. Must not be null.</param>
    /// <param name="options">The limits and switches, or <see langword="null"/> for the defaults.</param>
    /// <returns>Every problem found, errors first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    public static IReadOnlyList<HelpProblem> Validate(HelpCatalog catalog, HelpValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Order(CheckCatalog(catalog, options ?? new HelpValidationOptions()), catalog.Sources);
    }

    /// <summary>
    ///     Adds a source after the sources it imports, depth first, each provider once.
    /// </summary>
    /// <param name="source">The source to add.</param>
    /// <param name="seen">The providers that were already visited; this also ends an import cycle.</param>
    /// <param name="ordered">Receives the sources, imports before importers.</param>
    private static void CollectInDependencyOrder(HelpSource source, HashSet<HelpFileProvider> seen, List<HelpSource> ordered)
    {
        if (!seen.Add(source.Provider))
        {
            return;
        }

        foreach (var imported in source.ImportedSources)
        {
            CollectInDependencyOrder(imported, seen, ordered);
        }

        ordered.Add(source);
    }

    /// <summary>
    ///     Throws when a help source has errors, or warnings when the options ask for that.
    /// </summary>
    /// <param name="source">The source. Must not be null.</param>
    /// <param name="options">The limits and switches, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="HelpCatalogException">Thrown when a problem fails the validation.</exception>
    public static void AssertValid(HelpSource source, HelpValidationOptions? options = null)
    {
        Assert(Validate(source, options), options);
    }

    /// <summary>
    ///     Throws when the catalog that a configuration callback describes has errors, or warnings when the options ask for that.
    /// </summary>
    /// <param name="configure">Configures the builder. Must not be null.</param>
    /// <param name="options">The limits and switches, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <exception cref="HelpCatalogException">Thrown when a problem fails the validation.</exception>
    public static void AssertValid(Action<HelpCatalogBuilder> configure, HelpValidationOptions? options = null)
    {
        Assert(Validate(configure, options), options);
    }

    /// <summary>
    ///     Throws when a catalog has errors, or warnings when the options ask for that.
    /// </summary>
    /// <param name="catalog">The catalog or view. Must not be null.</param>
    /// <param name="options">The limits and switches, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    /// <exception cref="HelpCatalogException">Thrown when a problem fails the validation.</exception>
    public static void AssertValid(HelpCatalog catalog, HelpValidationOptions? options = null)
    {
        Assert(Validate(catalog, options), options);
    }

    /// <summary>
    ///     Throws one exception for the problems that fail a validation.
    /// </summary>
    /// <param name="problems">The result of a validation.</param>
    /// <param name="options">The options of the validation, or <see langword="null"/>.</param>
    /// <exception cref="HelpCatalogException">Thrown when an error, or a warning with the option, is present.</exception>
    private static void Assert(IReadOnlyList<HelpProblem> problems, HelpValidationOptions? options)
    {
        var warningsFail = options?.WarningsAreErrors ?? false;
        var failing = problems
            .Where(problem => problem.Severity == HelpSeverity.Error || (warningsFail && problem.Severity == HelpSeverity.Warning))
            .ToList();
        if (failing.Count > 0)
        {
            throw new HelpCatalogException(failing);
        }
    }

    /// <summary>
    ///     Sorts problems by severity (errors first), then by source, file, line, and rule.
    /// </summary>
    /// <remarks>
    ///     The sort is stable, so problems that are equal in every key keep the order in which they were found.
    ///     A problem of a source that the catalog does not list sorts after the listed sources.
    /// </remarks>
    /// <param name="problems">The problems.</param>
    /// <param name="sources">The sources of the catalog in registration order.</param>
    /// <returns>The ordered problems.</returns>
    private static IReadOnlyList<HelpProblem> Order(IEnumerable<HelpProblem> problems, IReadOnlyList<HelpSourceInfo> sources)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            index.TryAdd(source.Name, index.Count);
        }

        return problems
            .OrderByDescending(problem => problem.Severity)
            .ThenBy(problem => index.GetValueOrDefault(problem.SourceName, int.MaxValue))
            .ThenBy(problem => problem.File, StringComparer.Ordinal)
            .ThenBy(problem => problem.Line)
            .ThenBy(problem => problem.RuleId, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();
    }
}
