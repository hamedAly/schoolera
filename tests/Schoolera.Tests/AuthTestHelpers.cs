using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Schoolera.Tests;

/// <summary>
/// Cookie + antiforgery helpers for authenticated integration tests against the dev seed users.
/// </summary>
internal static class AuthTestHelpers
{
    public const string DefaultPassword = "Schoolera@Dev1";
    public const string ParentEmail = "parent@schoolera.local";
    public const string SchoolOwnerEmail = "schoolowner@schoolera.local";
    public const string SchoolAdminEmail = "schooladmin@schoolera.local";
    public const string AdmissionOfficerEmail = "admissionofficer@schoolera.local";
    public const string FinanceOfficerEmail = "financeofficer@schoolera.local";
    public const string ContentModeratorEmail = "contentmoderator@schoolera.local";
    public const string PlatformAdminEmail = "admin@schoolera.local";
    public const string SupportAgentEmail = "support@schoolera.local";
    public const string DemoSchoolSlug = "cairo-international-school";

    public static HttpClient CreateCookieClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    public static async Task<bool> TryLoginAsync(HttpClient client, string email, string password)
    {
        var meResponse = await client.GetAsync("/api/auth/me");
        ApplyAntiforgeryFromResponse(client, meResponse);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed for '{email}' with {(int)loginResponse.StatusCode}: {body}");
        }

        var refreshResponse = await client.GetAsync("/api/auth/me");
        ApplyAntiforgeryFromResponse(client, refreshResponse);
        return true;
    }

    public static void ApplyAntiforgeryFromResponse(HttpClient client, HttpResponseMessage response)
    {
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var token = ExtractXsrfToken(response);
        if (token is not null)
        {
            client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        }
    }

    private static string? ExtractXsrfToken(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return null;
        }

        const string prefix = "XSRF-TOKEN=";
        foreach (var cookie in setCookies)
        {
            var segment = cookie.Split(';', 2)[0];
            if (segment.StartsWith(prefix, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(segment[prefix.Length..]);
            }
        }

        return null;
    }
}
