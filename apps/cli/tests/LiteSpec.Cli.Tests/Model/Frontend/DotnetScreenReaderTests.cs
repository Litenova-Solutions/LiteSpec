using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>
///     Proves the .NET reader reads Blazor route entries and Android destinations with their
///     screen marks and generated-client calls.
/// </summary>
public sealed class DotnetScreenReaderTests
{
    /// <summary>Blazor Page Directive Becomes A Route At Its Template.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorPageDirectiveBecomesARouteAtItsTemplate()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(temp.Path, "Pages/ExpenseList.razor", "<h1>Expenses</h1>\n@page \"/expenses\"\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal("Pages/ExpenseList.razor", route.File);
        Assert.Equal(2, route.Line);
        Assert.Equal("/expenses", route.Address);
    }

    /// <summary>Blazor Component Without A Page Directive Is No Route.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorComponentWithoutAPageDirectiveIsNoRoute()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Layout/MainLayout.razor",
            "<div>@Body</div>\n",
            "Components/ExpenseRow.razor",
            "<span>@Expense.Title</span>\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "blazor")));
    }

    /// <summary>Blazor Escaped And Commented Page Directives Are No Routes.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorEscapedAndCommentedPageDirectivesAreNoRoutes()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/Help.razor",
            "<p>Write @@page \"/help\" to route.</p>\n",
            "Pages/Old.razor",
            "@* @page \"/old\" *@\n<h1>Old</h1>\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "blazor")));
    }

    /// <summary>Blazor Each Page Directive Becomes Its Own Route.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorEachPageDirectiveBecomesItsOwnRoute()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n@page \"/spending/expenses\"\n<h1>Expenses</h1>\n");

        var routes = DotnetScreenReader.Read(Config(root, "blazor"));

        Assert.Equal(2, routes.Count);
        Assert.Equal("/expenses", routes[0].Address);
        Assert.Equal(1, routes[0].Line);
        Assert.Equal("/spending/expenses", routes[1].Address);
        Assert.Equal(2, routes[1].Line);
    }

    /// <summary>Blazor Codebehind Doc Mark Becomes The Route Mark.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorCodebehindDocMarkBecomesTheRouteMark()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n/// <implements>screen/claimwick-app.expense-list</implements>\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <summary>Blazor Codebehind Attribute Mark Becomes The Route Mark.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorCodebehindAttributeMarkBecomesTheRouteMark()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n[Implements(\"screen/claimwick-app.expense-list\")]\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <summary>Blazor Code Block Mark Becomes The Route Mark Without Codebehind.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorCodeBlockMarkBecomesTheRouteMarkWithoutCodebehind()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n\n@code {\n    /// <implements>screen/claimwick-app.expense-list</implements>\n    private string title = \"\";\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <summary>Blazor Route Without A Mark Reports No Marks.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorRouteWithoutAMarkReportsNoMarks()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Empty(route.Marks);
    }

    /// <summary>Blazor Follows Marks Are Not Route Marks.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorFollowsMarksAreNotRouteMarks()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Empty(route.Marks);
    }

    /// <summary>Blazor Codebehind Client Call Becomes A Call Without Async.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorCodebehindClientCallBecomesACallWithoutAsync()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var behind =
            "namespace Web.Pages;\n\npublic partial class ExpenseList\n{\n    private readonly ExpenseClient _expenses;\n\n    public ExpenseList(ExpenseClient expenses)\n    {\n        _expenses = expenses;\n    }\n\n    private async Task LoadAsync()\n    {\n        await _expenses.ListAsync();\n    }\n}\n";
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            behind);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("List", call.Operation);
        Assert.Equal("Pages/ExpenseList.razor.cs", call.File);
        Assert.Equal(LineOf(behind, "ListAsync"), call.Line);
    }

    /// <summary>Blazor Non Client And Non Async And Undirected Invocations Are No Calls.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorNonClientAndNonAsyncAndUndirectedInvocationsAreNoCalls()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\npublic partial class ExpenseList\n{\n    private readonly ExpenseClient _expenses;\n    private readonly ILogger _logger;\n\n    public ExpenseList(ExpenseClient expenses, ILogger logger)\n    {\n        _expenses = expenses;\n        _logger = logger;\n    }\n\n    private async Task LoadAsync()\n    {\n        await _expenses.List();\n        await _logger.LogAsync(\"loaded\");\n        await RefreshAsync();\n    }\n\n    private Task RefreshAsync() => Task.CompletedTask;\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Empty(route.Calls);
    }

    /// <summary>Blazor Code Block Client Call Becomes A Call At Its Razor Line.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorCodeBlockClientCallBecomesACallAtItsRazorLine()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var razor =
            "@page \"/expenses\"\n\n@code {\n    private ExpenseClient _expenses = null!;\n\n    private async Task LoadAsync()\n    {\n        await _expenses.ListAsync();\n    }\n}\n";
        var root = WriteFrontend(temp.Path, "Pages/ExpenseList.razor", razor);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("List", call.Operation);
        Assert.Equal("Pages/ExpenseList.razor", call.File);
        Assert.Equal(LineOf(razor, "ListAsync"), call.Line);
    }

    /// <summary>Blazor Inline Markup Client Call Becomes A Call.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void BlazorInlineMarkupClientCallBecomesACall()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var razor =
            "@page \"/expenses\"\n<h1>Expenses</h1>\n<button @onclick=\"() => ExpensesClient.SaveAsync()\">Save</button>\n";
        var root = WriteFrontend(temp.Path, "Pages/ExpenseList.razor", razor);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("Save", call.Operation);
        Assert.Equal("Pages/ExpenseList.razor", call.File);
        Assert.Equal(LineOf(razor, "SaveAsync"), call.Line);
    }

    /// <summary>Android Activity Becomes A Destination Named By Its Class.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
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
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void UnknownPlatformReadsNoRoutes()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "svelte")));
    }

    /// <summary>Blazor Codebehind Mark Records The Style Seen.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void BlazorCodebehindMarkRecordsTheStyleSeen()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var doc = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n/// <implements>screen/claimwick-app.expense-list</implements>\npublic partial class ExpenseList\n{\n}\n");
        var attributed = WriteFrontend(
            temp.Path,
            "Pages/Receipt.razor",
            "@page \"/receipt\"\n",
            "Pages/Receipt.razor.cs",
            "namespace Web.Pages;\n\n[Implements(\"screen/claimwick-app.receipt\")]\npublic partial class Receipt\n{\n}\n");

        var fromDoc = DotnetScreenReader.Read(Config(doc, "blazor")).Single(route => route.Address == "/expenses");
        var fromAttribute = DotnetScreenReader.Read(Config(attributed, "blazor")).Single(route => route.Address == "/receipt");

        Assert.Equal([MarkStyle.Xml], fromDoc.Styles);
        Assert.Equal([MarkStyle.Attribute], fromAttribute.Styles);
    }

    /// <summary>Blazor Code Block Marks Record Each Style Seen.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void BlazorCodeBlockMarksRecordEachStyleSeen()
    {
        using var temp = TestCli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n\n@code {\n    /// <implements>screen/claimwick-app.expense-list</implements>\n    [Implements(\"screen/claimwick-app.receipt\")]\n    private string title = \"\";\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list", "screen/claimwick-app.receipt"], route.Marks);
        Assert.Equal([MarkStyle.Xml, MarkStyle.Attribute], route.Styles);
    }

    /// <summary>Android Destination Mark Records The Style Seen.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
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
