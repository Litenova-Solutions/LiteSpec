using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Serve;

namespace LiteSpec.Cli.Tests.Serve;

/// <summary>Proves the serve session answers MCP messages from the warm model.</summary>
public sealed class ServeTests
{
    /// <summary>The borrow use case the packet, impact and slice tests read from the small fixture.</summary>
    const string BorrowId = "use-case/lending.borrow-copy";

    /// <summary>Runs one serve session over the small fixture and returns the response lines.</summary>
    /// <param name="lines">The JSON-RPC request lines, one message per line.</param>
    /// <returns>The response lines in order.</returns>
    static IReadOnlyList<string> Serve(params string[] lines)
    {
        using var prepared = Model.ModelFixture.Prepare("small");
        var config = ConsumerConfig.Load(prepared.Path);
        using var input = new StringReader(string.Join("\n", lines));
        using var output = new StringWriter();
        var exit = ServeCommand.RunSession(input, output, config);
        Assert.Equal(0, exit);
        return output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>The result object of the single response, parsed as JSON.</summary>
    /// <param name="responses">The response lines.</param>
    /// <returns>The result object.</returns>
    static JsonObject ResultOf(IReadOnlyList<string> responses)
    {
        var response = Assert.Single(responses);
        return Assert.IsType<JsonObject>(JsonNode.Parse(response)!["result"]);
    }

    /// <summary>The text of a tools/call result.</summary>
    /// <param name="responses">The response lines.</param>
    /// <returns>The content text.</returns>
    static string CallText(IReadOnlyList<string> responses)
    {
        var result = ResultOf(responses);
        var content = Assert.IsType<JsonArray>(result["content"]);
        var first = Assert.IsType<JsonObject>(Assert.Single(content));
        Assert.False(first.ContainsKey("isError"));
        return first["text"]!.GetValue<string>();
    }

    /// <summary>Calls one tool against the small fixture and returns the content text.</summary>
    /// <param name="name">The tool name.</param>
    /// <param name="arguments">The tool arguments object.</param>
    /// <returns>The content text.</returns>
    static string CallTool(string name, string arguments) =>
        CallText(Serve(Request(1, "tools/call", $"{{\"name\":\"{name}\",\"arguments\":{arguments}}}")));

    /// <summary>One JSON-RPC request line.</summary>
    /// <param name="id">The request id.</param>
    /// <param name="method">The request method.</param>
    /// <param name="paramsJson">The params object, or null for none.</param>
    /// <returns>The request line.</returns>
    static string Request(int id, string method, string? paramsJson = null) =>
        paramsJson is null
            ? $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"{method}\"}}"
            : $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"{method}\",\"params\":{paramsJson}}}";

    /// <summary>Initialize answers the protocol version and the server name.</summary>
    [Fact]
    public void InitializeAnswers()
    {
        var result = ResultOf(Serve(Request(1, "initialize", """{"protocolVersion":"2024-11-05","capabilities":{}}""")));

        Assert.Equal("2024-11-05", result["protocolVersion"]!.GetValue<string>());
        Assert.Equal("litespec-serve", result["serverInfo"]!["name"]!.GetValue<string>());
    }

    /// <summary>Tools/list names the four tools.</summary>
    [Fact]
    public void ToolsListNamesFourTools()
    {
        var result = ResultOf(Serve(Request(1, "tools/list")));
        var tools = Assert.IsType<JsonArray>(result["tools"]);

        Assert.Equal(
            ["litespec_task_packet", "litespec_check", "litespec_impact", "litespec_verify_slice"],
            tools.Select(tool => tool!["name"]!.GetValue<string>()));
    }

    /// <summary>Every tool states its input schema with its required argument.</summary>
    [Fact]
    public void ToolsListStatesInputSchemas()
    {
        var result = ResultOf(Serve(Request(1, "tools/list")));
        var tools = Assert.IsType<JsonArray>(result["tools"])
            .Select(tool => Assert.IsType<JsonObject>(tool!))
            .ToDictionary(tool => tool["name"]!.GetValue<string>());

        Assert.Equal(["target"], RequiredOf(tools["litespec_task_packet"]));
        Assert.Equal([], RequiredOf(tools["litespec_check"]));
        Assert.Equal(["record_id"], RequiredOf(tools["litespec_impact"]));
        Assert.Equal(["use_case_id"], RequiredOf(tools["litespec_verify_slice"]));

        static IReadOnlyList<string> RequiredOf(JsonObject tool) =>
            Assert.IsType<JsonArray>(tool["inputSchema"]!["required"]!).Select(node => node!.GetValue<string>()).ToList();
    }

    /// <summary>The task packet carries the record, its judgment provisions and its neighbours inside the cap.</summary>
    [Fact]
    public void TaskPacketCarriesRecordProvisionsAndNeighbours()
    {
        var text = CallTool("litespec_task_packet", $"{{\"target\":\"{BorrowId}\"}}");

        Assert.Contains(BorrowId, text, StringComparison.Ordinal);
        Assert.Contains("Judgment provisions", text, StringComparison.Ordinal);
        Assert.Contains("Neighbours", text, StringComparison.Ordinal);
        Assert.True(text.Length <= ServeTools.PacketCap);
    }

    /// <summary>Capping states the cut instead of silently trimming.</summary>
    [Fact]
    public void PacketCapStatesTheCut()
    {
        var capped = ServeTools.Cap(new string('x', ServeTools.PacketCap + 1));

        // The brief caps the packet at PacketCap characters with the cut stated, so the note fits inside the cap.
        Assert.True(capped.Length <= ServeTools.PacketCap);
        Assert.Matches($@"\[cut: showing \d+ of {ServeTools.PacketCap + 1} characters\]$", capped);
        Assert.StartsWith(new string('x', ServeTools.PacketCap - 100), capped, StringComparison.Ordinal);
    }

    /// <summary>The check answers the open findings from the warm model.</summary>
    [Fact]
    public void CheckAnswersOpenFindings()
    {
        var text = CallTool("litespec_check", "{}");

        Assert.Contains("open findings", text, StringComparison.Ordinal);
    }

    /// <summary>The impact answers the record links in both directions.</summary>
    [Fact]
    public void ImpactAnswersBothDirections()
    {
        var text = CallTool("litespec_impact", $"{{\"record_id\":\"{BorrowId}\"}}");

        Assert.Contains($"Impact for {BorrowId}", text, StringComparison.Ordinal);
        Assert.Contains("Outgoing links", text, StringComparison.Ordinal);
        Assert.Contains("Incoming links", text, StringComparison.Ordinal);
        Assert.Contains("Code", text, StringComparison.Ordinal);
        Assert.Contains("Tests", text, StringComparison.Ordinal);
    }

    /// <summary>The verify slice answers the findings, scenarios with proof, code and tests.</summary>
    [Fact]
    public void VerifySliceAnswersFindingsScenariosCodeAndTests()
    {
        var text = CallTool("litespec_verify_slice", $"{{\"use_case_id\":\"{BorrowId}\"}}");

        Assert.Contains($"Verify slice for {BorrowId}", text, StringComparison.Ordinal);
        Assert.Contains("Findings", text, StringComparison.Ordinal);
        Assert.Contains("Scenarios", text, StringComparison.Ordinal);
        Assert.Contains("Code files", text, StringComparison.Ordinal);
        Assert.Contains("Test files", text, StringComparison.Ordinal);
    }

    /// <summary>An unknown tool returns a JSON-RPC error.</summary>
    [Fact]
    public void UnknownToolReturnsJsonRpcError()
    {
        var response = Assert.Single(Serve(Request(1, "tools/call", """{"name":"litespec_missing","arguments":{}}""")));
        var error = Assert.IsType<JsonObject>(JsonNode.Parse(response)!["error"]);

        Assert.Equal(-32601, error["code"]!.GetValue<int>());
        Assert.Contains("Unknown tool", error["message"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>A notification answers nothing.</summary>
    [Fact]
    public void InitializedNotificationAnswersNothing()
    {
        var responses = Serve(
            Request(1, "initialize", """{"protocolVersion":"2024-11-05","capabilities":{}}"""),
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            Request(2, "ping"));

        Assert.Equal(2, responses.Count);
    }
}
