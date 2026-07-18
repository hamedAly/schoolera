using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Cms.Commands.SubmitContactRequest;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Cms.Queries.GetPublishedCmsPageBySlug;
using Schoolera.Application.Cms.Queries.GetPublishedFaqs;
using Schoolera.Application.Cms.Queries.GetPublishedHomepage;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/content")]
public sealed class ContentController(
    ISender mediator,
    ILogger<ContentController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("pages/{slug}")]
    public Task<ActionResult<Result<PublicCmsPageDto>>> GetPage(
        string slug,
        CancellationToken cancellationToken) =>
        SendAsync(new GetPublishedCmsPageBySlugQuery(slug), cancellationToken);

    [HttpGet("faqs")]
    public Task<ActionResult<Result<IReadOnlyList<PublicFaqCategoryDto>>>> GetFaqs(
        CancellationToken cancellationToken) =>
        SendAsync(new GetPublishedFaqsQuery(), cancellationToken);

    [HttpGet("home")]
    public Task<ActionResult<Result<PublicHomepageDto>>> GetHome(
        CancellationToken cancellationToken) =>
        SendAsync(new GetPublishedHomepageQuery(), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
