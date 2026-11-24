using System.Globalization;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Reads one help source: classifies its files, decodes them, and builds definitions of its cards,
///     overlays, translations, and <c>_source.md</c>.
/// </summary>
/// <remarks>
///     <para>
///         The reader is the loader's workhorse. It lists the files of the source in ordinal order, rejects
///         names that fit no pattern (<c>file-name</c>), reads <c>_source.md</c> first because it declares the
///         name that decides which tokens are in scope, then reads every card and overlay file: it decodes the
///         front matter, checks the key matrix, validates every value against its grammar, replaces tokens,
///         splits the body into text and links, and applies the translation variant that the culture chain
///         selects for that file. Translation files are validated whether or not they are selected, so a
///         defective translation is found even when the default culture is requested.
///     </para>
///     <para>
///         The reader collects every problem it can find instead of stopping at the first. A file whose front
///         matter cannot be read contributes nothing and its id is listed in
///         <see cref="SourceDefinition.FailedIds"/> so references to it are not reported again. A reader
///         instance reads one source once; the static <see cref="Read"/> method is the entry point. Reading
///         performs file I/O through the source's provider and is not meant to be shared between threads, but
///         separate readers may run concurrently.
///     </para>
/// </remarks>
internal sealed partial class CardReader
{
    /// <summary>
    ///     The highest card format schema this loader reads; every schema from 1 up to it is read.
    /// </summary>
    public const int HighestSchema = 2;

    /// <summary>
    ///     The name used in problems before a source has a name.
    /// </summary>
    public const string UnnamedSource = "(unnamed)";

    /// <summary>
    ///     The default language of the neutral files of a source.
    /// </summary>
    private const string DefaultLanguage = "en";

    /// <summary>
    ///     The file stem of the source description.
    /// </summary>
    private const string SourceStem = "_source";

    /// <summary>
    ///     The most segments an id may have.
    /// </summary>
    private const int MaxIdSegments = 8;

    /// <summary>
    ///     The source being read.
    /// </summary>
    private readonly HelpSource _source;

    /// <summary>
    ///     The culture names to try for translations, most specific first.
    /// </summary>
    private readonly IReadOnlyList<string> _chain;

    /// <summary>
    ///     Creates the token resolver once the name of the source is known.
    /// </summary>
    private readonly Func<string, TokenResolver> _resolverFactory;

    /// <summary>
    ///     Every problem found so far, in the order found.
    /// </summary>
    private readonly List<HelpProblem> _problems = [];

    /// <summary>
    ///     Every listed path.
    /// </summary>
    private readonly HashSet<string> _files = new(StringComparer.Ordinal);

    /// <summary>
    ///     The raw text of the example data files read so far by path, or <see langword="null"/> when a read failed.
    /// </summary>
    private readonly Dictionary<string, string?> _exampleTexts = new(StringComparer.Ordinal);

    /// <summary>
    ///     The neutral card and overlay files by local id.
    /// </summary>
    private readonly SortedDictionary<string, string> _cards = new(StringComparer.Ordinal);

    /// <summary>
    ///     The translation files by neutral stem, then by culture suffix.
    /// </summary>
    private readonly Dictionary<string, SortedDictionary<string, string>> _translations = new(StringComparer.Ordinal);

    /// <summary>
    ///     The name of the source, or <see cref="UnnamedSource"/> until it is known.
    /// </summary>
    private string _name;

    /// <summary>
    ///     The path of <c>_source.md</c>, or <see langword="null"/> when the source has none.
    /// </summary>
    private string? _sourceFile;

    /// <summary>
    ///     The token scope of the source; assigned as soon as the name is known and before any text is read.
    /// </summary>
    private TokenResolver _tokens = null!;

    /// <summary>
    ///     The number of problems that make a file unusable, such as syntax and value errors.
    /// </summary>
    private int _structureErrors;

    /// <summary>
    ///     The schema of the source: 1 until <c>_source.md</c> has been read, then the value that it declares.
    /// </summary>
    private int _schema = 1;

    /// <summary>
    ///     Creates a reader for one source.
    /// </summary>
    /// <param name="source">The source to read.</param>
    /// <param name="culture">The culture whose translations are applied.</param>
    /// <param name="resolverFactory">Creates the token scope for the resolved name of the source.</param>
    private CardReader(HelpSource source, CultureInfo culture, Func<string, TokenResolver> resolverFactory)
    {
        _source = source;
        _chain = CultureSelector.GetFallbackChain(culture);
        _resolverFactory = resolverFactory;
        _name = source.Name ?? UnnamedSource;
    }

