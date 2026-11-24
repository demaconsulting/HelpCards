using System.Globalization;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="CultureSelector"/>.
/// </summary>
public class CultureSelectorTests
{
    /// <summary>
    ///     Proves that the invariant culture selects only neutral files: its chain is empty.
    /// </summary>
    [Fact]
    public void CultureSelector_GetFallbackChain_InvariantCulture_IsEmpty()
    {
        // Arrange / Act
        var chain = CultureSelector.GetFallbackChain(CultureInfo.InvariantCulture);

        // Assert
        Assert.Empty(chain);
    }

    /// <summary>
    ///     Proves that a neutral culture has a chain of one name.
    /// </summary>
    [Fact]
    public void CultureSelector_GetFallbackChain_NeutralCulture_HasOneName()
    {
        // Arrange / Act
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de"));

        // Assert
        Assert.Equal(["de"], chain);
    }

    /// <summary>
    ///     Proves that a region culture falls back to its language.
    /// </summary>
    [Fact]
    public void CultureSelector_GetFallbackChain_RegionCulture_TrimsLastPart()
    {
        // Arrange / Act
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de-DE"));

        // Assert: the specific name first, then the language
        Assert.Equal(["de-DE", "de"], chain);
    }

    /// <summary>
    ///     Proves that a culture with a script subtag trims one subtag at a time.
    /// </summary>
    [Fact]
    public void CultureSelector_GetFallbackChain_ScriptAndRegion_TrimsOnePartAtATime()
    {
        // Arrange / Act
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("zh-Hans-CN"));

        // Assert
        Assert.Equal(["zh-Hans-CN", "zh-Hans", "zh"], chain);
    }

    /// <summary>
    ///     Proves that a null culture is rejected.
    /// </summary>
    [Fact]
    public void CultureSelector_GetFallbackChain_NullCulture_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => CultureSelector.GetFallbackChain(null!));
    }

    /// <summary>
    ///     Proves that the most specific available translation is selected.
    /// </summary>
    [Fact]
    public void CultureSelector_Select_SpecificAndLanguageAvailable_ReturnsSpecific()
    {
        // Arrange
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de-DE"));

        // Act
        var selected = CultureSelector.Select(chain, ["de", "de-DE", "fr"]);

        // Assert
        Assert.Equal("de-DE", selected);
    }

    /// <summary>
    ///     Proves that the language translation is selected when the region translation is missing.
    /// </summary>
    [Fact]
    public void CultureSelector_Select_OnlyLanguageAvailable_ReturnsLanguage()
    {
        // Arrange
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de-DE"));

        // Act
        var selected = CultureSelector.Select(chain, ["de", "fr"]);

        // Assert
        Assert.Equal("de", selected);
    }

    /// <summary>
    ///     Proves that no translation is selected when none of the chain exists, so the neutral file is used.
    /// </summary>
    [Fact]
    public void CultureSelector_Select_NoMatch_ReturnsNull()
    {
        // Arrange
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de-DE"));

        // Act
        var selected = CultureSelector.Select(chain, ["fr", "es"]);

        // Assert
        Assert.Null(selected);
    }

    /// <summary>
    ///     Proves that a more specific translation is not used for a less specific request.
    /// </summary>
    [Fact]
    public void CultureSelector_Select_RequestedLanguageOnlyRegionAvailable_ReturnsNull()
    {
        // Arrange: the request is "de" and only "de-DE" exists
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de"));

        // Act
        var selected = CultureSelector.Select(chain, ["de-DE"]);

        // Assert: the fallback never goes to a more specific culture
        Assert.Null(selected);
    }

    /// <summary>
    ///     Proves that names are compared ignoring case and the suffix is returned as written in the file.
    /// </summary>
    [Fact]
    public void CultureSelector_Select_DifferentCase_MatchesAndReturnsWrittenSuffix()
    {
        // Arrange: a file written with a lower-case region
        var chain = CultureSelector.GetFallbackChain(new CultureInfo("de-DE"));

        // Act
        var selected = CultureSelector.Select(chain, ["de-de"]);

        // Assert
        Assert.Equal("de-de", selected);
    }

    /// <summary>
    ///     Proves that an empty chain (the invariant culture) never selects a translation.
    /// </summary>
    [Fact]
    public void CultureSelector_Select_EmptyChain_ReturnsNull()
    {
        // Arrange / Act / Assert
        Assert.Null(CultureSelector.Select([], ["de", "fr"]));
    }

    /// <summary>
    ///     Proves which texts are accepted as culture suffixes.
    /// </summary>
    [Fact]
    public void CultureSelector_IsCultureSuffix_ValidNames_ReturnsTrue()
    {
        // Arrange / Act / Assert: two or three letters, then dash-separated subtags of 2 to 8 letters or digits
        Assert.True(CultureSelector.IsCultureSuffix("de"));
        Assert.True(CultureSelector.IsCultureSuffix("fil"));
        Assert.True(CultureSelector.IsCultureSuffix("de-DE"));
        Assert.True(CultureSelector.IsCultureSuffix("zh-Hans-CN"));
        Assert.True(CultureSelector.IsCultureSuffix("es-419"));
    }

    /// <summary>
    ///     Proves which texts are rejected as culture suffixes.
    /// </summary>
    [Fact]
    public void CultureSelector_IsCultureSuffix_InvalidNames_ReturnsFalse()
    {
        // Arrange / Act / Assert
        Assert.False(CultureSelector.IsCultureSuffix(string.Empty));
        Assert.False(CultureSelector.IsCultureSuffix("d"));
        Assert.False(CultureSelector.IsCultureSuffix("DE"));
        Assert.False(CultureSelector.IsCultureSuffix("de_DE"));
        Assert.False(CultureSelector.IsCultureSuffix("de-D"));
        Assert.False(CultureSelector.IsCultureSuffix("de-"));
        Assert.False(CultureSelector.IsCultureSuffix("1234"));
    }
}
