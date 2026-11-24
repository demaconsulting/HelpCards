using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     A rule of the adopting product that the validator applies to every card, next to its own quality rules.
/// </summary>
/// <remarks>
///     <para>
///         Domain rules belong to the product, not to the library: a product may require, for example, that
///         the cards of a library never contain command-line text. A rule inspects one card at a time through
///         the public card model and returns the problems it found, each with the rule identifier, file, and
///         line the rule chooses.
///     </para>
///     <para>
///         The validator calls <see cref="Check"/> once per visible card, in catalog order, on the calling
///         thread. An implementation should be free of side effects; an exception that it throws is a defect of
///         the rule and is not caught.
///     </para>
/// </remarks>
public interface IHelpCardRule
{
    /// <summary>
    ///     Checks one card and returns every problem found.
    /// </summary>
    /// <param name="card">The card to check.</param>
    /// <returns>
    ///     The problems of the card; empty when the card satisfies the rule. A <see langword="null"/> result is
    ///     tolerated and treated as empty, although the signature does not declare it, and is the only supported null.
    /// </returns>
    IEnumerable<HelpProblem> Check(HelpCard card);
}
