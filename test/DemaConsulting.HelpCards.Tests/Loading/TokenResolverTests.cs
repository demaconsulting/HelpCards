using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="TokenResolver"/>.
/// </summary>
public class TokenResolverTests
{
    /// <summary>
    ///     An empty token set.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> NoTokens = new Dictionary<string, string>();

    /// <summary>
    ///     Creates a resolver for a source with the given own tokens and no imports.
    /// </summary>
    /// <param name="own">The own tokens.</param>
    /// <returns>The resolver.</returns>
    private static TokenResolver Create(params (string Name, string Value)[] own)
    {
        return TokenResolver.Create(
            "lib",
            own.ToDictionary(pair => pair.Name, pair => pair.Value),
            [],
            "demo",
            "1.2");
    }

    /// <summary>
    ///     Proves that names of letters and digits are valid token names and others are not.
    /// </summary>
    [Fact]
    public void TokenResolver_IsValidTokenName_LettersAndDigits_ReturnsTrueOtherwiseFalse()
    {
        // Arrange / Act / Assert: ASCII letters and digits only, case preserved
        Assert.True(TokenResolver.IsValidTokenName("maxDpi"));
        Assert.True(TokenResolver.IsValidTokenName("A1"));
        Assert.False(TokenResolver.IsValidTokenName(string.Empty));
        Assert.False(TokenResolver.IsValidTokenName(null));
        Assert.False(TokenResolver.IsValidTokenName("max-dpi"));
        Assert.False(TokenResolver.IsValidTokenName("max dpi"));
        Assert.False(TokenResolver.IsValidTokenName("é"));
    }

    /// <summary>
    ///     Proves that the built-in tokens are replaced.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_BuiltInTokens_AreReplaced()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("Run {{tool}} {{version}}.", "a.md", 1, problems);

