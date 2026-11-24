using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     One source as the adopter registered it with the builder, before anything is read.
/// </summary>
/// <remarks>
///     The registration order is significant: it decides the order of the roots, of overlays, and which imports
///     are allowed. The record is immutable.
/// </remarks>
/// <param name="Source">The source.</param>
/// <param name="Mounted">Whether the source is mounted under a root card of its own.</param>
/// <param name="Mount">The requested mount, or <see langword="null"/> for the declared name of the source.</param>
internal sealed record SourceRegistration(HelpSource Source, bool Mounted, string? Mount);
