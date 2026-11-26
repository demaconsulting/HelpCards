namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One finding about a help source: an error, a warning, or an informational note.
/// </summary>
/// <remarks>
///     <para>
///         Problems are plain immutable values so a caller can collect, sort, filter, and print them without
///         catching exceptions. Every problem names where it was found (source, file, and line) and which rule
///         it breaks, so an author can fix the card without reading library code. The record is immutable and
///         therefore safe for concurrent use.
///     </para>
///     <para>
///         <b>Where problems come from.</b> <c>HelpCatalog.Create</c> collects every <em>error</em> and throws one
///         <see cref="HelpCatalogException"/> whose <see cref="HelpCatalogException.Problems"/> lists them all;
///         <em>info</em> notes of a successful build are in <see cref="HelpCatalog.Diagnostics"/>; <em>warnings</em>
///         are produced only by <c>HelpCardValidator</c> (in <c>DemaConsulting.HelpCards.Testing</c>), which returns
///         errors first, then warnings. <c>AssertValid</c> throws for errors and, with
///         <c>WarningsAreErrors</c>, for warnings.
///     </para>
///     <para>
///         <b>Errors are reported in stages.</b> Each file is checked on its own, so the errors of different files
///         appear together, but the checks of one file are ordered: a <c>syntax</c> error hides every other finding of
///         that file; a key or value error (<c>key-unknown</c>, <c>key-missing</c>, <c>value-invalid</c>) still lets
///         <c>token-unknown</c> and <c>link-syntax</c> of its body appear, while <c>link-target</c>,
///         <c>example-target</c> and <c>see-also-target</c> are skipped for that file (and a link to it is not
///         reported). Warnings appear only when there is no error. Fix what is listed and run again: more problems can
///         show up.
///     </para>
///     <para>
///         <b>What <see cref="Line"/> 0 means.</b> A line number is the one-based line in <see cref="File"/>; 0 means
///         that no single line applies (the finding is about the whole file, a card, or the whole source), for example
///         <c>parent-missing</c>, <c>summary-length</c>, <c>card-size</c>, <c>command-parse</c>,
///         <c>unused-token</c>, <c>resources-mismatch</c>, <c>roots-mismatch</c> and <c>collision</c>. <see cref="File"/>
///         is empty when the finding is about the source as a whole. The text of a problem reads
///         <c>Help card 'a.md' of source 'demo', line 1: The required key 'summary' is missing.</c>;
///         with line 0 the part <c>, line 3</c> is left out, and without a file it is
///         <c>Help source 'mylib': ...</c>. The message of a <see cref="HelpCatalogException"/> shows only the first
///         problem and the number of the others, so print <see cref="HelpCatalogException.Problems"/> to see all.
///     </para>
///     <para>
///         <b>Every rule id.</b> This is the one table of the rule identifiers (the strings in <see cref="RuleId"/>).
///         Rule ids are part of the contract: you may match on them in tests.
///     </para>
///     <list type="table">
///         <listheader><term>Rule id</term><description>Severity and meaning</description></listheader>
///         <item><term><c>file-name</c></term><description>Error. A file name fits no pattern: a card is <c>segment.md</c> (lower case, digits, single hyphens), a translation <c>segment.culture.md</c>, plus <c>_source.md</c> and data files.</description></item>
///         <item><term><c>syntax</c></term><description>Error. The front matter is not valid in the supported subset: missing <c>---</c> line, flow collection, anchor, trailing comment, tab indentation, duplicate key, unterminated quote, an unquoted value that must be quoted, and so on.</description></item>
///         <item><term><c>key-unknown</c></term><description>Error. A front-matter key is not allowed for this kind of file; the message lists the allowed keys. The key <c>save-as</c> of an example in a schema 1 source gets a message that names schema 2.</description></item>
///         <item><term><c>key-missing</c></term><description>Error. A required key is missing: <c>title</c> or <c>summary</c> of a card, <c>extends</c> of an overlay, <c>name</c>, <c>kind</c>, <c>language</c> or <c>description</c> of an example, the <c>file</c> of a <c>source</c> or <c>snippet</c> example, or the <c>content</c> of a <c>command</c> example.</description></item>
///         <item><term><c>value-invalid</c></term><description>Error. A value does not follow the grammar of its key (for example an <c>order</c> that is not an integer, a tag that is not a segment, an unknown example <c>kind</c>, a <c>file</c> on a <c>command</c> example, an <c>exit</c> that is not an integer from 0 to 255, an empty <c>expect</c>, a <c>tokens</c> that is not <c>true</c> or <c>false</c>, or an <c>exit</c> or <c>expect</c> on a <c>source</c> or <c>snippet</c>).</description></item>
///         <item><term><c>block-scalar-not-allowed</c></term><description>Error. A literal block (<c>|</c>) is used anywhere but the <c>content</c> or the <c>expect</c> of an example.</description></item>
///         <item><term><c>token-unknown</c></term><description>Error. A <c>{{token}}</c> is not defined in the scope of its file (write <c>\{{</c> for a literal <c>{{</c>).</description></item>
///         <item><term><c>token-redefined</c></term><description>Error. A token name is defined twice in one scope or redefines <c>tool</c> or <c>version</c>.</description></item>
///         <item><term><c>link-syntax</c></term><description>Error. Text starts like a <c>[[link]]</c> but is not one.</description></item>
///         <item><term><c>link-target</c></term><description>Error. A link names a card that does not exist.</description></item>
///         <item><term><c>example-target</c></term><description>Error. A link names an example that does not exist.</description></item>
///         <item><term><c>see-also-target</c></term><description>Error. A <c>see-also</c> entry names a card that does not exist.</description></item>
///         <item><term><c>extends-target</c></term><description>Error. An overlay extends a card of a source that is not in the catalog (and the overlay is not <c>optional</c>) or that is added after the overlay's own source.</description></item>
///         <item><term><c>overlay-target</c></term><description>Error. An overlay extends something that is no card (an unknown id, or a generated mount root).</description></item>
///         <item><term><c>children-duplicate</c></term><description>Error. A <c>children</c> list names an entry twice.</description></item>
///         <item><term><c>children-unknown</c></term><description>Error. A <c>children</c> list names something that is not a direct child card.</description></item>
///         <item><term><c>parent-missing</c></term><description>Error. A card <c>a/b</c> has no parent card <c>a</c> (no <c>a.md</c>).</description></item>
///         <item><term><c>tree-foreign</c></term><description>Error. A card sits under a card of another source; use an overlay instead.</description></item>
///         <item><term><c>name-duplicate</c></term><description>Error. Two siblings or roots of one source share a name or an alias.</description></item>
///         <item><term><c>collision</c></term><description>Error. Two sources contribute the same global id, mount, or name.</description></item>
///         <item><term><c>example-invalid</c></term><description>Error. The content of an example is not valid: it is empty, it starts with a blank line, it is a list, or a <c>command</c> is not exactly one line.</description></item>
///         <item><term><c>example-file-missing</c></term><description>Error. The data file named by an example does not exist next to the card.</description></item>
///         <item><term><c>duplicate-example</c></term><description>Error. An example name is used twice in one card; names are unique per card, so two cards may use the same name.</description></item>
///         <item><term><c>tag-unknown</c></term><description>Error. A tag is not in the closed vocabulary set with <c>HelpCatalogBuilder.TagVocabulary</c>.</description></item>
///         <item><term><c>tag-required</c></term><description>Error. A card has none of the tags that the vocabulary requires.</description></item>
///         <item><term><c>roots-mismatch</c></term><description>Error. The root cards differ from those set with <c>HelpCatalogBuilder.ExpectedRoots</c>.</description></item>
///         <item><term><c>source-invalid</c></term><description>Error. A source has no valid name (<c>name</c> in <c>_source.md</c> or <c>WithName</c>) or an invalid description.</description></item>
///         <item><term><c>source-name-duplicate</c></term><description>Error. Two sources declare the same name.</description></item>
///         <item><term><c>mount-invalid</c></term><description>Error. A mount is not a single valid segment.</description></item>
///         <item><term><c>import-order</c></term><description>Error. A source is added before a source it imports; add the imported source first.</description></item>
///         <item><term><c>import-missing</c></term><description>Error. An imported source is not registered.</description></item>
///         <item><term><c>import-cycle</c></term><description>Error. Sources import each other in a cycle.</description></item>
///         <item><term><c>ambiguous-reference</c></term><description>Error. A bare reference matches cards of two sources, or an example link <c>[[#x]]</c> or <c>[[card#x]]</c> finds no <c>x</c> in the card itself and finds it in two cards below it; write <c>source:id</c> or <c>[[card#x]]</c> with the card that owns the example.</description></item>
///         <item><term><c>schema-unsupported</c></term><description>Error. A source declares a <c>schema</c> newer than this library supports (the highest is 2).</description></item>
///         <item><term><c>translation-orphan</c></term><description>Error. A translation file has no neutral file.</description></item>
///         <item><term><c>translation-key</c></term><description>Error. A translation contains a key that translations may not contain.</description></item>
///         <item><term><c>culture-invalid</c></term><description>Error. A culture name (a <c>language</c> value or a file suffix) is not valid.</description></item>
///         <item><term><c>source-read</c></term><description>Error. A file of a source could not be listed or read (for example, it is not valid UTF-8).</description></item>
///         <item><term><c>external-source-missing</c></term><description>Error, validator only. An external link names a source that is absent while <c>HelpValidationOptions.RequireExternalSources</c> is set.</description></item>
///         <item><term><c>external-source-absent</c></term><description>Info. An external link names a source that is not in the catalog; the link shows its text. Never fails anything.</description></item>
///         <item><term><c>overlay-optional-skipped</c></term><description>Info. An <c>optional</c> overlay was skipped because its target source is absent.</description></item>
///         <item><term><c>summary-length</c></term><description>Warning. A summary is longer than <c>MaxSummaryChars</c> (100) or, for a root card, <c>MaxRootSummaryChars</c> (80, so that the summary fits two lines of the topic index).</description></item>
///         <item><term><c>card-size</c></term><description>Warning. A card renders as more than <c>MaxCardChars</c> (6000) characters, or <c>MaxRootIndexChars</c> (3500) for a root card with children.</description></item>
///         <item><term><c>heading-reserved</c></term><description>Warning. A body has a level-one heading or one of the generated headings <c>## Cards</c>, <c>## Examples</c>, <c>## See also</c>.</description></item>
///         <item><term><c>fence-language</c></term><description>Warning. A fenced code block names no language.</description></item>
///         <item><term><c>fence-unclosed</c></term><description>Warning. A fenced code block is never closed.</description></item>
///         <item><term><c>unused-token</c></term><description>Warning. A token set with <c>WithTokens</c> is used by no file.</description></item>
///         <item><term><c>line-length</c></term><description>Warning. A body line is longer than <c>MaxLineLength</c>, or a line inside a fenced code block is longer than <c>MaxFenceLineLength</c> when that is set (both off unless set).</description></item>
///         <item><term><c>command-parse</c></term><description>Warning. A <c>command</c> example cannot be split into arguments by <c>ExampleCommandLine</c>.</description></item>
///         <item><term><c>save-as-duplicate</c></term><description>Warning. Two <c>source</c> examples of one card are saved under the same name, so <c>HelpExampleRunner</c> would keep only the last one.</description></item>
///         <item><term><c>resources-mismatch</c></term><description>Warning. <c>CompareToDirectory</c> (a packaging check, not a unit test) found a file on only one side: on disk but not embedded, or embedded but not on disk.</description></item>
///         <item><term><c>example-parse</c></term><description>Error, example runner only. A <c>command</c> example cannot be split into arguments by <c>ExampleCommandLine</c>, so <c>HelpExampleRunner</c> did not run it. It is reported in the run report, never by <c>HelpCatalog.Create</c>.</description></item>
///         <item><term><c>example-exit-code</c></term><description>Error, example runner only. A command ended with another exit code than the <c>exit</c> key of its example states (0 when absent).</description></item>
///         <item><term><c>example-output</c></term><description>Error, example runner only. The output of a command does not contain a fragment of the <c>expect</c> key of its example; one problem for each missing fragment.</description></item>
///     </list>
///     <para>
///         A rule you add through <c>HelpValidationOptions.Rules</c> reports its own rule ids.
///     </para>
/// </remarks>
/// <param name="Severity">How serious the problem is.</param>
/// <param name="RuleId">The identifier of the rule that was broken, such as <c>syntax</c> or <c>link-target</c>.</param>
/// <param name="SourceName">The name of the source the problem was found in.</param>
/// <param name="File">The path of the file inside the source, or an empty string when no file applies.</param>
/// <param name="Line">
///     The one-based line in the file, or 0 when no single line applies (the finding is about a whole file, card, or
///     source, such as <c>parent-missing</c>, <c>summary-length</c> or <c>resources-mismatch</c>).
/// </param>
/// <param name="Message">A sentence that says what is wrong and, where possible, how to fix it.</param>
public sealed record HelpProblem(
    HelpSeverity Severity,
    string RuleId,
    string SourceName,
    string File,
    int Line,
    string Message)
{
    /// <summary>
    ///     Creates an <see cref="HelpSeverity.Error"/> problem.
    /// </summary>
    /// <remarks>
    ///     A named factory keeps the many raise sites short and makes it obvious at each site that the
    ///     problem fails catalog creation.
    /// </remarks>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="sourceName">The name of the source the problem was found in.</param>
    /// <param name="file">The path of the file inside the source, or an empty string.</param>
    /// <param name="line">The one-based line, or 0 when no line applies.</param>
    /// <param name="message">The message that says what is wrong and how to fix it.</param>
    /// <returns>The new problem.</returns>
    internal static HelpProblem CreateError(string ruleId, string sourceName, string file, int line, string message)
    {
        return new HelpProblem(HelpSeverity.Error, ruleId, sourceName, file, line, message);
    }

    /// <summary>
    ///     Creates an <see cref="HelpSeverity.Info"/> problem.
    /// </summary>
    /// <remarks>
    ///     Informational problems record a deliberate degradation, such as a link to a source that is not
    ///     present. They never fail catalog creation.
    /// </remarks>
    /// <param name="ruleId">The rule the note belongs to.</param>
    /// <param name="sourceName">The name of the source the note is about.</param>
    /// <param name="file">The path of the file inside the source, or an empty string.</param>
    /// <param name="line">The one-based line, or 0 when no line applies.</param>
    /// <param name="message">The message that says what happened.</param>
    /// <returns>The new problem.</returns>
    internal static HelpProblem CreateInfo(string ruleId, string sourceName, string file, int line, string message)
    {
        return new HelpProblem(HelpSeverity.Info, ruleId, sourceName, file, line, message);
    }

    /// <summary>
    ///     Writes the problem as one line of text for a person.
    /// </summary>
    /// <remarks>
    ///     The text is <c>Help card '{file}' of source '{source}', line {n}: {message}.</c>. The line is left
    ///     out when it is 0. A problem that names no file reads <c>Help source '{source}': {message}.</c>. The
    ///     closing period is added only when the message does not already end a sentence.
    /// </remarks>
    /// <returns>The display text.</returns>
    internal string ToDisplayText()
    {
        var where = $"Help source '{SourceName}'";
        if (File.Length > 0)
        {
            where = $"Help card '{File}' of source '{SourceName}'";
            where += Line > 0 ? $", line {Line}" : string.Empty;
        }

        var endsSentence = Message.EndsWith('.') || Message.EndsWith('!') || Message.EndsWith('?');
        return $"{where}: {Message}{(endsSentence ? string.Empty : ".")}";
    }
}
