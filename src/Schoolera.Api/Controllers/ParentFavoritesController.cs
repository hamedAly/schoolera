using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Favorites.Commands.AddFavoriteSchool;
using Schoolera.Application.Favorites.Commands.RemoveFavoriteSchool;
using Schoolera.Application.Favorites.Dtos;
using Schoolera.Application.Favorites.Queries.ListFavoriteSchools;

namespace Schoolera.Api.Controllers;

[Route("api/parent/favorites")]
[Authorize(Policy = SchooleraPolicies.ParentOnly)]
public sealed class ParentFavoritesController(
    ISender mediator,
    ILogger<ParentFavoritesController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<PagedResult<FavoriteSchoolListItemDto>>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendAsync(new ListFavoriteSchoolsQuery(pageNumber, pageSize), cancellationToken);

    [HttpPost("{schoolId:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<bool>>> Add(
        Guid schoolId,
        CancellationToken cancellationToken) =>
        SendAsync(new AddFavoriteSchoolCommand(schoolId), cancellationToken);

    [HttpDelete("{schoolId:guid}")]
    [ValidateAntiForgeryToken]
    public Task<ActionResult<Result<bool>>> Remove(
        Guid schoolId,
        CancellationToken cancellationToken) =>
        SendAsync(new RemoveFavoriteSchoolCommand(schoolId), cancellationToken);

    private async Task<ActionResult<Result<T>>> SendAsync<T>(
        IRequest<Result<T>> request,
        CancellationToken cancellationToken) =>
        FromResult(await Mediator.Send(request, cancellationToken));
}
