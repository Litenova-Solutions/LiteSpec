using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Screens;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>
///     Proves the .NET reader reads Android destinations with their
///     screen marks and generated-client calls.
/// </summary>
public sealed class DotnetScreenReaderTests
{
    /// <summary>Android Activity Becomes A Destination Named By Its Class.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidActivityBecomesADestinationNamedByItsClass()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var source =
            "namespace Mobile.Screens;\n\n/// <implements>screen/claimwick-app.expense-list</implements>\npublic class ExpenseListActivity : Activity\n{\n}\n";
        var root = WriteFrontend(temp.Path, "Screens/ExpenseListActivity.cs", source);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Equal("Screens/ExpenseListActivity.cs", route.File);
        Assert.Equal(LineOf(source, "class ExpenseListActivity"), route.Line);
        Assert.Equal("ExpenseListActivity", route.Address);
        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <summary>Android Fragment With A Qualified Base Becomes A Destination.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidFragmentWithAQualifiedBaseBecomesADestination()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ReceiptFragment.cs",
            "namespace Mobile.Screens;\n\n[Implements(\"screen/claimwick-app.receipt\")]\npublic class ReceiptFragment : AndroidX.Fragment.App.Fragment\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Equal("ReceiptFragment", route.Address);
        Assert.Equal(["screen/claimwick-app.receipt"], route.Marks);
    }

    /// <summary>Android Class Without A Destination Base Is No Route.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidClassWithoutADestinationBaseIsNoRoute()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseAdapter.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseAdapter : RecyclerView.Adapter\n{\n}\n",
            "Screens/Expenses.cs",
            "namespace Mobile.Screens;\n\npublic static class Expenses\n{\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "android")));
    }

    /// <summary>Android Nested Destination Class Is No Route.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidNestedDestinationClassIsNoRoute()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/Outer.cs",
            "namespace Mobile.Screens;\n\npublic class Outer\n{\n    public class NestedActivity : Activity\n    {\n    }\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "android")));
    }

    /// <summary>Android Destination Without A Mark Reports No Marks.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidDestinationWithoutAMarkReportsNoMarks()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Empty(route.Marks);
    }

    /// <summary>Android Client Call Becomes A Call Without Async.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidClientCallBecomesACallWithoutAsync()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var source =
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n    private ExpenseClient _expenses = null!;\n\n    public void Load()\n    {\n        _expenses.ListAsync();\n    }\n}\n";
        var root = WriteFrontend(temp.Path, "Screens/ExpenseListActivity.cs", source);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("List", call.Operation);
        Assert.Equal("Screens/ExpenseListActivity.cs", call.File);
        Assert.Equal(LineOf(source, "ListAsync"), call.Line);
    }

    /// <summary>Android Non Client Invocation Is No Call.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void AndroidNonClientInvocationIsNoCall()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n    private Navigator _navigator = null!;\n\n    public void Load()\n    {\n        _navigator.ShowAsync();\n    }\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Empty(route.Calls);
    }

    /// <summary>Unknown Platform Reads No Routes.</summary>
    /// <proves>litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen</proves>
    [Fact]
    public void UnknownPlatformReadsNoRoutes()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "svelte")));
    }

    /// <summary>Android Destination Mark Records The Style Seen.</summary>
    /// <proves>litespec/packs/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void AndroidDestinationMarkRecordsTheStyleSeen()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\n[Implements(\"screen/claimwick-app.expense-list\")]\npublic class ExpenseListActivity : Activity\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Equal([MarkStyle.Attribute], route.Styles);
    }

    static FrontendConfig Config(string root, string platform) => new("web", root, platform);

    static string WriteFrontend(string temp, string firstRelative, string firstText, params string[] rest)
    {
        var root = Path.Combine(temp, "apps", "web");
        Write(root, firstRelative, firstText);

        for (var index = 0; index < rest.Length; index += 2)
        {
            Write(root, rest[index], rest[index + 1]);
        }

        return root;
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static int LineOf(string text, string fragment)
    {
        var line = 1;

        foreach (var current in text.AsSpan(0, text.IndexOf(fragment, StringComparison.Ordinal)))
        {
            if (current == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