    /// <summary>
    ///     Reads a source completely.
    /// </summary>
    /// <remarks>
    ///     The call lists and reads the files of the source through its provider. Defects never throw; they
    ///     are returned in <see cref="SourceDefinition.Problems"/>.
    /// </remarks>
    /// <param name="source">The source to read. Must not be null.</param>
    /// <param name="culture">The culture whose translations are applied. Must not be null.</param>
    /// <param name="resolverFactory">
    ///     Creates the token scope for the resolved name of the source. Must not be null.
    /// </param>
    /// <returns>The definition of the source, including every problem found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static SourceDefinition Read(
        HelpSource source,
        CultureInfo culture,
        Func<string, TokenResolver> resolverFactory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(resolverFactory);
        return new CardReader(source, culture, resolverFactory).ReadSource();
    }

    /// <summary>
    ///     Runs the whole read.
    /// </summary>
    /// <returns>The definition of the source.</returns>
    private SourceDefinition ReadSource()
    {
        // Without a file list there is nothing to read; report it and return an empty definition
        var provisionalName = _name;
        if (!TryListFiles())
        {
            _tokens = _resolverFactory(_name);
            _problems.AddRange(_tokens.ScopeProblems);
            return CreateHeader(null, DefaultLanguage, 1);
        }

        ClassifyFiles();

        // The source file comes first: its name decides which tokens are in scope for every other file
        var document = DecodeSourceFile();
        _name = ResolveName(document);
        RenameProblems(provisionalName);
        _tokens = _resolverFactory(_name);
        _problems.AddRange(_tokens.ScopeProblems);

        var header = ReadSourceHeader(document);
        _schema = header.Schema;
        if (header.Schema > HighestSchema)
        {
            return header with { Problems = _problems.ToList() };
        }

        var cards = new List<CardDefinition>();
        var overlays = new List<OverlayDefinition>();
        var failed = new List<string>();
        foreach (var (localId, path) in _cards)
        {
            ReadCardFile(localId, path, header.Language, cards, overlays, failed);
        }

        ReportOrphans();
        return header with
        {
            Cards = cards,
            Overlays = overlays,
            FailedIds = failed,
            Problems = _problems.ToList()
        };
    }

    /// <summary>
    ///     Lists the files of the source and remembers them.
    /// </summary>
    /// <returns><see langword="true"/> when the files were listed.</returns>
    private bool TryListFiles()
    {
        try
        {
            foreach (var path in _source.Provider.ListFiles())
            {
                _files.Add(path);
            }

            return true;
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            Report(RuleIds.SourceRead, string.Empty, 0, $"The files of the source could not be listed ({Describe(ex)}).");
            return false;
        }
    }

    /// <summary>
    ///     Reads the text of one file; a failure becomes a <c>source-read</c> problem.
    /// </summary>
    /// <param name="path">The path of the file inside the source.</param>
    /// <param name="text">The text, or an empty string on failure.</param>
    /// <returns><see langword="true"/> when the file was read.</returns>
    private bool TryReadText(string path, out string text)
    {
        try
        {
            text = _source.Provider.ReadText(path);
            return true;
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            text = string.Empty;
            Report(RuleIds.SourceRead, path, 0, $"The file could not be read ({Describe(ex)}).");
            return false;
        }
    }

    /// <summary>
    ///     Reads and decodes a card, overlay, translation, or source file.
    /// </summary>
    /// <param name="path">The path of the file inside the source.</param>
    /// <param name="document">The decoded document, or <see langword="null"/> on failure.</param>
    /// <returns><see langword="true"/> when the file was decoded.</returns>
    private bool TryDecode(string path, out FrontMatterDocument? document)
    {
        document = null;
        if (!TryReadText(path, out var text))
        {
            _structureErrors++;
            return false;
        }

        if (FrontMatterDecoder.TryDecode(text, _name, path, out document, out var problem))
        {
            return true;
        }

        _problems.Add(problem!);
        _structureErrors++;
        return false;
    }

    /// <summary>
    ///     Records an error that does not make a file unusable.
    /// </summary>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="file">The path of the file, or an empty string.</param>
    /// <param name="line">The one-based line, or 0.</param>
    /// <param name="message">The message.</param>
    private void Report(string ruleId, string file, int line, string message)
    {
        _problems.Add(HelpProblem.CreateError(ruleId, _name, file, line, message));
    }

    /// <summary>
    ///     Records an error in the structure of a file, which makes the file unusable.
    /// </summary>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The one-based line.</param>
    /// <param name="message">The message.</param>
    private void ReportStructure(string ruleId, string file, int line, string message)
    {
        _structureErrors++;
        Report(ruleId, file, line, message);
    }

    /// <summary>
    ///     Replaces the provisional source name in problems that were found before the name was known.
    /// </summary>
    /// <param name="provisionalName">The name used until now.</param>
    private void RenameProblems(string provisionalName)
    {
        if (provisionalName == _name)
        {
            return;
        }

        for (var i = 0; i < _problems.Count; i++)
        {
            if (_problems[i].SourceName == provisionalName)
            {
                _problems[i] = _problems[i] with { SourceName = _name };
            }
        }
    }

    /// <summary>
    ///     Determines whether an exception from the provider is a failure to read rather than a fatal error.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <returns><see langword="false"/> for exceptions that must never be swallowed.</returns>
    private static bool IsRecoverable(Exception ex)
    {
        return ex is not (OutOfMemoryException or StackOverflowException or AccessViolationException or ThreadAbortException);
    }

    /// <summary>
    ///     Describes an exception by type and message.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <returns>The description.</returns>
    private static string Describe(Exception ex)
    {
        return $"{ex.GetType().Name}: {ex.Message}";
    }

    /// <summary>
    ///     Creates the header definition from the pieces read so far.
    /// </summary>
    /// <param name="document">The decoded source file, or <see langword="null"/>.</param>
    /// <param name="language">The language of the neutral files.</param>
    /// <param name="schema">The schema of the source.</param>
    /// <returns>A definition with no cards.</returns>
    private SourceDefinition CreateHeader(FrontMatterDocument? document, string language, int schema)
    {
        return new SourceDefinition(
            _name,
            _sourceFile ?? string.Empty,
            _name,
            $"Help cards of {_name}.",
            language,
            schema,
            [],
            [],
            new Dictionary<string, string>(),
            language,
            [],
            document?.BodyLine ?? 0,
            [],
            [],
            [],
            _problems.ToList());
    }
}
