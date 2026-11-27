using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Explore;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for the walk driver against a fake handler: faults, denies, reuse, and determinism.</summary>
public sealed class ExploreWalkTests
{
    /// <summary>A 5xx is a server-error fault with the problem detail.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "a 5xx is a server-error fault with the problem detail")]
    public async Task ServerErrorIsAFault()
    {
        using var http = Client(new FakeExploreHandler(_ => Problem(HttpStatusCode.InternalServerError, "The list failed.")));
        var document = Document(""" "/expenses": {"post": {"requestBody": {"content": {"application/json": {"schema": {"type": "object", "properties": {"title": {"type": "string"}}, "required": ["title"]}}}}}}""");

        var result = await ExploreWalk.RunAsync(http, document, Walk(steps: 3, seed: 1), CancellationToken.None);

        Assert.Equal(3, result.Steps);
        Assert.Equal(3, result.Faults.Count);

        foreach (var fault in result.Faults)
        {
            Assert.Equal(ExploreFault.ServerError, fault.Kind);
            Assert.Equal(500, fault.StatusCode);
            Assert.Equal("The list failed.", fault.Detail);
            Assert.Contains("title", fault.RequestBody, StringComparison.Ordinal);
        }

        Assert.Equal(3, result.StatusCounts["500"]);
    }

    /// <summary>A 4xx outside problem details is a client-format fault.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "a 4xx outside problem details is a client-format fault")]
    public async Task ClientErrorOutsideProblemDetailsIsAFault()
    {
        using var http = Client(new FakeExploreHandler(_ => Text(HttpStatusCode.BadRequest, "bad request")));
        var document = Document(""" "/expenses": {"get": {}}""");

        var result = await ExploreWalk.RunAsync(http, document, Walk(steps: 2, seed: 1), CancellationToken.None);

        Assert.Equal(2, result.Faults.Count);

        foreach (var fault in result.Faults)
        {
            Assert.Equal(ExploreFault.ClientFormat, fault.Kind);
            Assert.Equal(400, fault.StatusCode);
            Assert.Null(fault.Detail);
        }

        Assert.Equal(2, result.StatusCounts["400"]);
    }

    /// <summary>A 4xx as problem details is no fault.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "a 4xx as problem details is no fault")]
    public async Task ClientErrorWithProblemDetailsIsNoFault()
    {
        using var http = Client(new FakeExploreHandler(_ => Problem(HttpStatusCode.BadRequest, "The title is missing.")));
        var document = Document(""" "/expenses": {"get": {}}""");

        var result = await ExploreWalk.RunAsync(http, document, Walk(steps: 2, seed: 1), CancellationToken.None);

        Assert.Empty(result.Faults);
        Assert.Equal(2, result.Steps);
        Assert.Equal(2, result.StatusCounts["400"]);
    }

    /// <summary>A call that times out is a timeout fault and the walk continues.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "a call that times out is a timeout fault and the walk continues")]
    public async Task TimeoutIsAFault()
    {
        using var http = Client(new FakeExploreHandler(_ => throw new TaskCanceledException("The request timed out.")));
        var document = Document(""" "/expenses": {"get": {}}""");

        var result = await ExploreWalk.RunAsync(http, document, Walk(steps: 2, seed: 1), CancellationToken.None);

        Assert.Equal(2, result.Steps);
        Assert.Equal(2, result.Faults.Count);

        foreach (var fault in result.Faults)
        {
            Assert.Equal(ExploreFault.Timeout, fault.Kind);
            Assert.Equal(0, fault.StatusCode);
            Assert.Equal(string.Empty, fault.ResponseExcerpt);
        }

        Assert.Equal(2, result.StatusCounts["timeout"]);
    }

    /// <summary>Denied operations are never called.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "denied operations are never called")]
    public async Task DeniedOperationsAreNeverCalled()
    {
        var fake = new FakeExploreHandler(_ => Text(HttpStatusCode.OK, "{}"));
        using var http = Client(fake);
        var document = Document(""" "/expenses": {"get": {}}, "/expenses/{expenseId}": {"delete": {}}""");

        var result = await ExploreWalk.RunAsync(
            http,
            document,
            Walk(steps: 20, seed: 1, denies: ExploreDeny.Defaults(allowDelete: false)),
            CancellationToken.None);

        Assert.Equal(1, result.OperationsListed);
        Assert.All(fake.Requests, request => Assert.Equal("GET", request.Method));
        Assert.Contains(result.Skipped, entry => entry.Template == "/expenses/{expenseId}");
    }

    /// <summary>--allow-delete walks DELETE operations.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "--allow-delete walks DELETE operations")]
    public async Task AllowDeleteWalksDeletes()
    {
        var fake = new FakeExploreHandler(_ => Text(HttpStatusCode.OK, "{}"));
        using var http = Client(fake);
        var document = Document(""" "/expenses/{expenseId}": {"delete": {}}""");

        var result = await ExploreWalk.RunAsync(
            http,
            document,
            Walk(steps: 3, seed: 1, denies: ExploreDeny.Defaults(allowDelete: true)),
            CancellationToken.None);

        Assert.Equal(1, result.OperationsListed);
        Assert.Equal(3, result.Steps);
        Assert.All(fake.Requests, request => Assert.Equal("DELETE", request.Method));
    }

