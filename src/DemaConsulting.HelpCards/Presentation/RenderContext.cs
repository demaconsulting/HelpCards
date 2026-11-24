using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The settings of one rendering call together with the catalog view it renders: the resolved options, the tool
///     name, and the walks over the visible tree that several renderers share.
/// </summary>
/// <remarks>
///     A context is created once per call and discarded with it, so nothing a rendering needs is cached between calls.
///     It never changes after creation and reads only the immutable catalog.
/// </remarks>
internal sealed class RenderContext
{
    /// <summary>
    ///     The tool name used when neither the builder nor a source supplies one.
    /// </summary>
    private const string FallbackToolName = "tool";

    /// <summary>
    ///     Creates the context of a call.
    /// </summary>
    /// <param name="catalog">The catalog view to render. Must not be null.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    private RenderContext(HelpCatalog catalog, HelpRenderOptions? options)
    {
        Catalog = catalog;
        Options = options ?? new HelpRenderOptions();
        var data = catalog.Scope.Data;

        // The tool name is the one given to the builder; otherwise the first source stands in for the tool
        ToolName = data.ToolName ?? data.Sources.FirstOrDefault()?.Name ?? FallbackToolName;
        ToolVersion = data.ToolVersion;
    }

    /// <summary>
    ///     Gets the catalog view being rendered.
    /// </summary>
    public HelpCatalog Catalog { get; }

    /// <summary>
    ///     Gets the options of the call; never null.
    /// </summary>
    public HelpRenderOptions Options { get; }

    /// <summary>
    ///     Gets the name of the tool that appears in commands.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    ///     Gets the version given to the builder, or <see langword="null"/> when none was set.
    /// </summary>
    public string? ToolVersion { get; }

    /// <summary>
    ///     Gets a value indicating whether the format is Markdown.
    /// </summary>
    public bool IsMarkdown => Options.Format == HelpFormat.Markdown;

    /// <summary>
    ///     Gets the width at which generated lines are wrapped; 0 when nothing is wrapped (Markdown and no wrapping).
    /// </summary>
    public int WrapWidth => IsMarkdown ? 0 : Options.Width;

    /// <summary>
    ///     Gets a value indicating whether commands are written: the link style is <see cref="HelpLinkStyle.Command"/>.
    /// </summary>
    public bool UsesCommands => Options.LinkStyle == HelpLinkStyle.Command;

    /// <summary>
    ///     Creates the context of a call.
    /// </summary>
    /// <param name="catalog">The catalog view to render.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The context.</returns>
    public static RenderContext Create(HelpCatalog catalog, HelpRenderOptions? options)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return new RenderContext(catalog, options);
    }

    /// <summary>
    ///     Finds the card of this catalog view that has the id of a card, so a card of another view is rendered with
    ///     the rules of this one.
    /// </summary>
    /// <param name="card">The card. Must not be null.</param>
    /// <returns>The card of this view.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the card is not visible in this catalog view.</exception>
    public HelpCard Own(HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return Catalog.GetCardById(card.Id)
            ?? throw new ArgumentException($"The help card '{card.Id}' is not visible in this catalog.", nameof(card));
    }

    /// <summary>
    ///     Finds an example of a card or of the cards below it by name.
    /// </summary>
    /// <param name="card">The card to search from. Must not be null.</param>
    /// <param name="name">The example name. Must not be null or white space.</param>
    /// <returns>The example with the id of the card that owns it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name"/> is null or white space, the card is not visible, or no such example
    ///     exists.
    /// </exception>
    public HelpExampleReference RequireExample(HelpCard card, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var own = Own(card);
        return Catalog.FindExample(own, name)
            ?? throw new ArgumentException($"The help card '{own.Id}' has no example '{name}'.", nameof(name));
    }

    /// <summary>
    ///     Lists the visible cards depth first with their depth, down to a depth.
    /// </summary>
    /// <param name="maxDepth">The deepest level to include; 0 lists only the roots.</param>
    /// <returns>The cards in index order.</returns>
    public IReadOnlyList<(HelpCard Card, int Depth)> Flatten(int maxDepth)
    {
        var result = new List<(HelpCard Card, int Depth)>();
        foreach (var root in Catalog.Roots)
        {
            Walk(root, 0, maxDepth, result);
        }

        return result;
    }

    /// <summary>
    ///     Lists the visible cards below a card, depth first, that have examples of their own.
    /// </summary>
    /// <param name="card">The card whose descendants are listed.</param>
    /// <returns>The descendants with examples, in depth-first order.</returns>
    public IReadOnlyList<HelpCard> DescendantsWithExamples(HelpCard card)
    {
        var result = new List<HelpCard>();
        foreach (var child in Catalog.GetChildren(card))
        {
            CollectWithExamples(child, result);
        }

        return result;
    }

    /// <summary>
    ///     Adds a card and everything below it to a list, down to a depth.
    /// </summary>
    /// <param name="card">The card to add.</param>
    /// <param name="depth">The depth of the card.</param>
    /// <param name="maxDepth">The deepest level to include.</param>
    /// <param name="result">The list to add to.</param>
    private void Walk(HelpCard card, int depth, int maxDepth, List<(HelpCard Card, int Depth)> result)
    {
        result.Add((card, depth));
        if (depth >= maxDepth)
        {
            return;
        }

        foreach (var child in Catalog.GetChildren(card))
        {
            Walk(child, depth + 1, maxDepth, result);
        }
    }

    /// <summary>
    ///     Adds a card to a list when it has examples, and then does the same for the cards below it.
    /// </summary>
    /// <param name="card">The card to look at.</param>
    /// <param name="result">The list to add to.</param>
    private void CollectWithExamples(HelpCard card, List<HelpCard> result)
    {
        if (card.Examples.Count > 0)
        {
            result.Add(card);
        }

        foreach (var child in Catalog.GetChildren(card))
        {
            CollectWithExamples(child, result);
        }
    }
}
