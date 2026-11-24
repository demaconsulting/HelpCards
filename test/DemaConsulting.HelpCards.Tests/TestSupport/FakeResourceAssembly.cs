using System.Reflection;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     An assembly that serves embedded resources from memory, so tests can embed any bytes under any name.
/// </summary>
/// <remarks>
///     The runtime cannot embed a resource into a dynamic assembly, and the checked-in fixtures must stay valid
///     cards, so tests that need an invalid or byte-order-marked resource, or a resource name with a backslash as
///     MSBuild writes it on Windows, use this assembly. Only the members the resource provider calls are
///     implemented.
/// </remarks>
internal sealed class FakeResourceAssembly : Assembly
{
    /// <summary>
    ///     The resources by name.
    /// </summary>
    private readonly Dictionary<string, byte[]> _resources;

    /// <summary>
    ///     The simple name of the assembly.
    /// </summary>
    private readonly string _name;

    /// <summary>
    ///     Creates the assembly.
    /// </summary>
    /// <param name="name">The simple name of the assembly.</param>
    /// <param name="resources">The bytes of each resource by resource name.</param>
    public FakeResourceAssembly(string name, Dictionary<string, byte[]> resources)
    {
        _name = name;
        _resources = resources;
    }

    /// <summary>
    ///     Gets the name of the assembly.
    /// </summary>
    /// <returns>The assembly name.</returns>
    public override AssemblyName GetName()
    {
        return new AssemblyName(_name);
    }

    /// <summary>
    ///     Gets the names of the resources.
    /// </summary>
    /// <returns>The resource names.</returns>
    public override string[] GetManifestResourceNames()
    {
        return [.. _resources.Keys];
    }

    /// <summary>
    ///     Opens a resource.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <returns>A stream over the bytes, or <see langword="null"/> for an unknown name.</returns>
    public override Stream? GetManifestResourceStream(string name)
    {
        return _resources.TryGetValue(name, out var bytes) ? new MemoryStream(bytes) : null;
    }
}
