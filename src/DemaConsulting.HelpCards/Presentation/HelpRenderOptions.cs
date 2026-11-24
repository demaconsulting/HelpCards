using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The optional settings of a rendering: format, link style, command template, width, and hooks.
/// </summary>
/// <remarks>
///     <para>
///         Every property has a default, so <c>new HelpRenderOptions()</c> renders wrapped plain text with plain
///         links. Values are checked when they are set, so an invalid option fails where it is written and not
///         somewhere inside a rendering. The object is immutable after initialization and safe to share.
///     </para>
///     <para>
///         The two callbacks run on the calling thread and may be called concurrently by concurrent callers of a
///         shared catalog; they must be thread-safe and free of side effects. The catalog never serializes them.
///     </para>
/// </remarks>
public sealed class HelpRenderOptions
{
    /// <summary>
    ///     The smallest wrapping width that is allowed.
    /// </summary>
    internal const int MinimumWidth = 20;

    /// <summary>
    ///     The largest wrapping width that is allowed.
    /// </summary>
    internal const int MaximumWidth = 500;

    /// <summary>
    ///     The default wrapping width.
    /// </summary>
    internal const int DefaultWidth = 78;

    /// <summary>
    ///     The command template that applies when none is set.
    /// </summary>
    internal const string DefaultCommandTemplate = "{tool} help {path}";

    /// <summary>
    ///     The format, checked when set.
    /// </summary>
    private readonly HelpFormat _format;

    /// <summary>
    ///     The link style, checked when set.
    /// </summary>
    private readonly HelpLinkStyle _linkStyle;

    /// <summary>
    ///     The command template, checked when set.
    /// </summary>
    private readonly string? _commandTemplate;

    /// <summary>
    ///     The wrapping width, checked when set.
    /// </summary>
    private readonly int _width = DefaultWidth;

    /// <summary>
    ///     Gets the output format; <see cref="HelpFormat.Text"/> by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on initialization with a value that is not defined.</exception>
    public HelpFormat Format
    {
        get => _format;
        init
        {
            // Reject numbers that were cast to the enumeration but name no format
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The help format is not defined.");
            }

            _format = value;
        }
    }

    /// <summary>
    ///     Gets how links are written; <see cref="HelpLinkStyle.Plain"/> by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on initialization with a value that is not defined.</exception>
    public HelpLinkStyle LinkStyle
    {
        get => _linkStyle;
        init
        {
            // Reject numbers that were cast to the enumeration but name no style
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The help link style is not defined.");
            }

            _linkStyle = value;
        }
    }

    /// <summary>
    ///     Gets the template of the command that prints a card, or <see langword="null"/> for <c>{tool} help {path}</c>.
    /// </summary>
    /// <remarks>
    ///     The placeholder <c>{tool}</c> stands for the tool name and <c>{path}</c> for the global id of the card with
    ///     each <c>/</c> replaced by a space. The template is used by <see cref="HelpLinkStyle.Command"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown on initialization with white space only.</exception>
    public string? CommandTemplate
    {
        get => _commandTemplate;
        init
        {
            // A template that is only white space can never name a command
            if (value is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
            }

            _commandTemplate = value;
        }
    }

    /// <summary>
    ///     Gets a callback that writes every link itself and overrides <see cref="LinkStyle"/> when set.
    /// </summary>
    /// <remarks>
    ///     The callback receives the link with global ids, the title, the display text, and the source, and only for
    ///     targets that are visible in the catalog. It returns the text that replaces the link.
    /// </remarks>
    public Func<HelpLink, string>? LinkFormatter { get; init; }

    /// <summary>
    ///     Gets the width at which generated text lines are wrapped; 78 by default, 0 for no wrapping.
    /// </summary>
    /// <remarks>
    ///     Wrapping applies only to lines the library generates and only to the text format. Authored bodies are
    ///     never reflowed, and a word longer than the width sits on its own line.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on initialization with a value that is neither 0 nor 20 to 500.</exception>
    public int Width
    {
        get => _width;
        init
        {
            // Zero switches wrapping off; every other value must be a sensible line length
            if (value != 0 && (value < MinimumWidth || value > MaximumWidth))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"The width must be 0 or between {MinimumWidth} and {MaximumWidth}.");
            }

            _width = value;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether a card shows its tags in text and Markdown; <see langword="true"/> by default.
    /// </summary>
    public bool IncludeTags { get; init; } = true;

    /// <summary>
    ///     Gets a value indicating whether a card shows the content of its examples in text and Markdown;
    ///     <see langword="false"/> by default.
    /// </summary>
    public bool IncludeExampleContent { get; init; }

    /// <summary>
    ///     Gets a callback that writes the top-level JSON object around the fields of a document.
    /// </summary>
    /// <remarks>
    ///     The callback receives the kind of the document and an action that writes the fields into a JSON writer,
    ///     and returns the complete document. It must write its own members first and then call the action while the
    ///     object is open. When it is not set, the default envelope with <c>helpSchemaVersion</c>, <c>ok</c>, and
    ///     <c>kind</c> is used.
    /// </remarks>
    public Func<string, Action<Utf8JsonWriter>, string>? JsonEnvelope { get; init; }
}
