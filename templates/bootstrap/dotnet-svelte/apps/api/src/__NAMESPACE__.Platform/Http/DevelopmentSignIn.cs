using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace __NAMESPACE__.Platform.Http;

/// <summary>
/// The development sign-in: a cookie session for the cast persons, with no
/// external provider. Every environment registers the cookie baseline; only
/// Development maps the sign-in route, so production has no way to mint a person.
/// </summary>
/// <follows>litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind</follows>
public static class DevelopmentSignIn
{
    /// <summary>The cookie authentication scheme of the development sign-in.</summary>
    public const string Scheme = "Development";

    /// <summary>The cookie name of the development session.</summary>
    public const string CookieName = "__SCOPE__.dev";

    /// <summary>Registers the development cookie session.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDevelopmentSignIn(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddAuthentication(Scheme).AddCookie(Scheme, options =>
        {
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                },
            };
        });
        return services;
    }

    /// <summary>Maps the development sign-in route.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void MapDevelopmentSignIn(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/auth/development/{person}", SignInAsync)
            .WithName("development-sign-in")
            .WithSummary("Development sign-in")
            .ExcludeFromDescription()
            .AllowAnonymous();
    }

    /// <summary>Signs the named cast person in with a session cookie.</summary>
    /// <param name="person">The person slug from the cast.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The signed-in person.</returns>
    private static async Task<IResult> SignInAsync(string person, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(person) || person.Length > 100)
        {
            return Results.BadRequest(new { error = "The person slug is required." });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, person),
            new Claim("person", person),
        };
        await context.SignInAsync(
            Scheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme))).ConfigureAwait(false);
        return Results.Ok(new { person });
    }
}
