namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     A single scalar value of the front matter together with the line it starts on.
/// </summary>
/// <remarks>
///     The text is already unquoted and unescaped. The line is the one-based line of the file where the
///     value starts (for a literal block scalar, the first content line), so later checks can report the
///     exact line of a bad value.
/// </remarks>
/// <param name="Text">The unquoted value.</param>
/// <param name="Line">The one-based line of the file the value starts on.</param>
internal sealed record FrontMatterScalar(string Text, int Line);
