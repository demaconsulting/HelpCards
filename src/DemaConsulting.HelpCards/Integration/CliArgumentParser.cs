using System.Globalization;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     Reads the arguments that follow the help verb into a <see cref="CliRequest"/>, without any command-line library.
/// </summary>
/// <remarks>
///     <para>
///         Words that are not options are the words of the card path. Options may stand anywhere; <c>--</c> ends them. A
///         value option takes the next argument only, and an argument that starts with <c>--</c> is never taken as a
///         value. Every option may appear once, except <c>--tag</c> and <c>--source</c>, which repeat. Option names are
///         case sensitive.
///     </para>
///     <para>
///         The parser reports the first usage error it finds, in this order: errors found while reading the arguments from
///         left to right (unknown, repeated, or valueless options and invalid values), then <c>--json</c> against
///         <c>--format</c>, then conflicts between options, then options that need or forbid a card path. A parser
///         object is used once and is not shared between threads; the entry point is static and therefore safe.
///     </para>
/// </remarks>
internal sealed class CliArgumentParser
{
    /// <summary>
    ///     The least width of a wrapped line that the user may ask for.
    /// </summary>
    private const int MinimumWidth = HelpRenderOptions.MinimumWidth;

    /// <summary>
    ///     The most width of a wrapped line that the user may ask for.
    /// </summary>
    private const int MaximumWidth = HelpRenderOptions.MaximumWidth;

    /// <summary>
    ///     The options that take no value.
    /// </summary>
    private static readonly string[] FlagOptions = ["--json", "--examples", "--all"];

    /// <summary>
    ///     The options that take a value, without the language option.
    /// </summary>
    private static readonly string[] ValueOptions = ["--format", "--example", "--search", "--tag", "--source", "--width"];

    /// <summary>
    ///     The arguments to read.
    /// </summary>
    private readonly IReadOnlyList<string> _arguments;

    /// <summary>
    ///     Whether the language option is valid.
    /// </summary>
    private readonly bool _languageEnabled;

    /// <summary>
    ///     The options that were seen, for the check that an option is not repeated.
    /// </summary>
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    /// <summary>
    ///     The words that are not options.
    /// </summary>
    private readonly List<string> _words = [];

    /// <summary>
    ///     The values of the <c>--tag</c> options.
    /// </summary>
    private readonly List<string> _tags = [];

    /// <summary>
    ///     The values of the <c>--source</c> options.
    /// </summary>
    private readonly List<string> _sources = [];

    /// <summary>
    ///     The format named by <c>--format</c>, or <see langword="null"/>.
    /// </summary>
    private HelpFormat? _format;

    /// <summary>
    ///     The width the user gave, or <see langword="null"/>.
    /// </summary>
    private int? _width;

    /// <summary>
    ///     Whether the user asked for the width of the console.
    /// </summary>
    private bool _autoWidth;

    /// <summary>
    ///     The example name of <c>--example</c>, or <see langword="null"/>.
    /// </summary>
    private string? _example;

    /// <summary>
    ///     The query of <c>--search</c>, or <see langword="null"/>.
    /// </summary>
    private string? _search;

    /// <summary>
    ///     The culture of <c>--lang</c>, or <see langword="null"/>.
    /// </summary>
    private CultureInfo? _culture;

    /// <summary>
    ///     The message of the first error, or <see langword="null"/>.
    /// </summary>
    private string? _message;

    /// <summary>
    ///     The hint of the first error, or <see langword="null"/>.
    /// </summary>
    private string? _hint;

    /// <summary>
    ///     Creates a parser for one run.
    /// </summary>
    /// <param name="arguments">The arguments that follow the verb.</param>
    /// <param name="languageEnabled">Whether the language option is valid.</param>
    private CliArgumentParser(IReadOnlyList<string> arguments, bool languageEnabled)
    {
        _arguments = arguments;
        _languageEnabled = languageEnabled;
    }

