using System.Globalization;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Schoolera.Api.Middleware;
using Schoolera.Api.Resources;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.Schools.Commands.CreateSchool;

namespace Schoolera.Tests;

public sealed class LocalizationFoundationTests
{
    [Theory]
    [InlineData(null, "ar-EG")]
    [InlineData("ar", "ar")]
    [InlineData("ar-EG", "ar-EG")]
    [InlineData("en", "en")]
    [InlineData("en-US", "en-US")]
    [InlineData("fr-FR", "ar-EG")]
    public async Task RequestLocalization_SelectsExpectedCulture(string? acceptLanguage, string expectedCulture)
    {
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLocalization();
                services.AddRouting();
            })
            .Configure(app =>
            {
                var supported = new[] { "ar-EG", "en-US", "ar", "en" };
                app.UseRequestLocalization(options =>
                {
                    options.SetDefaultCulture("ar-EG")
                        .AddSupportedCultures(supported)
                        .AddSupportedUICultures(supported);
                    options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
                });

                app.Run(async context =>
                {
                    await context.Response.WriteAsync(CultureInfo.CurrentUICulture.Name);
                });
            }));

        var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedCulture, body);
    }

    [Fact]
    public async Task ExceptionMiddleware_IncludesStableUnexpectedErrorCode()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        var json = await ReadJsonAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Unexpected, json.RootElement.GetProperty("errorCodes")[0].GetString());
        Assert.Equal("An unexpected error occurred.", json.RootElement.GetProperty("errors")[0].GetString());
    }

    [Fact]
    public async Task ExceptionMiddleware_IncludesValidationErrorCodes()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException([new ValidationFailure("Name", "Name is required.")]),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context, new StubApiMessagesLocalizer());

        var json = await ReadJsonAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.Validation, json.RootElement.GetProperty("errorCodes")[0].GetString());
    }

    [Fact]
    public void CreateSchoolValidator_UsesLocalizedArabicMessages()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
        var validator = new CreateSchoolCommandValidator(new StubValidationLocalizer());
        var result = validator.Validate(new CreateSchoolCommand(string.Empty, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "اسم المدرسة مطلوب.");
    }

    [Fact]
    public void CreateSchoolValidator_UsesLocalizedEnglishMessages()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var validator = new CreateSchoolCommandValidator(new StubValidationLocalizer());
        var result = validator.Validate(new CreateSchoolCommand(string.Empty, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "School name is required.");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    private sealed class StubApiMessagesLocalizer : IStringLocalizer<ApiMessages>
    {
        public LocalizedString this[string name] =>
            new(name, name == "UnexpectedError" ? "An unexpected error occurred." : name);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            [this["UnexpectedError"]];
    }

    private sealed class StubValidationLocalizer : IStringLocalizer<ValidationMessages>
    {
        public LocalizedString this[string name]
        {
            get
            {
                var arabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
                var value = name switch
                {
                    "SchoolNameRequired" => arabic ? "اسم المدرسة مطلوب." : "School name is required.",
                    "SchoolNameMaxLength" => arabic
                        ? "يجب ألا يتجاوز اسم المدرسة {0} حرفًا."
                        : "School name must be at most {0} characters.",
                    "SchoolCityMaxLength" => arabic
                        ? "يجب ألا تتجاوز المدينة {0} حرفًا."
                        : "City must be at most {0} characters.",
                    _ => name,
                };
                return new LocalizedString(name, value);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var template = this[name].Value;
                return new LocalizedString(name, string.Format(CultureInfo.CurrentUICulture, template, arguments));
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            [this["SchoolNameRequired"]];
    }
}
