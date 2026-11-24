using System.Globalization;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Unit tests for <see cref="HelpStrings"/>.
/// </summary>
public class HelpStringsTests
{
    /// <summary>
    ///     Proves that the headings and labels of the chrome are the English words of the layout.
    /// </summary>
    [Fact]
    public void HelpStrings_Chrome_HeadingsAndLabels_AreEnglish()
    {
        // Assert
        Assert.Equal("Cards", HelpStrings.CardsHeading);
        Assert.Equal("Examples", HelpStrings.ExamplesHeading);
        Assert.Equal("See also", HelpStrings.SeeAlsoHeading);
        Assert.Equal("Cards below with examples", HelpStrings.ExamplesBelowHeading);
        Assert.Equal("## Cards", HelpStrings.Heading(HelpStrings.CardsHeading));
        Assert.Equal("# Tags: examples", HelpStrings.ExamplesTitle("Tags"));
        Assert.Equal("Also known as:", HelpStrings.Label(HelpStrings.AliasesLabel, false));
        Assert.Equal("**Tags:**", HelpStrings.Label(HelpStrings.TagsLabel, true));
    }

    /// <summary>
    ///     Proves that the hint sentences name the command and the placeholder of the example name.
    /// </summary>
    [Fact]
    public void HelpStrings_Hints_NameTheCommand()
    {
        // Act
        var many = HelpStrings.PrintExamplesHint("tool help a");
        var one = HelpStrings.PrintOneExampleHint("tool help a");

        // Assert
        Assert.Equal("Print them in full with: tool help a --examples, or one alone with: tool help a --example <name>.", many);
        Assert.Equal("Print one alone with: tool help a --example <name>", one);
        Assert.Equal("No help cards match 'x y'.", HelpStrings.NoResults("x y"));
    }

    /// <summary>
    ///     Proves that the chrome does not depend on the current culture of the machine.
    /// </summary>
    [Fact]
    public void HelpStrings_Chrome_DoesNotDependOnCurrentCulture()
    {
        // Arrange: a culture with unusual casing and formatting rules
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // Act
            var heading = HelpStrings.Heading(HelpStrings.SeeAlsoHeading);
            var title = HelpStrings.ExamplesTitle("INFO");

            // Assert
            Assert.Equal("## See also", heading);
            Assert.Equal("# INFO: examples", title);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
