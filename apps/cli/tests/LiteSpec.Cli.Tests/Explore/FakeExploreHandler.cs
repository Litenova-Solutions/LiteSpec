namespace LiteSpec.Cli.Tests.Explore;

/// <summary>A message handler that answers every request from its responder and records the calls.</summary>
sealed class FakeExploreHandler : HttpMessageHandler
{
    readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

    /// <summary>Initializes a new instance of the <see cref="FakeExploreHandler"/> class.</summary>
    /// <param name="responder">The answer for one request, throwing to fail the call.</param>
    public FakeExploreHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        this.responder = responder;
    }

    /// <summary>Gets the requests in the order they arrived: the method and the path with its query.</summary>
    public List<(string Method, string Path)> Requests { get; } = [];

    /// <summary>Sends one request: records it and answers from the responder.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The responder's answer.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(Send(request));

    /// <summary>Records one request and answers it.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The responder's answer.</returns>
    HttpResponseMessage Send(HttpRequestMessage request)
    {
        Requests.Add((request.Method.Method, request.RequestUri!.PathAndQuery));
        return responder(request);
    }
}
