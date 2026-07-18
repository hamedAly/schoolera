using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Commands.ActivateFaqItem;
using Schoolera.Application.Cms.Commands.CreateFaqCategory;
using Schoolera.Application.Cms.Commands.CreateFaqItem;
using Schoolera.Application.Cms.Commands.DeactivateFaqItem;
using Schoolera.Application.Cms.Commands.PublishFaqCategory;
using Schoolera.Application.Cms.Commands.PublishFaqItem;
using Schoolera.Application.Cms.Commands.ReorderFaqCategories;
using Schoolera.Application.Cms.Commands.ReorderFaqItems;
using Schoolera.Application.Cms.Commands.UnpublishFaqCategory;
using Schoolera.Application.Cms.Commands.UnpublishFaqItem;
using Schoolera.Application.Cms.Commands.UpdateFaqCategory;
using Schoolera.Application.Cms.Commands.UpdateFaqItem;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Cms.Queries.ListFaqCategories;
using Schoolera.Application.Cms.Queries.ListInterviewFaqItems;
using Schoolera.Application.Common.Models;

namespace Schoolera.Api.Controllers;

[Route("api/admin/cms/faq")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminCmsFaqController(
    ISender mediator,
    ILogger<AdminCmsFaqController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("categories")]
    public Task<ActionResult<Result<IReadOnlyList<FaqCategoryAdminDto>>>> ListCategories(
        CancellationToken cancellationToken) =>
        SendAsync(new ListFaqCategoriesQuery(), cancellationToken);

    [HttpGet("interview-items")]
    public Task<ActionResult<Result<IReadOnlyList<FaqItemAdminDto>>>> ListInterviewItems(
        [FromQuery] int? category,
        [FromQuery] bool? isPublished,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        SendAsync(ListInterviewFaqItemsQuery.FromFilters(
            category, isPublished, isActive, search), cancellationToken);

    [HttpPost("categories")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqCategoryAdminDto>>> CreateCategory(
        [FromBody] CreateFaqCategoryCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("categories/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqCategoryAdminDto>>> UpdateCategory(
        Guid id,
        [FromBody] UpdateFaqCategoryBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            new UpdateFaqCategoryCommand(id, body.NameAr, body.NameEn, body.Slug),
            cancellationToken);

    [HttpPost("categories/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqCategoryAdminDto>>> PublishCategory(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new PublishFaqCategoryCommand(id), cancellationToken);

    [HttpPost("categories/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqCategoryAdminDto>>> UnpublishCategory(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new UnpublishFaqCategoryCommand(id), cancellationToken);

    [HttpPost("categories/reorder")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<IReadOnlyList<FaqCategoryAdminDto>>>> ReorderCategories(
        [FromBody] ReorderFaqCategoriesCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPost("items")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqItemAdminDto>>> CreateItem(
        [FromBody] CreateFaqItemCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    [HttpPut("items/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqItemAdminDto>>> UpdateItem(
        Guid id,
        [FromBody] UpdateFaqItemBody body,
        CancellationToken cancellationToken) =>
        SendAsync(
            UpdateFaqItemCommand.FromBody(
                id,
                body.CategoryId,
                body.QuestionAr,
                body.QuestionEn,
                body.AnswerAr,
                body.AnswerEn,
                body.InterviewCategory,
                body.RowVersion),
            cancellationToken);

    [HttpPost("items/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqItemAdminDto>>> PublishItem(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new PublishFaqItemCommand(id), cancellationToken);

    [HttpPost("items/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqItemAdminDto>>> UnpublishItem(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new UnpublishFaqItemCommand(id), cancellationToken);

    [HttpPost("items/{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqItemAdminDto>>> ActivateItem(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new ActivateFaqItemCommand(id), cancellationToken);

    [HttpPost("items/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<FaqItemAdminDto>>> DeactivateItem(
        Guid id,
        CancellationToken cancellationToken) =>
        SendAsync(new DeactivateFaqItemCommand(id), cancellationToken);

    [HttpPost("items/reorder")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<IReadOnlyList<FaqItemAdminDto>>>> ReorderItems(
        [FromBody] ReorderFaqItemsCommand command,
        CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));

    public sealed record UpdateFaqCategoryBody(string NameAr, string NameEn, string Slug);

    public sealed record UpdateFaqItemBody(
        Guid CategoryId,
        string QuestionAr,
        string QuestionEn,
        string AnswerAr,
        string AnswerEn,
        int? InterviewCategory = null,
        byte[]? RowVersion = null);
}
