using System.Globalization;
using System.Text.RegularExpressions;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Chooses which culture variant of a file supplies the text for a requested culture.
/// </summary>
/// <remarks>
///     <para>
///         The requested culture is always explicit: the library never reads <c>CurrentUICulture</c>. The
///         invariant culture asks for the neutral files only. For any other culture the fallback chain trims
///         the last subtag each time (<c>de-DE</c>, then <c>de</c>) and ends with the neutral file. The
///         choice is made per file, so one file may come from a translation while another falls back to the
///         neutral text.
///     </para>
///     <para>
///         Names are compared with <see cref="StringComparison.OrdinalIgnoreCase"/> and are not validated
///         against the operating system. The class is stateless and safe for concurrent use.
///     </para>
/// </remarks>
internal static partial class CultureSelector
{
    /// <summary>
    ///     Gets the culture names to try for a requested culture, most specific first, without the neutral file.
    /// </summary>
    /// <remarks>
    ///     The invariant culture (an empty name) yields an empty chain, which selects the neutral file.
    ///     <c>zh-Hans-CN</c> yields <c>zh-Hans-CN</c>, <c>zh-Hans</c>, <c>zh</c>.
    /// </remarks>
    /// <param name="culture">The requested culture. Must not be null.</param>
    /// <returns>The names to try in order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="culture"/> is null.</exception>
    public static IReadOnlyList<string> GetFallbackChain(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var chain = new List<string>();
        var name = culture.Name;
        while (name.Length > 0)
        {
            chain.Add(name);
            var cut = name.LastIndexOf('-');
            name = cut < 0 ? string.Empty : name[..cut];
        }

        return chain;
    }

    /// <summary>
    ///     Selects the first culture of the chain that has a translation file.
    /// </summary>
    /// <param name="chain">The culture names to try, from <see cref="GetFallbackChain"/>.</param>
    /// <param name="available">The culture suffixes of the translation files that exist for one file stem.</param>
    /// <returns>
    ///     The available suffix as it is written in the file name, or <see langword="null"/> when the neutral
    ///     file must be used.
    /// </returns>
    public static string? Select(IReadOnlyList<string> chain, IEnumerable<string> available)
    {
        var suffixes = available.ToList();
        foreach (var name in chain)
        {
            var match = suffixes.FirstOrDefault(suffix => string.Equals(suffix, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>
    ///     Determines whether a text is a valid culture suffix of a file name.
    /// </summary>
    /// <param name="text">The text between the stem and <c>.md</c>.</param>
    /// <returns><see langword="true"/> for two or three lower-case letters followed by dash-separated subtags.</returns>
    public static bool IsCultureSuffix(string text)
    {
        return CulturePattern().IsMatch(text);
    }

    /// <summary>
    ///     Matches the culture suffix grammar.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CulturePattern();
}