        // Assert
        Assert.Equal("Run demo 1.2.", text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that the tokens of the source are replaced, including more than one in one text.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_OwnTokens_AreReplaced()
    {
        // Arrange
        var resolver = Create(("maxDpi", "600"), ("a1", "x"));
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("{{maxDpi}} dpi, {{a1}}{{a1}}", "a.md", 1, problems);

        // Assert
        Assert.Equal("600 dpi, xx", text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that token names are case-sensitive.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_DifferentCase_IsUnknown()
    {
        // Arrange
        var resolver = Create(("Limit", "600"));
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("{{limit}}", "a.md", 1, problems);

        // Assert: the text is unchanged and the token is reported
        Assert.Equal("{{limit}}", text);
        Assert.Equal(RuleIds.TokenUnknown, Assert.Single(problems).RuleId);
    }

    /// <summary>
    ///     Proves that an unknown token is reported at its own line, counted from the first line of the text.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_UnknownTokenOnThirdLine_ReportsFileAndLine()
    {
        // Arrange: a text that starts on file line 10 and has the token on its third line
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("one\ntwo\nthree {{nope}} four", "a/b.md", 10, problems);

        // Assert: the token stays in the text and the problem is exact
        Assert.Equal("one\ntwo\nthree {{nope}} four", text);
        var problem = Assert.Single(problems);
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal(RuleIds.TokenUnknown, problem.RuleId);
        Assert.Equal("lib", problem.SourceName);
        Assert.Equal("a/b.md", problem.File);
        Assert.Equal(12, problem.Line);
    }

    /// <summary>
    ///     Proves that every unknown token of a text is reported, not only the first.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_SeveralUnknownTokens_ReportsEach()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        resolver.Substitute("{{a}}\n{{b}}", "a.md", 1, problems);

        // Assert
        Assert.Equal([1, 2], problems.Select(problem => problem.Line));
    }

    /// <summary>
    ///     Proves that text that only looks like a token is left alone without a problem.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_MalformedTokenLookalikes_AreLeftAlone()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("{{ tool }} {{a-b}} {{}} {x} {{tool}", "a.md", 1, problems);

        // Assert: none of them is a token
        Assert.Equal("{{ tool }} {{a-b}} {{}} {x} {{tool}", text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a backslash before the braces escapes a token: it renders as literal braces.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_BackslashBeforeToken_RendersLiteralBraces()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("\\{{tool}}", "a.md", 1, problems);

        // Assert: the backslash is dropped and the token is not replaced
        Assert.Equal("{{tool}}", text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that an escaped name that is not defined is not reported.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_EscapedUnknownName_IsNotReported()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("pkg\\{{Package}}", "a.md", 1, problems);

        // Assert
        Assert.Equal("pkg{{Package}}", text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that an escape does not hide a real unknown token beside it.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_EscapeBesideUnknownToken_StillReportsUnknown()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act: the unknown token is on the second line, which is file line 5
        var text = resolver.Substitute("\\{{a}}\n{{nope}} {{tool}}", "a.md", 4, problems);

        // Assert: the escape is rendered, the unknown token stays and is reported, the known one is replaced
        Assert.Equal("{{a}}\n{{nope}} demo", text);
        var problem = Assert.Single(problems);
        Assert.Equal(RuleIds.TokenUnknown, problem.RuleId);
        Assert.Equal(5, problem.Line);
    }

    /// <summary>
    ///     Proves that an escape needs no name after it and that other backslashes are kept.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_EscapeWithoutName_RemovesBackslash()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var text = resolver.Substitute("a\\{{ b C:\\dir \\{x}", "a.md", 1, problems);

        // Assert: only the backslash before two braces is dropped
        Assert.Equal("a{{ b C:\\dir \\{x}", text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that with two backslashes the first stays and the second escapes.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_DoubleBackslash_EscapesOnlyTheLast()
    {
        // Arrange
        var resolver = Create();

        // Act
        var text = resolver.Substitute("\\\\{{tool}}", "a.md", 1, new List<HelpProblem>());

        // Assert: one backslash stays and the token is not replaced
        Assert.Equal("\\{{tool}}", text);
    }

    /// <summary>
    ///     Proves that an escape at the end of the input, inside what looks like a token, and across CRLF line endings
    ///     renders as literal braces and leaves the rest of the text as the escape rule says.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="expected">The text after substitution.</param>
    [Theory]
    [InlineData("abc\\{{", "abc{{")]
    [InlineData("\\{{", "{{")]
    [InlineData("{{na\\{{me}}", "{{na{{me}}")]
    [InlineData("\\{{a}}\r\n{{tool}}\r\n\\{{b}}\r\n", "{{a}}\r\ndemo\r\n{{b}}\r\n")]
    public void TokenResolver_Substitute_EscapeAtEndInsideTokenAndCrLf_FollowsTheEscapeRule(string text, string expected)
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act
        var result = resolver.Substitute(text, "a.md", 1, problems);

        // Assert: the backslash is dropped, nothing is reported, and the line endings are untouched
        Assert.Equal(expected, result);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that an unknown token after CRLF line endings is reported at the right line.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_UnknownTokenAfterCrLf_ReportsItsLine()
    {
        // Arrange
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act: the unknown token is on the third line, which is file line 12
        var text = resolver.Substitute("\\{{a}}\r\n{{tool}}\r\n{{nope}}", "a.md", 10, problems);

        // Assert
        Assert.Equal("{{a}}\r\ndemo\r\n{{nope}}", text);
        var problem = Assert.Single(problems);
        Assert.Equal(RuleIds.TokenUnknown, problem.RuleId);
        Assert.Equal(12, problem.Line);
    }

    /// <summary>
    ///     Proves that tokens are replaced inside code fences and spans, as in the body of a card.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_InsideCode_AreReplaced()
    {
        // Arrange
        var resolver = Create();

        // Act
        var text = resolver.Substitute("`{{tool}}`\n```\n{{tool}} help\n```", "a.md", 1, new List<HelpProblem>());

        // Assert
        Assert.Equal("`demo`\n```\ndemo help\n```", text);
    }

    /// <summary>
    ///     Proves that a text without braces is returned unchanged.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_NoTokens_ReturnsTextUnchanged()
    {
        // Arrange
        var resolver = Create();

        // Act / Assert
        Assert.Equal("plain text", resolver.Substitute("plain text", "a.md", 1, new List<HelpProblem>()));
    }

    /// <summary>
    ///     Proves that the tokens of an imported source are in scope.
    /// </summary>
    [Fact]
    public void TokenResolver_Create_ImportedTokens_AreInScope()
    {
        // Arrange: an importing source and one imported source with a token
        var imported = new KeyValuePair<string, IReadOnlyDictionary<string, string>>(
            "base",
            new Dictionary<string, string> { ["maxDpi"] = "600" });

        // Act
        var resolver = TokenResolver.Create("app", NoTokens, [imported], "t", "v");

        // Assert: the imported token is replaced and no problem exists
        Assert.Equal("600", resolver.Substitute("{{maxDpi}}", "a.md", 1, new List<HelpProblem>()));
        Assert.Empty(resolver.ScopeProblems);
    }

    /// <summary>
    ///     Proves that a name defined by the source and by an import is reported as redefined.
    /// </summary>
    [Fact]
    public void TokenResolver_Create_OwnAndImportedSameName_ReportsRedefined()
    {
        // Arrange
        var own = new Dictionary<string, string> { ["x"] = "own" };
        var imported = new KeyValuePair<string, IReadOnlyDictionary<string, string>>(
            "base",
            new Dictionary<string, string> { ["x"] = "imported" });

        // Act
        var resolver = TokenResolver.Create("app", own, [imported], "t", "v");

        // Assert: one redefinition naming both sources; the first definition wins
        var problem = Assert.Single(resolver.ScopeProblems);
        Assert.Equal(RuleIds.TokenRedefined, problem.RuleId);
        Assert.Equal("app", problem.SourceName);
        Assert.Contains("'app'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'base'", problem.Message, StringComparison.Ordinal);
        Assert.Equal("own", resolver.Substitute("{{x}}", "a.md", 1, new List<HelpProblem>()));
    }

    /// <summary>
    ///     Proves that a name defined by two imports is reported as redefined.
    /// </summary>
    [Fact]
    public void TokenResolver_Create_TwoImportsSameName_ReportsRedefined()
    {
        // Arrange
        var first = new KeyValuePair<string, IReadOnlyDictionary<string, string>>("one", new Dictionary<string, string> { ["x"] = "1" });
        var second = new KeyValuePair<string, IReadOnlyDictionary<string, string>>("two", new Dictionary<string, string> { ["x"] = "2" });

        // Act
        var resolver = TokenResolver.Create("app", NoTokens, [first, second], "t", "v");

        // Assert
        Assert.Equal(RuleIds.TokenRedefined, Assert.Single(resolver.ScopeProblems).RuleId);
    }

    /// <summary>
    ///     Proves that the built-in names cannot be redefined by a source.
    /// </summary>
    [Fact]
    public void TokenResolver_Create_BuiltInNamesRedefined_ReportsRedefinedAndKeepsBuiltIn()
    {
        // Arrange: a source that defines both built-in names
        var own = new Dictionary<string, string> { ["tool"] = "other", ["version"] = "9" };

        // Act
        var resolver = TokenResolver.Create("lib", own, [], "demo", "1.2");

        // Assert: two problems and the built-in values still win
        Assert.Equal(2, resolver.ScopeProblems.Count);
        Assert.All(resolver.ScopeProblems, problem => Assert.Equal(RuleIds.TokenRedefined, problem.RuleId));
        Assert.Equal("demo 1.2", resolver.Substitute("{{tool}} {{version}}", "a.md", 1, new List<HelpProblem>()));
    }

    /// <summary>
    ///     Proves that a source does not see tokens that it did not import.
    /// </summary>
    [Fact]
    public void TokenResolver_Create_NotImportedSource_TokensAreUnknown()
    {
        // Arrange: a resolver for a source with no imports
        var resolver = Create();
        var problems = new List<HelpProblem>();

        // Act: use a token that belongs to some other source
        resolver.Substitute("{{maxDpi}}", "a.md", 1, problems);

        // Assert
        Assert.Equal(RuleIds.TokenUnknown, Assert.Single(problems).RuleId);
    }

    /// <summary>
    ///     Proves that the version token is empty when no version is known.
    /// </summary>
    [Fact]
    public void TokenResolver_Substitute_EmptyVersion_ReplacesWithEmptyText()
    {
        // Arrange
        var resolver = TokenResolver.Create("lib", NoTokens, [], "demo", string.Empty);

        // Act / Assert
        Assert.Equal("v=", resolver.Substitute("v={{version}}", "a.md", 1, new List<HelpProblem>()));
    }

    /// <summary>
    ///     Proves that the names of the well-formed tokens of a text are listed once each in order of first use and
    ///     that text that only looks like a token is not listed.
    /// </summary>
    [Fact]
    public void TokenResolver_FindNames_Text_ListsDistinctWellFormedNames()
    {
        // Act
        var names = TokenResolver.FindNames("{{b}} and {{a}} and {{b}}, but not {{ c }}, {{d-e}}, {f}, or {{}}.");
        var none = TokenResolver.FindNames("no tokens here");

        // Assert
        Assert.Equal(["b", "a"], names);
        Assert.Empty(none);
    }

    /// <summary>
    ///     Proves that an escaped token is not a token and is not listed.
    /// </summary>
    [Fact]
    public void TokenResolver_FindNames_EscapedToken_IsNotListed()
    {
        // Act
        var names = TokenResolver.FindNames("\\{{a}} {{b}}");

        // Assert: only the real token is listed
        Assert.Equal(["b"], names);
    }
}
