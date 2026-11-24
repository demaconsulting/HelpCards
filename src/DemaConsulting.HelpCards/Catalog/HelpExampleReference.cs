namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     An example together with the card it belongs to.
/// </summary>
/// <remarks>
///     Example lookups search several cards, so a result must say which card owns the example. The record is
///     immutable and compares by value.
/// </remarks>
/// <param name="CardId">The global id of the card that owns the example.</param>
/// <param name="Example">The example.</param>
public sealed record HelpExampleReference(string CardId, HelpExample Example);
