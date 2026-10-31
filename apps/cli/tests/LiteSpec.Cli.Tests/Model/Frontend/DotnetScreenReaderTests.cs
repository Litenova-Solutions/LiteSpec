using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>
///     Proves the .NET reader reads Blazor route entries and Android destinations with their
///     screen marks and generated-client calls.
/// </summary>
public sealed class DotnetScreenReaderTests
{
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Page_Directive_Becomes_A_Route_At_Its_Template()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(temp.Path, "Pages/ExpenseList.razor", "<h1>Expenses</h1>\n@page \"/expenses\"\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal("Pages/ExpenseList.razor", route.File);
        Assert.Equal(2, route.Line);
        Assert.Equal("/expenses", route.Address);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Component_Without_A_Page_Directive_Is_No_Route()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Layout/MainLayout.razor",
            "<div>@Body</div>\n",
            "Components/ExpenseRow.razor",
            "<span>@Expense.Title</span>\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "blazor")));
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Escaped_And_Commented_Page_Directives_Are_No_Routes()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/Help.razor",
            "<p>Write @@page \"/help\" to route.</p>\n",
            "Pages/Old.razor",
            "@* @page \"/old\" *@\n<h1>Old</h1>\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "blazor")));
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Each_Page_Directive_Becomes_Its_Own_Route()
    {
        using var temp = Cli.Temp("dotnet-screens");
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

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Codebehind_Doc_Mark_Becomes_The_Route_Mark()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n/// <implements>screen/claimwick-app.expense-list</implements>\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Codebehind_Attribute_Mark_Becomes_The_Route_Mark()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n[Implements(\"screen/claimwick-app.expense-list\")]\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Code_Block_Mark_Becomes_The_Route_Mark_Without_Codebehind()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n\n@code {\n    /// <implements>screen/claimwick-app.expense-list</implements>\n    private string title = \"\";\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Route_Without_A_Mark_Reports_No_Marks()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Empty(route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Follows_Marks_Are_Not_Route_Marks()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\n/// <follows>litespec/frontend/blazor/stack#publish-static-webassembly-output</follows>\npublic partial class ExpenseList\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Empty(route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Codebehind_Client_Call_Becomes_A_Call_Without_Async()
    {
        using var temp = Cli.Temp("dotnet-screens");
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

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Non_Client_And_Non_Async_And_Undirected_Invocations_Are_No_Calls()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Pages/ExpenseList.razor.cs",
            "namespace Web.Pages;\n\npublic partial class ExpenseList\n{\n    private readonly ExpenseClient _expenses;\n    private readonly ILogger _logger;\n\n    public ExpenseList(ExpenseClient expenses, ILogger logger)\n    {\n        _expenses = expenses;\n        _logger = logger;\n    }\n\n    private async Task LoadAsync()\n    {\n        await _expenses.List();\n        await _logger.LogAsync(\"loaded\");\n        await RefreshAsync();\n    }\n\n    private Task RefreshAsync() => Task.CompletedTask;\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Empty(route.Calls);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Code_Block_Client_Call_Becomes_A_Call_At_Its_Razor_Line()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var razor =
            "@page \"/expenses\"\n\n@code {\n    private ExpenseClient _expenses = null!;\n\n    private async Task LoadAsync()\n    {\n        await _expenses.ListAsync();\n    }\n}\n";
        var root = WriteFrontend(temp.Path, "Pages/ExpenseList.razor", razor);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("List", call.Operation);
        Assert.Equal("Pages/ExpenseList.razor", call.File);
        Assert.Equal(LineOf(razor, "ListAsync"), call.Line);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Blazor_Inline_Markup_Client_Call_Becomes_A_Call()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var razor =
            "@page \"/expenses\"\n<h1>Expenses</h1>\n<button @onclick=\"() => ExpensesClient.SaveAsync()\">Save</button>\n";
        var root = WriteFrontend(temp.Path, "Pages/ExpenseList.razor", razor);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("Save", call.Operation);
        Assert.Equal("Pages/ExpenseList.razor", call.File);
        Assert.Equal(LineOf(razor, "SaveAsync"), call.Line);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Activity_Becomes_A_Destination_Named_By_Its_Class()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var source =
            "namespace Mobile.Screens;\n\n/// <implements>screen/claimwick-app.expense-list</implements>\npublic class ExpenseListActivity : Activity\n{\n}\n";
        var root = WriteFrontend(temp.Path, "Screens/ExpenseListActivity.cs", source);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Equal("Screens/ExpenseListActivity.cs", route.File);
        Assert.Equal(LineOf(source, "class ExpenseListActivity"), route.Line);
        Assert.Equal("ExpenseListActivity", route.Address);
        Assert.Equal(["screen/claimwick-app.expense-list"], route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Fragment_With_A_Qualified_Base_Becomes_A_Destination()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ReceiptFragment.cs",
            "namespace Mobile.Screens;\n\n[Implements(\"screen/claimwick-app.receipt\")]\npublic class ReceiptFragment : AndroidX.Fragment.App.Fragment\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Equal("ReceiptFragment", route.Address);
        Assert.Equal(["screen/claimwick-app.receipt"], route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Class_Without_A_Destination_Base_Is_No_Route()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseAdapter.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseAdapter : RecyclerView.Adapter\n{\n}\n",
            "Screens/Expenses.cs",
            "namespace Mobile.Screens;\n\npublic static class Expenses\n{\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "android")));
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Nested_Destination_Class_Is_No_Route()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/Outer.cs",
            "namespace Mobile.Screens;\n\npublic class Outer\n{\n    public class NestedActivity : Activity\n    {\n    }\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "android")));
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Destination_Without_A_Mark_Reports_No_Marks()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Empty(route.Marks);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Client_Call_Becomes_A_Call_Without_Async()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var source =
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n    private ExpenseClient _expenses = null!;\n\n    public void Load()\n    {\n        _expenses.ListAsync();\n    }\n}\n";
        var root = WriteFrontend(temp.Path, "Screens/ExpenseListActivity.cs", source);

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));
        var call = Assert.Single(route.Calls);

        Assert.Equal("List", call.Operation);
        Assert.Equal("Screens/ExpenseListActivity.cs", call.File);
        Assert.Equal(LineOf(source, "ListAsync"), call.Line);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Android_Non_Client_Invocation_Is_No_Call()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n    private Navigator _navigator = null!;\n\n    public void Load()\n    {\n        _navigator.ShowAsync();\n    }\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "android")));

        Assert.Empty(route.Calls);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Unknown_Platform_Reads_No_Routes()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n",
            "Screens/ExpenseListActivity.cs",
            "namespace Mobile.Screens;\n\npublic class ExpenseListActivity : Activity\n{\n}\n");

        Assert.Empty(DotnetScreenReader.Read(Config(root, "svelte")));
    }

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Blazor_Codebehind_Mark_Records_The_Style_Seen()
    {
        using var temp = Cli.Temp("dotnet-screens");
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

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Blazor_Code_Block_Marks_Record_Each_Style_Seen()
    {
        using var temp = Cli.Temp("dotnet-screens");
        var root = WriteFrontend(
            temp.Path,
            "Pages/ExpenseList.razor",
            "@page \"/expenses\"\n\n@code {\n    /// <implements>screen/claimwick-app.expense-list</implements>\n    [Implements(\"screen/claimwick-app.receipt\")]\n    private string title = \"\";\n}\n");

        var route = Assert.Single(DotnetScreenReader.Read(Config(root, "blazor")));

        Assert.Equal(["screen/claimwick-app.expense-list", "screen/claimwick-app.receipt"], route.Marks);
        Assert.Equal([MarkStyle.Xml, MarkStyle.Attribute], route.Styles);
    }

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Android_Destination_Mark_Records_The_Style_Seen()
    {
        using var temp = Cli.Temp("dotnet-screens");
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