    /// <summary>
    ///     Reads the arguments of a help command.
    /// </summary>
    /// <param name="arguments">The arguments that follow the verb. Must not be null or contain null.</param>
    /// <param name="languageEnabled">Whether the <c>--lang</c> option is valid.</param>
    /// <returns>The request, or the first usage error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null or contains null.</exception>
    public static CliParseResult Parse(IReadOnlyList<string> arguments, bool languageEnabled)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Any(argument => argument is null))
        {
            throw new ArgumentNullException(nameof(arguments), "The arguments must not contain null.");
        }

        return new CliArgumentParser(arguments, languageEnabled).Run();
    }

    /// <summary>
    ///     Gets the valid options, in the order in which messages list them.
    /// </summary>
    /// <param name="languageEnabled">Whether the language option is valid.</param>
    /// <returns>The option names.</returns>
    public static IReadOnlyList<string> GetValidOptions(bool languageEnabled)
    {
        var options = FlagOptions.Concat(ValueOptions).ToList();
        if (languageEnabled)
        {
            options.Add("--lang");
        }

        return options;
    }

    /// <summary>
    ///     Reads every argument and then checks the combination of options.
    /// </summary>
    /// <returns>The result.</returns>
    private CliParseResult Run()
    {
        var json = IsJsonRequested();
        var jsonFlag = false;
        var optionsEnded = false;
        var index = 0;
        while (index < _arguments.Count && _message is null)
        {
            var word = _arguments[index++];
            if (optionsEnded || word.Length < 2 || word[0] != '-')
            {
                _words.Add(word);
            }
            else if (word == "--")
            {
                optionsEnded = true;
            }
            else if (word == "--json")
            {
                if (!Repeated(word))
                {
                    jsonFlag = true;
                }
            }
            else if (FlagOptions.Contains(word))
            {
                Repeated(word);
            }
            else if (ValueOptions.Contains(word) || (_languageEnabled && word == "--lang"))
            {
                index += ReadValueOption(word, index);
            }
            else
            {
                Fail($"Unknown option '{word}'.", $"Valid options: {string.Join(", ", GetValidOptions(_languageEnabled))}.");
            }
        }

        if (_message is null)
        {
            return CheckCombination(jsonFlag, json);
        }

        return new CliParseResult(null, _message, _hint, json);
    }

    /// <summary>
    ///     Determines whether the arguments ask for JSON, so that even an error is written as JSON.
    /// </summary>
    /// <returns><see langword="true"/> when <c>--json</c> or <c>--format json</c> appears before <c>--</c>.</returns>
    private bool IsJsonRequested()
    {
        for (var index = 0; index < _arguments.Count && _arguments[index] != "--"; index++)
        {
            if (_arguments[index] == "--json"
                || (_arguments[index] == "--format" && index + 1 < _arguments.Count
                    && string.Equals(_arguments[index + 1], "json", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Records that an option was seen and reports an error when it was seen before.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <returns><see langword="true"/> when the option was seen before.</returns>
    private bool Repeated(string option)
    {
        if (_seen.Add(option))
        {
            return false;
        }

        Fail($"option '{option}' may be given only once.", null);
        return true;
    }

    /// <summary>
    ///     Reads a value option with its value.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <param name="valueIndex">The index of the argument after the option.</param>
    /// <returns>The number of arguments after the option that were used: 1 when its value was read, otherwise 0.</returns>
    private int ReadValueOption(string option, int valueIndex)
    {
        var repeatable = option is "--tag" or "--source";
        if (!repeatable && Repeated(option))
        {
            return 0;
        }

        // The value is the next argument only, and an argument that starts with "--" is never a value
        var value = valueIndex < _arguments.Count ? _arguments[valueIndex] : null;
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("--", StringComparison.Ordinal))
        {
            Fail($"option '{option}' needs a value.", null);
            return 0;
        }

        value = value.Trim();
        switch (option)
        {
            case "--format":
                ReadFormat(value);
                break;
            case "--width":
                ReadWidth(value);
                break;
            case "--lang":
                ReadCulture(value);
                break;
            case "--example":
                _example = value;
                break;
            case "--search":
                _search = value;
                break;
            case "--tag":
                _tags.Add(value);
                break;
            default:
                _sources.Add(value);
                break;
        }

        return 1;
    }

    /// <summary>
    ///     Reads the value of <c>--format</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    private void ReadFormat(string value)
    {
        _format = value.ToLowerInvariant() switch
        {
            "text" => HelpFormat.Text,
            "markdown" => HelpFormat.Markdown,
            "json" => HelpFormat.Json,
            _ => null,
        };
        if (_format is null)
        {
            Fail($"The format '{value}' is not valid.", "Valid formats: text, markdown, json.");
        }
    }

    /// <summary>
    ///     Reads the value of <c>--width</c>: an integer from 20 to 500, or <c>auto</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    private void ReadWidth(string value)
    {
        if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
        {
            _autoWidth = true;
        }
        else if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var width)
            && width is >= MinimumWidth and <= MaximumWidth)
        {
            _width = width;
        }
        else
        {
            Fail($"The width '{value}' is not valid.", $"Use a number from {MinimumWidth} to {MaximumWidth}, or auto.");
        }
    }

    /// <summary>
    ///     Reads the value of <c>--lang</c>: a well-formed culture name.
    /// </summary>
    /// <param name="value">The value.</param>
    private void ReadCulture(string value)
    {
        // A language of two or three letters followed by subtags of letters and digits, such as de or de-DE
        var parts = value.Split('-');
        var wellFormed = parts[0].Length is 2 or 3
            && parts[0].All(char.IsAsciiLetter)
            && parts.Skip(1).All(part => part.Length is >= 1 and <= 8 && part.All(char.IsAsciiLetterOrDigit));
        try
        {
            _culture = wellFormed ? CultureInfo.GetCultureInfo(value) : null;
        }
        catch (CultureNotFoundException)
        {
            _culture = null;
        }

        if (_culture is null)
        {
            Fail($"The language '{value}' is not a valid culture name.", "Use a name such as en or de-DE.");
        }
    }

    /// <summary>
    ///     Checks the combination of options and words and builds the request.
    /// </summary>
    /// <param name="jsonFlag">Whether <c>--json</c> was given.</param>
    /// <param name="json">Whether the arguments ask for JSON.</param>
    /// <returns>The result.</returns>
    private CliParseResult CheckCombination(bool jsonFlag, bool json)
    {
        var format = _format ?? HelpFormat.Text;
        if (jsonFlag && _format is not null and not HelpFormat.Json)
        {
            Fail($"option '--json' cannot be combined with '--format {_format.ToString()!.ToLowerInvariant()}'.", null);
        }
        else if (jsonFlag)
        {
            format = HelpFormat.Json;
        }

        var path = _words.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => word.Trim()).ToList();
        var modes = new List<string>();
        AddIf(modes, "--examples", _seen.Contains("--examples"));
        AddIf(modes, "--example", _example is not null);
        AddIf(modes, "--search", _search is not null);

        if (_message is null)
        {
            CheckConflicts(modes, path);
        }

        if (_message is not null)
        {
            return new CliParseResult(null, _message, _hint, json);
        }

        var mode = DetermineMode(path);
        var name = mode == CliMode.Search ? _search : _example;
        var request = new CliRequest(
            mode,
            path,
            format,
            _width,
            _autoWidth,
            name,
            _tags,
            _sources,
            _seen.Contains("--all"),
            _culture);
        return new CliParseResult(request, null, null, json);
    }

    /// <summary>
    ///     Reports the first conflict between modes and the other options, or a path that is needed or forbidden.
    /// </summary>
    /// <param name="modes">The mode options that were given, in the order examples, example, search.</param>
    /// <param name="path">The words of the card path.</param>
    private void CheckConflicts(List<string> modes, List<string> path)
    {
        var all = _seen.Contains("--all");
        if (modes.Count > 1)
        {
            Fail($"options '{modes[0]}' and '{modes[1]}' cannot be combined.", null);
        }
        else if (modes.Count == 1 && modes[0] != "--search" && _tags.Count > 0)
        {
            Fail($"options '{modes[0]}' and '--tag' cannot be combined.", null);
        }
        else if (modes.Count == 1 && all)
        {
            Fail($"options '{modes[0]}' and '--all' cannot be combined.", null);
        }
        else if (_tags.Count > 0 && all)
        {
            Fail("options '--tag' and '--all' cannot be combined.", null);
        }
        else if (_search is not null && path.Count > 0)
        {
            Fail("option '--search' takes one query and no card path.", "Quote a query of several words, for example --search \"two words\".");
        }
        else if (modes.Count == 1 && modes[0] != "--search" && path.Count == 0)
        {
            Fail($"option '{modes[0]}' needs a card path.", null);
        }
        else if (path.Count > 0 && _tags.Count > 0)
        {
            Fail("option '--tag' cannot be used with a card path.", null);
        }
        else if (path.Count > 0 && _sources.Count > 0)
        {
            Fail("option '--source' cannot be used with a card path.", null);
        }
        else if (path.Count > 0 && all)
        {
            Fail("option '--all' cannot be used with a card path.", null);
        }
    }

    /// <summary>
    ///     Determines the mode of the command from the options and the path.
    /// </summary>
    /// <param name="path">The words of the card path.</param>
    /// <returns>The mode.</returns>
    private CliMode DetermineMode(List<string> path)
    {
        if (_search is not null)
        {
            return CliMode.Search;
        }

        if (_seen.Contains("--examples"))
        {
            return CliMode.Examples;
        }

        if (_example is not null)
        {
            return CliMode.Example;
        }

        if (_tags.Count > 0)
        {
            return CliMode.Tags;
        }

        return path.Count > 0 ? CliMode.Card : CliMode.Index;
    }

    /// <summary>
    ///     Adds a name to a list when a condition holds.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="name">The name.</param>
    /// <param name="condition">The condition.</param>
    private static void AddIf(List<string> list, string name, bool condition)
    {
        if (condition)
        {
            list.Add(name);
        }
    }

    /// <summary>
    ///     Records the first error; later errors are ignored.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="hint">The hint, or <see langword="null"/>.</param>
    private void Fail(string message, string? hint)
    {
        if (_message is null)
        {
            _message = message;
            _hint = hint;
        }
    }
}
