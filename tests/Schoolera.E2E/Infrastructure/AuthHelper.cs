using Microsoft.Playwright;

namespace Schoolera.E2E.Infrastructure;

public static class AuthHelper
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task EnsureStorageStatesAsync()
    {
        await Gate.WaitAsync();
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !E2EConfig.Headed,
            });

            foreach (var role in SeedUsers.All)
            {
                var path = SeedUsers.StorageStatePath(role);
                // Always regenerate storage states for deterministic E2E runs.
                // Old cookies/session state can become invalid after server restarts or seed changes.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                await CreateStorageStateAsync(browser, role, path);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task CreateStorageStateAsync(IBrowser browser, SeedRole role, string? path = null)
    {
        path ??= SeedUsers.StorageStatePath(role);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = E2EConfig.BaseUrl.TrimEnd('/') + "/",
            Locale = "ar-EG",
        });

        try
        {
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(E2EConfig.DefaultTimeoutMs);
            await LoginViaUiAsync(page, role);
            await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path });
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    public static async Task LoginViaUiAsync(IPage page, SeedRole role)
    {
        await page.GotoAsync("/auth/login", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = E2EConfig.NavigationTimeoutMs,
        });

        await page.Locator("#email").FillAsync(SeedUsers.Email(role));
        await page.Locator("#password").FillAsync(E2EConfig.SeedPassword);
        await page.Locator("form.auth-form button[type='submit']").ClickAsync();

        var prefix = SeedUsers.ExpectedPathPrefix(role);
        await page.WaitForURLAsync(
            url =>
            {
                try
                {
                    var path = new Uri(url).AbsolutePath;
                    return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            },
            new PageWaitForURLOptions { Timeout = E2EConfig.NavigationTimeoutMs });
    }

    private static string? TryExtractSchoolIdFromUrl(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath;
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2
                && parts[0].Equals("school", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(parts[1], out _))
            {
                return parts[1];
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public static async Task<string> ResolveSchoolIdAsync(IPage page)
    {
        await page.GotoAsync("/school", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = E2EConfig.NavigationTimeoutMs,
        });

        var direct = TryExtractSchoolIdFromUrl(page.Url);
        if (direct is not null)
            return direct;

        // Some flows land on an "entry" page at /school (no :schoolId in the URL yet).
        // Try selecting the first school link that points to a portal route.
        var overviewLink = page.Locator("a[href^='/school/'][href*='/overview']").First;
        if (await overviewLink.CountAsync() > 0)
        {
            await overviewLink.ClickAsync();
        }
        else
        {
            var anySchoolLink = page.Locator("a[href^='/school/']").First;
            if (await anySchoolLink.CountAsync() > 0)
            {
                await anySchoolLink.ClickAsync();
            }
        }

        await page.WaitForURLAsync(
            url => TryExtractSchoolIdFromUrl(url) is not null,
            new PageWaitForURLOptions { Timeout = E2EConfig.NavigationTimeoutMs });

        var resolved = TryExtractSchoolIdFromUrl(page.Url);
        if (resolved is not null)
            return resolved;

        throw new InvalidOperationException($"Unable to resolve schoolId from /school route. Final URL: {page.Url}");
    }
}
