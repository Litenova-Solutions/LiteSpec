namespace LiteSpec.Analyzers.Rules;

/// <summary>A LiteSpec provision a diagnostic enforces, and its published address.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class Provision
{
    /// <summary>The release the help links point at.</summary>
    public const string Release = "3.0.0";

    /// <summary>Initializes a new instance of the <see cref="Provision"/> class.</summary>
    /// <param name="page">The docs page path under docs/, such as backend/dotnet/domain.</param>
    /// <param name="slug">The heading slug within the page, such as model-every-closed-set-of-domain-values-without-enums.</param>
    private Provision(string page, string slug)
    {
        Page = page;
        Slug = slug;
    }

    /// <summary>Gets the page path under docs/, such as <c>backend/dotnet/domain</c>.</summary>
    public string Page { get; }

    /// <summary>Gets the heading slug, such as <c>model-every-closed-set-of-domain-values-without-enums</c>.</summary>
    public string Slug { get; }

    /// <summary>Gets the provision ID: <c>litespec/&lt;page&gt;#&lt;slug&gt;</c>.</summary>
    public string Id => $"litespec/{Page}#{Slug}";

    /// <summary>Gets the provision on litespec.dev.</summary>
    public string HelpLink => $"https://litespec.dev/{Release}/{Page}/#{Slug}";

    /// <summary>Creates the provision for the page and slug.</summary>
    /// <param name="page">The docs page path under docs/, such as backend/dotnet/domain.</param>
    /// <param name="slug">The heading slug within the page, such as model-every-closed-set-of-domain-values-without-enums.</param>
    /// <returns>The provision.</returns>
    public static Provision Of(string page, string slug) => new(page, slug);
}
