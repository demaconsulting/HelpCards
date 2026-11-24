using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The example sections of a card and the example documents of the text and Markdown renderer.
/// </summary>
internal static partial class TextRenderer
{
    /// <summary>
    ///     The shortest code fence.
    /// </summary>
    private const int MinimumFenceLength = 3;

    /// <summary>
    ///     Renders the examples document of a card: its own examples in full and a list of the cards below it that
    ///     have examples.
    /// </summary>
    /// <remarks>
    ///     The contents of the examples of descendants are not printed; only their names are listed, with the command
    ///     that prints them when the link style writes commands.
    /// </remarks>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card, as the catalog view of the call shows it. Must not be null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string RenderExamples(RenderContext context, HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(card);

        var builder = new StringBuilder();
        builder.Append(HelpStrings.ExamplesTitle(card.Title)).Append('\n');

        // The card's own examples come first, each with its description and its content in a fence
        if (card.Examples.Count == 0)
        {
            builder.Append('\n');
            TextWrapper.Append(builder, HelpStrings.NoOwnExamples, context.WrapWidth, string.Empty, string.Empty);
        }

        foreach (var example in card.Examples)
        {
            builder.Append('\n').Append("## ").Append(ExampleHeader(example)).Append("\n\n");
            AppendExampleBody(context, builder, example);
        }

        // Commands tell the reader how to print one example alone
        if (card.Examples.Count > 0 && context.UsesCommands)
        {
            builder.Append('\n');
            var hint = HelpStrings.PrintOneExampleHint(LinkFormatter.Command(context, card.Id));
            TextWrapper.Append(builder, hint, context.WrapWidth, string.Empty, EntryIndent);
        }

        AppendDescendantsWithExamples(context, builder, card);
        return Finish(builder);
    }

    /// <summary>
    ///     Gets the text of one example, for the text and Markdown formats.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card to search from, as the catalog view of the call shows it. Must not be null.</param>
    /// <param name="name">The example name. Must not be null or white space.</param>
    /// <returns>Exactly the content of the example, with no header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when there is no such example.</exception>
    public static string RenderExample(RenderContext context, HelpCard card, string name)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.RequireExample(card, name).Example.Content;
    }

    /// <summary>
    ///     Appends the examples section of a card document when the card has examples.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="card">The card.</param>
    private static void AppendExamplesSection(RenderContext context, StringBuilder builder, HelpCard card)
    {
        if (card.Examples.Count == 0)
        {
            return;
        }

        AppendHeading(builder, HelpStrings.ExamplesHeading);
        if (context.Options.IncludeExampleContent)
        {
            AppendExampleSections(context, builder, card);
        }
        else
        {
            foreach (var example in card.Examples)
            {
                var text = example.Description.Length == 0
                    ? ExampleHeader(example)
                    : $"{ExampleHeader(example)}: {example.Description}";
                TextWrapper.Append(builder, text, context.WrapWidth, BulletPrefix, EntryIndent);
            }
        }

        // Commands tell the reader how to print the examples in full or one alone
        if (context.UsesCommands)
        {
            builder.Append('\n');
            var hint = HelpStrings.PrintExamplesHint(LinkFormatter.Command(context, card.Id));
            TextWrapper.Append(builder, hint, context.WrapWidth, string.Empty, EntryIndent);
        }
    }

    /// <summary>
    ///     Appends each example of a card as a third-level section with its description and fenced content.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="card">The card.</param>
    private static void AppendExampleSections(RenderContext context, StringBuilder builder, HelpCard card)
    {
        var first = true;
        foreach (var example in card.Examples)
        {
            // Sections are separated by one blank line; the heading already follows a blank line
            if (!first)
            {
                builder.Append('\n');
            }

            first = false;
            builder.Append("### ").Append(ExampleHeader(example)).Append("\n\n");
            AppendExampleBody(context, builder, example);
        }
    }

    /// <summary>
    ///     Appends the description of an example, a blank line, and the content in a fence.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="example">The example.</param>
    private static void AppendExampleBody(RenderContext context, StringBuilder builder, HelpExample example)
    {
        if (example.Description.Length > 0)
        {
            TextWrapper.Append(builder, example.Description, context.WrapWidth, string.Empty, string.Empty);
            builder.Append('\n');
        }

        // The fence is one backtick longer than the longest run in the content so the content cannot end it
        var fence = CreateFence(example.Content);
        builder.Append(fence).Append(example.Language).Append('\n');
        builder.Append(example.Content.TrimEnd('\n')).Append('\n');
        builder.Append(fence).Append('\n');
    }

    /// <summary>
    ///     Appends the list of cards below a card that have examples, with the names of their examples.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="card">The card whose descendants are listed.</param>
    private static void AppendDescendantsWithExamples(RenderContext context, StringBuilder builder, HelpCard card)
    {
        var below = context.DescendantsWithExamples(card);
        if (below.Count == 0)
        {
            return;
        }

        AppendHeading(builder, HelpStrings.ExamplesBelowHeading);
        foreach (var target in below)
        {
            var label = LinkFormatter.FormatLabel(context, target.Id, target.Title, target.SourceName);

            // A command label ends with the option that lists the examples of the card
            var suffix = context.UsesCommands && context.Options.LinkFormatter is null ? " --examples" : string.Empty;
            builder.Append(BulletPrefix).Append(label).Append(suffix).Append('\n');
            var names = string.Join(", ", target.Examples.Select(example => example.Name));
            TextWrapper.Append(
                builder,
                $"{HelpStrings.ExampleNamesLabel}: {names}",
                context.WrapWidth,
                EntryIndent,
                EntryIndent);
        }
    }

    /// <summary>
    ///     Writes the name, kind, and file name of an example, such as <c>run [command]</c> or
    ///     <c>settings [source] settings.yaml</c>.
    /// </summary>
    /// <param name="example">The example.</param>
    /// <returns>The header text.</returns>
    private static string ExampleHeader(HelpExample example)
    {
        var header = $"{example.Name} [{example.Kind}]";
        return example.FileName is null ? header : $"{header} {example.FileName}";
    }

    /// <summary>
    ///     Creates the code fence for some content: at least three backticks and one more than the longest run of
    ///     backticks in the content.
    /// </summary>
    /// <param name="content">The content to enclose.</param>
    /// <returns>The fence.</returns>
    internal static string CreateFence(string content)
    {
        // Measure the longest run of backticks in the content
        var longest = 0;
        var run = 0;
        foreach (var character in content)
        {
            run = character == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        return new string('`', Math.Max(MinimumFenceLength, longest + 1));
    }
}
