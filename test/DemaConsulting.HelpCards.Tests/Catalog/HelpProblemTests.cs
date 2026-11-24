using System.Reflection;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="HelpProblem"/>, <see cref="HelpSeverity"/>, and <see cref="RuleIds"/>.
/// </summary>
public class HelpProblemTests
{
    /// <summary>
    ///     Proves that a problem exposes every field it was created with.
    /// </summary>
    [Fact]
    public void HelpProblem_Constructor_AllFields_ExposesFields()
    {
        // Arrange: the fields of a problem
        // Act: create the problem
        var problem = new HelpProblem(HelpSeverity.Warning, "summary-length", "demo", "a/b.md", 7, "Too long.");

        // Assert: every field reads back unchanged
        Assert.Equal(HelpSeverity.Warning, problem.Severity);
        Assert.Equal("summary-length", problem.RuleId);
        Assert.Equal("demo", problem.SourceName);
        Assert.Equal("a/b.md", problem.File);
        Assert.Equal(7, problem.Line);
        Assert.Equal("Too long.", problem.Message);
    }

    /// <summary>
    ///     Proves that two problems with the same fields are equal, so problem lists can be compared.
    /// </summary>
    [Fact]
    public void HelpProblem_Equality_SameFields_AreEqual()
    {
        // Arrange: two problems built from the same values
        var first = new HelpProblem(HelpSeverity.Error, "syntax", "demo", "x.md", 2, "Bad.");
        var second = new HelpProblem(HelpSeverity.Error, "syntax", "demo", "x.md", 2, "Bad.");

        // Act / Assert: they are equal and have equal hash codes
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    ///     Proves that two problems that differ in one field are not equal.
    /// </summary>
    [Fact]
    public void HelpProblem_Equality_DifferentLine_AreNotEqual()
    {
        // Arrange: two problems that differ only in the line
        var first = new HelpProblem(HelpSeverity.Error, "syntax", "demo", "x.md", 2, "Bad.");
        var second = first with { Line = 3 };

        // Act / Assert: they are not equal
        Assert.NotEqual(first, second);
    }

    /// <summary>
    ///     Proves that the severities are ordered from informational to error.
    /// </summary>
    [Fact]
    public void HelpSeverity_Values_AreOrderedInfoWarningError()
    {
        // Arrange / Act: read the numeric values
        var values = new[] { (int)HelpSeverity.Info, (int)HelpSeverity.Warning, (int)HelpSeverity.Error };

        // Assert: Info < Warning < Error
        Assert.Equal([0, 1, 2], values);
    }

    /// <summary>
    ///     Proves that the error factory creates an error problem with the given fields.
    /// </summary>
    [Fact]
    public void HelpProblem_CreateError_GivenFields_CreatesErrorProblem()
    {
        // Arrange / Act: create an error through the factory
        var problem = HelpProblem.CreateError(RuleIds.LinkTarget, "demo", "c.md", 4, "No such card.");

        // Assert: the severity is Error and the fields are kept
        Assert.Equal(new HelpProblem(HelpSeverity.Error, "link-target", "demo", "c.md", 4, "No such card."), problem);
    }

    /// <summary>
    ///     Proves that every rule identifier is a distinct lower-case dashed name.
    /// </summary>
    [Fact]
    public void RuleIds_Constants_AreDistinctKebabCaseNames()
    {
        // Arrange: read every public constant of the rule identifier class
        var values = typeof(RuleIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        // Act / Assert: names are distinct and use lower-case letters and single dashes
        Assert.Equal(values.Count, values.Distinct(StringComparer.Ordinal).Count());
        Assert.All(values, value => Assert.Matches("^[a-z]+(-[a-z]+)*$", value));
    }

    /// <summary>
    ///     Proves that the rule identifiers include every rule the specification names for errors.
    /// </summary>
    [Fact]
    public void RuleIds_Constants_CoverSpecifiedErrorRules()
    {
        // Arrange: the error rules of the failure model
        string[] expected =
        [
            "file-name", "syntax", "key-unknown", "key-missing", "value-invalid", "block-scalar-not-allowed",
            "token-unknown", "token-redefined", "link-syntax", "link-target", "see-also-target", "example-target",
            "extends-target", "overlay-target", "children-duplicate", "children-unknown", "parent-missing",
            "collision", "tree-foreign", "name-duplicate", "example-invalid", "example-file-missing",
            "duplicate-example", "tag-unknown", "tag-required", "roots-mismatch", "source-invalid",
            "source-name-duplicate", "mount-invalid", "import-order", "import-missing", "import-cycle",
            "ambiguous-reference", "schema-unsupported", "translation-orphan", "translation-key",
            "culture-invalid", "source-read", "external-source-missing", "example-parse", "example-exit-code", "example-output"
        ];

        // Act: read the defined constants
        var defined = typeof(RuleIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        // Assert: every specified rule has a constant
        Assert.All(expected, rule => Assert.Contains(rule, defined));
    }

    /// <summary>
    ///     Proves that the rule identifiers include the informational and warning rules of the failure model.
    /// </summary>
    [Fact]
    public void RuleIds_Constants_CoverInfoAndWarningRules()
    {
        // Arrange
        string[] expected =
        [
            "external-source-absent", "overlay-optional-skipped", "summary-length", "card-size", "heading-reserved",
            "fence-language", "fence-unclosed", "unused-token", "line-length", "command-parse", "resources-mismatch"
        ];

        // Act
        var defined = typeof(RuleIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        // Assert
        Assert.All(expected, rule => Assert.Contains(rule, defined));
    }

    /// <summary>
    ///     Proves that the informational factory creates an informational problem with the given fields.
    /// </summary>
    [Fact]
    public void HelpProblem_CreateInfo_GivenFields_CreatesInfoProblem()
    {
        // Arrange / Act
        var problem = HelpProblem.CreateInfo(RuleIds.ExternalSourceAbsent, "demo", "c.md", 4, "Absent.");

        // Assert
        Assert.Equal(new HelpProblem(HelpSeverity.Info, "external-source-absent", "demo", "c.md", 4, "Absent."), problem);
    }

    /// <summary>
    ///     Proves that the display text names file, source, and line, and omits the line when it is 0.
    /// </summary>
    [Fact]
    public void HelpProblem_ToDisplayText_Fields_WritesOneSentence()
    {
        // Arrange
        (string File, int Line, string Message, string Expected)[] cases =
        [
            ("a/b.md", 7, "Bad value.", "Help card 'a/b.md' of source 'demo', line 7: Bad value."),
            ("a/b.md", 0, "Bad value.", "Help card 'a/b.md' of source 'demo': Bad value."),
            ("a/b.md", 3, "No period", "Help card 'a/b.md' of source 'demo', line 3: No period."),
            ("a/b.md", 3, "Ask why?", "Help card 'a/b.md' of source 'demo', line 3: Ask why?"),
            (string.Empty, 0, "No file.", "Help source 'demo': No file.")
        ];

        // Act / Assert
        foreach (var (file, line, message, expected) in cases)
        {
            Assert.Equal(expected, HelpProblem.CreateError(RuleIds.Syntax, "demo", file, line, message).ToDisplayText());
        }
    }

    /// <summary>
    ///     Proves that the exception carries every problem and a message with the first problem and the rest counted.
    /// </summary>
    [Fact]
    public void HelpCatalogException_Constructor_Problems_ExposesThemAndWritesMessage()
    {
        // Arrange
        var first = HelpProblem.CreateError(RuleIds.Syntax, "demo", "a.md", 2, "First.");
        var second = HelpProblem.CreateError(RuleIds.LinkTarget, "demo", "b.md", 3, "Second.");

        // Act
        var single = new HelpCatalogException([first]);
        var several = new HelpCatalogException([first, second, second]);

        // Assert
        Assert.Equal("Help card 'a.md' of source 'demo', line 2: First.", single.Message);
        Assert.Equal("Help card 'a.md' of source 'demo', line 2: First. (and 2 more)", several.Message);
        Assert.Equal([first, second, second], several.Problems);
        Assert.IsAssignableFrom<InvalidOperationException>(several);
    }

    /// <summary>
    ///     Proves that the exception cannot be created without a problem.
    /// </summary>
    [Fact]
    public void HelpCatalogException_Constructor_NoProblems_ThrowsArgumentException()
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new HelpCatalogException([]));
        Assert.Throws<ArgumentNullException>(() => new HelpCatalogException(null!));
    }
}
