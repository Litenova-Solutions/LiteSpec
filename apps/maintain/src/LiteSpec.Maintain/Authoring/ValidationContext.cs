namespace LiteSpec.Authoring;

/// <summary>
///     The shared state one validation run carries: the repository root, the
///     diagnostic sink, the provision identifiers collected while parsing, and
///     the focused validators that share them.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed class ValidationContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidationContext"/> class.
    /// </summary>
    /// <param name="root">The resolved repository root.</param>
    /// <param name="add">The diagnostic sink, taking the relative path, line, code, and message.</param>
    public ValidationContext(string root, Action<string, int, string, string> add)
    {
        Root = root;
        Add = add;
        Pages = new PageValidator(this);
        Provisions = new ProvisionValidator(this);
        Manifest = new ManifestValidator(this);
        Repository = new RepositoryValidator(this);
        Marks = new MarksValidator(this);
        Records = new RecordsValidator(this);
        Fields = new FieldValidator(this);
        ValueStyle = new ValueStyleValidator(this);
    }

    /// <summary>Gets the resolved repository root.</summary>
    public string Root { get; }

    /// <summary>Gets the diagnostic sink, taking the relative path, line, code, and message.</summary>
    public Action<string, int, string, string> Add { get; }

    /// <summary>Gets the provision identifiers collected while parsing, mapped to their first location.</summary>
    public Dictionary<string, string> GlobalIds { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the page contract and per-page parse validator.</summary>
    public PageValidator Pages { get; }

    /// <summary>Gets the provision contract validator.</summary>
    public ProvisionValidator Provisions { get; }

    /// <summary>Gets the manifest and schema validator.</summary>
    public ManifestValidator Manifest { get; }

    /// <summary>Gets the repository-wide validator.</summary>
    public RepositoryValidator Repository { get; }

    /// <summary>Gets the self-mark validator.</summary>
    public MarksValidator Marks { get; }

    /// <summary>Gets the record validator.</summary>
    public RecordsValidator Records { get; }

    /// <summary>Gets the field-naming validator.</summary>
    public FieldValidator Fields { get; }

    /// <summary>Gets the value-style validator.</summary>
    public ValueStyleValidator ValueStyle { get; }
}
