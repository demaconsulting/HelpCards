using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="ProblemCollector"/>.
/// </summary>
public class ProblemCollectorTests
{
    /// <summary>
    ///     Creates an error problem for a source.
    /// </summary>
    /// <param name="rule">The rule id.</param>
    /// <param name="file">The file.</param>
    /// <param name="line">The line.</param>
    /// <returns>The problem.</returns>
    private static HelpProblem Error(string rule, string file, int line)
    {
        return HelpProblem.CreateError(rule, "demo", file, line, "Message.");
    }

    /// <summary>
    ///     Proves that problems are ordered by source, file, line, and rule.
    /// </summary>
    [Fact]
    public void ProblemCollector_GetOrdered_Problems_AreOrderedBySourceFileLineAndRule()
    {
        // Arrange: added in a scrambled order
        var collector = new ProblemCollector();
        collector.Add(1, Error("a", "a.md", 1));
        collector.Add(0, Error("z-rule", "b.md", 2));
        collector.Add(0, Error("a-rule", "b.md", 2));
        collector.Add(0, Error("any", "b.md", 1));
        collector.Add(0, Error("any", "B.md", 9));
        collector.Add(0, Error("any", "a.md", 7));

        // Act
        var ordered = collector.GetOrdered();

        // Assert: ordinal file order puts upper case first
        Assert.Equal(
            [("B.md", 9, "any"), ("a.md", 7, "any"), ("b.md", 1, "any"), ("b.md", 2, "a-rule"), ("b.md", 2, "z-rule"), ("a.md", 1, "a")],
            ordered.Select(problem => (problem.File, problem.Line, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that problems equal in every sort key keep the order in which they were found.
    /// </summary>
    [Fact]
    public void ProblemCollector_GetOrdered_EqualKeys_KeepInsertionOrder()
    {
        // Arrange
        var collector = new ProblemCollector();
        collector.Add(0, HelpProblem.CreateError("rule", "demo", "a.md", 1, "Second message."));
        collector.Add(0, HelpProblem.CreateError("rule", "demo", "a.md", 1, "First message."));

        // Act
        var messages = collector.GetOrdered().Select(problem => problem.Message);

        // Assert
        Assert.Equal(["Second message.", "First message."], messages);
    }

    /// <summary>
    ///     Proves that an identical problem of the same source is kept once.
    /// </summary>
    [Fact]
    public void ProblemCollector_GetOrdered_IdenticalProblem_IsKeptOnce()
    {
        // Arrange
        var collector = new ProblemCollector();
        collector.Add(0, Error("rule", "a.md", 1));
        collector.AddRange(0, [Error("rule", "a.md", 1), Error("rule", "a.md", 2)]);
        collector.Add(1, Error("rule", "a.md", 1));

        // Act
        var ordered = collector.GetOrdered();

        // Assert: the same problem in another source is a different problem
        Assert.Equal(3, ordered.Count);
    }

    /// <summary>
    ///     Proves that the severity filter and the error flag follow the collected problems.
    /// </summary>
    [Fact]
    public void ProblemCollector_SeverityFilter_AndHasErrors_FollowProblems()
    {
        // Arrange
        var collector = new ProblemCollector();
        var info = HelpProblem.CreateInfo("note", "demo", "a.md", 1, "Note.");

        // Act
        collector.Add(0, info);
        var hasErrorsWithInfoOnly = collector.HasErrors;
        collector.Add(0, Error("rule", "a.md", 2));

        // Assert
        Assert.False(hasErrorsWithInfoOnly);
        Assert.True(collector.HasErrors);
        Assert.Equal([info], collector.GetOrdered(HelpSeverity.Info));
        Assert.Single(collector.GetOrdered(HelpSeverity.Error));
        Assert.Empty(collector.GetOrdered(HelpSeverity.Warning));
        Assert.Equal(2, collector.GetOrdered().Count);
    }

    /// <summary>
    ///     Proves that null problems are rejected.
    /// </summary>
    [Fact]
    public void ProblemCollector_Add_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var collector = new ProblemCollector();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => collector.Add(0, null!));
        Assert.Throws<ArgumentNullException>(() => collector.AddRange(0, null!));
    }
}