    /// <summary>The same seed replays the same calls twice.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "the same seed replays the same calls twice")]
    public async Task SameSeedReplaysSameCalls()
    {
        var document = Document(
            """
            "/expenses": {
              "get": {},
              "post": {"requestBody": {"content": {"application/json": {"schema": {"type": "object", "properties": {"title": {"type": "string"}}}}}}}
            },
            "/expenses/{expenseId}": {"get": {}}
            """);
        var firstFake = new FakeExploreHandler(Answer);
        using var firstHttp = Client(firstFake);
        var first = await ExploreWalk.RunAsync(firstHttp, document, Walk(steps: 30, seed: 7), CancellationToken.None);
        var secondFake = new FakeExploreHandler(Answer);
        using var secondHttp = Client(secondFake);
        var second = await ExploreWalk.RunAsync(secondHttp, document, Walk(steps: 30, seed: 7), CancellationToken.None);

        Assert.Equal(firstFake.Requests, secondFake.Requests);
        Assert.Equal(first.Faults.Count, second.Faults.Count);

        static HttpResponseMessage Answer(HttpRequestMessage request) =>
            request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/expenses"
                ? Text(HttpStatusCode.OK, """[{"expenseId": "e-1"}]""")
                : Text(HttpStatusCode.OK, "{}");
    }

    /// <summary>A connection failure stops the walk with the reason.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    /// <returns>A task that completes when the walk is checked.</returns>
    [Fact(DisplayName = "a connection failure stops the walk with the reason")]
    public async Task ConnectionFailureStopsTheWalk()
    {
        using var http = Client(new FakeExploreHandler(_ => throw new HttpRequestException("Connection refused.")));
        var document = Document(""" "/expenses": {"get": {}}""");

        var result = await ExploreWalk.RunAsync(http, document, Walk(steps: 10, seed: 1), CancellationToken.None);

        Assert.Equal(1, result.Steps);
        Assert.Empty(result.Faults);
        Assert.Equal("Connection refused.", result.StoppedEarly);
    }

    /// <summary>A 5xx without a detail reports no detail.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a 5xx without a detail reports no detail")]
    public void FaultKindsDistinguishStatusAndContent()
    {
        Assert.True(ExploreWalk.IsFault(500, "text/html"));
        Assert.True(ExploreWalk.IsFault(500, "application/problem+json"));
        Assert.True(ExploreWalk.IsFault(404, "text/plain"));
        Assert.False(ExploreWalk.IsFault(404, "application/problem+json"));
        Assert.False(ExploreWalk.IsFault(404, "application/problem+json; charset=utf-8"));
        Assert.False(ExploreWalk.IsFault(200, "application/json"));
        Assert.False(ExploreWalk.IsFault(302, "text/html"));
        Assert.Null(ExploreWalk.ProblemDetail("<html>oops</html>"));
        Assert.Null(ExploreWalk.ProblemDetail("""{"title": "Failed."}"""));
        Assert.Equal("The list failed.", ExploreWalk.ProblemDetail("""{"title": "Failed.", "detail": "The list failed."}"""));
    }

    /// <summary>The walk settings of one test: the steps, the seed and the denies.</summary>
    /// <param name="steps">How many calls the walk makes.</param>
    /// <param name="seed">The seed that replays the same calls.</param>
    /// <param name="denies">The deny entries, or null for none.</param>
    /// <returns>The walk settings.</returns>
    static ExploreWalk.Settings Walk(int steps, int seed, IReadOnlyList<(ExploreDeny Deny, string Reason)>? denies = null) =>
        new(steps, seed, denies ?? [], [], null);

    /// <summary>An OpenAPI document holding the paths.</summary>
    /// <param name="paths">The paths object members.</param>
    /// <returns>The document.</returns>
    static JsonObject Document(string paths) =>
        JsonNode.Parse($"{{\"openapi\": \"3.1.0\", \"paths\": {{{paths}}}}}")!.AsObject();

    /// <summary>A client answering from the fake handler.</summary>
    /// <param name="fake">The fake handler.</param>
    /// <returns>The client.</returns>
    static HttpClient Client(FakeExploreHandler fake) =>
        new(fake) { BaseAddress = new Uri("http://localhost:5000") };

    /// <summary>A plain-text answer with the status.</summary>
    /// <param name="status">The response status.</param>
    /// <param name="body">The response body.</param>
    /// <returns>The answer.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The answer owns its content and the walk disposes the answer with using.")]
    static HttpResponseMessage Text(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };

    /// <summary>A problem-details answer with the status and the detail.</summary>
    /// <param name="status">The response status.</param>
    /// <param name="detail">The problem detail.</param>
    /// <returns>The answer.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The answer owns its content and the walk disposes the answer with using.")]
    static HttpResponseMessage Problem(HttpStatusCode status, string detail) =>
        new(status) { Content = new StringContent($"{{\"detail\": \"{detail}\"}}", Encoding.UTF8, "application/problem+json") };
}
