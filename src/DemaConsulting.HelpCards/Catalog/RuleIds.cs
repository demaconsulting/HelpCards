namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The identifiers of every rule that a <see cref="HelpProblem"/> can name.
/// </summary>
/// <remarks>
///     Rule identifiers are part of the observable contract (tests and adopters match on them), so they are
///     named constants in one place instead of string literals scattered across the units that raise them.
///     The groups follow the failure model: errors make catalog creation fail, informational notes never
///     fail, and warnings are produced only by the validator.
///     The identifiers of the composition of several sources and overlays are raised by the Composition
///     subsystem: <see cref="Collision"/>, <see cref="TreeForeign"/>, <see cref="ExtendsTarget"/>,
///     <see cref="OverlayTarget"/>, <see cref="SourceNameDuplicate"/>, <see cref="MountInvalid"/>,
///     <see cref="ImportOrder"/>, <see cref="ImportMissing"/>, <see cref="ImportCycle"/>,
///     <see cref="AmbiguousReference"/>, and <see cref="OverlayOptionalSkipped"/>.
///     Within one source a repeated sibling name or alias is <see cref="NameDuplicate"/>, never
///     <see cref="Collision"/>.
/// </remarks>
internal static class RuleIds
{
    // Errors that make catalog creation fail

    /// <summary>A file name fits no file name pattern.</summary>
    public const string FileName = "file-name";

    /// <summary>The front matter of a file is not valid in the supported subset.</summary>
    public const string Syntax = "syntax";

    /// <summary>A front-matter key is not allowed for the kind of file.</summary>
    public const string KeyUnknown = "key-unknown";

    /// <summary>A required front-matter key is missing.</summary>
    public const string KeyMissing = "key-missing";

    /// <summary>A front-matter value does not follow the grammar of its key.</summary>
    public const string ValueInvalid = "value-invalid";

    /// <summary>A literal block scalar is used where only a single-line value is allowed.</summary>
    public const string BlockScalarNotAllowed = "block-scalar-not-allowed";

    /// <summary>A token is not defined in the scope of its file.</summary>
    public const string TokenUnknown = "token-unknown";

    /// <summary>A token name is defined more than once in one scope or redefines a built-in.</summary>
    public const string TokenRedefined = "token-redefined";

    /// <summary>Text starts like a link but is not one.</summary>
    public const string LinkSyntax = "link-syntax";

    /// <summary>A link names a card or an example that does not exist.</summary>
    public const string LinkTarget = "link-target";

    /// <summary>A see-also entry names a card that does not exist.</summary>
    public const string SeeAlsoTarget = "see-also-target";

    /// <summary>A link names an example that does not exist.</summary>
    public const string ExampleTarget = "example-target";

    /// <summary>An overlay extends a card that does not exist.</summary>
    public const string ExtendsTarget = "extends-target";

    /// <summary>An overlay targets something that cannot be targeted.</summary>
    public const string OverlayTarget = "overlay-target";

    /// <summary>A <c>children</c> list names an entry twice.</summary>
    public const string ChildrenDuplicate = "children-duplicate";

    /// <summary>A <c>children</c> list names something that is not a direct child card.</summary>
    public const string ChildrenUnknown = "children-unknown";

    /// <summary>A card has no parent card although its path has a parent folder.</summary>
    public const string ParentMissing = "parent-missing";

    /// <summary>Two sources contribute the same global id, mount, or name.</summary>
    public const string Collision = "collision";

    /// <summary>A card of one source sits under a card of another source.</summary>
    public const string TreeForeign = "tree-foreign";

    /// <summary>Two siblings or roots share a name or alias.</summary>
    public const string NameDuplicate = "name-duplicate";

    /// <summary>An example is not valid for its kind.</summary>
    public const string ExampleInvalid = "example-invalid";

    /// <summary>The file of an example does not exist.</summary>
    public const string ExampleFileMissing = "example-file-missing";

    /// <summary>An example name is used twice in one scope.</summary>
    public const string DuplicateExample = "duplicate-example";

    /// <summary>A tag is not in the closed vocabulary.</summary>
    public const string TagUnknown = "tag-unknown";

    /// <summary>A card has none of the required tags.</summary>
    public const string TagRequired = "tag-required";

    /// <summary>The roots of the catalog differ from the expected roots.</summary>
    public const string RootsMismatch = "roots-mismatch";

    /// <summary>A source has no valid name or an invalid description.</summary>
    public const string SourceInvalid = "source-invalid";

    /// <summary>Two sources declare the same name.</summary>
    public const string SourceNameDuplicate = "source-name-duplicate";

    /// <summary>A mount is not a valid single segment.</summary>
    public const string MountInvalid = "mount-invalid";

    /// <summary>A source is imported after the source that imports it.</summary>
    public const string ImportOrder = "import-order";

    /// <summary>An imported source is not registered.</summary>
    public const string ImportMissing = "import-missing";

    /// <summary>Sources import each other in a cycle.</summary>
    public const string ImportCycle = "import-cycle";

    /// <summary>A bare reference matches cards of two sources.</summary>
    public const string AmbiguousReference = "ambiguous-reference";

    /// <summary>A source declares a schema newer than the loader supports.</summary>
    public const string SchemaUnsupported = "schema-unsupported";

    /// <summary>A translation file has no neutral file.</summary>
    public const string TranslationOrphan = "translation-orphan";

    /// <summary>A translation file contains a key that translations may not contain.</summary>
    public const string TranslationKey = "translation-key";

    /// <summary>A culture name is not valid.</summary>
    public const string CultureInvalid = "culture-invalid";

    /// <summary>A file of a source could not be listed or read.</summary>
    public const string SourceRead = "source-read";

    // Informational notes

    /// <summary>An external link names a source that is not present.</summary>
    public const string ExternalSourceAbsent = "external-source-absent";

    /// <summary>An optional overlay was skipped because its target source is absent.</summary>
    public const string OverlayOptionalSkipped = "overlay-optional-skipped";

    // Warnings produced only by the validator

    /// <summary>A summary is longer than the limit or spans lines.</summary>
    public const string SummaryLength = "summary-length";

    /// <summary>A card is larger than the limit.</summary>
    public const string CardSize = "card-size";

    /// <summary>A body uses a reserved heading.</summary>
    public const string HeadingReserved = "heading-reserved";

    /// <summary>A fenced block names no language.</summary>
    public const string FenceLanguage = "fence-language";

    /// <summary>A fenced block is not closed.</summary>
    public const string FenceUnclosed = "fence-unclosed";

    /// <summary>A token is defined but never used.</summary>
    public const string UnusedToken = "unused-token";

    /// <summary>A body line is longer than the limit.</summary>
    public const string LineLength = "line-length";

    /// <summary>A command example does not parse.</summary>
    public const string CommandParse = "command-parse";

    /// <summary>Files on disk and in the assembly differ.</summary>
    public const string ResourcesMismatch = "resources-mismatch";

    /// <summary>An external source is absent although the caller required it.</summary>
    public const string ExternalSourceMissing = "external-source-missing";
}
