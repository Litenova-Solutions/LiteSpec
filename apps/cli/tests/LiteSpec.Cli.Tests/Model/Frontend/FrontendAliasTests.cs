using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Frontend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves the import aliases the reader is given come from the frontend's package file.</summary>
public sealed class FrontendAliasTests
{
    /// <summary>Declared Subpath Imports Are The Aliases.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void DeclaredSubpathImportsAreTheAliases()
    {
        using var temp = TestCli.Temp("frontend-aliases");
        var app = Path.Combine(temp.Path, "apps", "web");
        Directory.CreateDirectory(app);
        File.WriteAllText(Path.Combine(app, "package.json"), """
            { "name": "web", "imports": { "#lib/*": "./src/lib/*", "#shared/*": "../shared/*" } }
            """);

        var aliases = NodeFrontendReader.Aliases(new FrontendConfig("web", app, "svelte"));

        Assert.Equal(
            [("#lib", Path.Combine(app, "src", "lib")), ("#shared", Path.GetFullPath(Path.Combine(app, "..", "shared")))],
            aliases);
    }

    /// <summary>A Frontend Declaring None Takes Its Stack Alias.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void AFrontendDeclaringNoneTakesItsStackAlias()
    {
        using var temp = TestCli.Temp("frontend-aliases-default");
        var app = Path.Combine(temp.Path, "apps", "web");
        Directory.CreateDirectory(app);

        Assert.Equal([("#lib", Path.Combine(app, "src", "lib"))], NodeFrontendReader.Aliases(new FrontendConfig("web", app, "svelte")));
        Assert.Equal([("@", Path.Combine(app, "src"))], NodeFrontendReader.Aliases(new FrontendConfig("admin", app, "react")));
    }
}
