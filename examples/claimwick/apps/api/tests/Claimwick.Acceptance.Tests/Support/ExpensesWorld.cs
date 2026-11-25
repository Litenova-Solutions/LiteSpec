using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Claimwick.Acceptance.Tests.Support;

/// <summary>
///     The running API and the names one scenario gives its records. The acceptance
///     fixture sets the client before each scenario and clears the names after it.
/// </summary>
public static class ExpensesWorld
{
    static readonly Dictionary<string, Guid> ids = new(StringComparer.Ordinal);

    /// <summary>The client for the running API host.</summary>
    public static HttpClient Api { get; set; } = null!;

    /// <summary>The last answer a When step received.</summary>
    public static HttpResponseMessage? LastResponse { get; private set; }

    /// <summary>The identity a scenario name stands for, created on first use.</summary>
    public static Guid IdOf(string name)
    {
        if (!ids.TryGetValue(name, out var id))
        {
            id = Guid.CreateVersion7();
            ids[name] = id;
        }

        return id;
    }

    /// <summary>Binds a scenario name to an identity the API returned.</summary>
    public static void Name(string name, Guid id) => ids[name] = id;

    /// <summary>
    ///     Signs the following requests in as the named person, through the test
    ///     host's authentication scheme, which issues that person's identity as a claim.
    /// </summary>
    public static void SignInAs(string name)
    {
        Api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", IdOf(name).ToString());
    }

    /// <summary>Sends a POST and keeps the answer for the Then step.</summary>
    public static async Task PostAsync(string route, object? body = null)
    {
        LastResponse = await Api.PostAsJsonAsync(route, body ?? new { });
    }

    /// <summary>Sends a multipart POST carrying one file part and keeps the answer for the Then step.</summary>
    public static async Task PostMultipartAsync(string route, string partName, string fileName, string contentType, byte[] bytes)
    {
        using var files = new MultipartFormDataContent();
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        files.Add(part, partName, fileName);
        LastResponse = await Api.PostAsync(route, files);
    }

    /// <summary>Sends a GET and keeps the answer for the Then step.</summary>
    public static async Task GetAsync(string route)
    {
        LastResponse = await Api.GetAsync(route);
    }

    /// <summary>Clears the names between scenarios.</summary>
    public static void Reset() => ids.Clear();
}
