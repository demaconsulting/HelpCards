namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The exception thrown when a help source or a composition of sources is defective.
/// </summary>
/// <remarks>
///     <para>
///         Catalog creation collects the defects it can find and throws this one exception afterwards, so an
///         author sees many problems at once, but not necessarily all of them: the checks run in stages, and the body
///         checks that need a finished card (<c>link-target</c>, <c>example-target</c>, <c>see-also-target</c>) are
///         skipped for a file whose front matter has an error, while a <c>syntax</c> error hides everything else in its
///         file. After fixing the listed errors, more can appear; see <see cref="HelpCatalog.Create"/>.
///         <see cref="Problems"/> holds every error that was found in the deterministic order
///         of the failure model; the message is the display text of the first problem followed by the number of
///         further ones. Bad arguments never produce this exception; they throw
///         <see cref="ArgumentException"/> or <see cref="ArgumentNullException"/>.
///     </para>
///     <para>
///         The exception derives from <see cref="InvalidOperationException"/> so callers that catch the broader
///         type also see it. It is immutable and therefore safe to share between threads.
///     </para>
///     <para>
///         <see cref="Exception.Message"/> shows only the first problem and the number of the others, for example
///         <c>Help card 'a.md' of source 'demo', line 1: The required key 'summary' is missing. (and 1 more)</c>. In a
///         test or a log, print every entry of <see cref="Problems"/>; each has the severity, rule id, file, line and
///         message (the table of rule ids is in the remarks of <see cref="HelpProblem"/>).
///     </para>
/// </remarks>
/// <example>
///     Catching the exception and printing every problem:
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
///
/// HelpSource broken = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["_source.md"] = "---\nname: demo\ntitle: Demo\nsummary: Demo cards.\n---\nBody.",
///     ["a.md"] = "---\ntitle: A\n---\nBody.",
///     ["b/c.md"] = "---\ntitle: C\nsummary: Needs a parent.\n---\nSee [[nowhere]].",
/// }, name: "demo");
///
/// try
/// {
///     HelpCatalog.Create(builder => builder.AddSource(broken));
/// }
/// catch (HelpCatalogException exception)
/// {
///     Console.WriteLine(exception.Message);
///     foreach (HelpProblem problem in exception.Problems)
///     {
///         Console.WriteLine($"{problem.RuleId}: {problem.Message}");
///     }
/// }
/// </code>
///     <code language="text">
/// Help card 'a.md' of source 'demo', line 1: The required key 'summary' is missing. (and 1 more)
/// key-missing: The required key 'summary' is missing.
/// parent-missing: The card 'b/c' needs a parent card 'b' ('b.md'), which does not exist.
/// </code>
/// </example>
public sealed class HelpCatalogException : InvalidOperationException
{
    /// <summary>
    ///     Creates the exception for a list of errors.
    /// </summary>
    /// <param name="problems">The errors, in display order. Must not be null or empty.</param>
    internal HelpCatalogException(IReadOnlyList<HelpProblem> problems)
        : base(CreateMessage(problems))
    {
        Problems = CardNode.Freeze(problems);
    }

    /// <summary>
    ///     Gets every error that was found, in the order source, file, line, and rule.
    /// </summary>
    public IReadOnlyList<HelpProblem> Problems { get; }

    /// <summary>
    ///     Writes the message: the first problem and how many more there are.
    /// </summary>
    /// <param name="problems">The errors.</param>
    /// <returns>The message.</returns>
    private static string CreateMessage(IReadOnlyList<HelpProblem> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);
        if (problems.Count == 0)
        {
            throw new ArgumentException("At least one problem is required.", nameof(problems));
        }

        var first = problems[0].ToDisplayText();
        return problems.Count == 1 ? first : $"{first} (and {problems.Count - 1} more)";
    }
}
