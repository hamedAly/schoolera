using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Couriers;
using Schoolera.Application.Couriers.Commands.MutateCourier;
using Schoolera.Application.Couriers.Queries.GetCourierAdminDetail;
using Schoolera.Application.Couriers.Queries.GetCourierHealthHistory;
using Schoolera.Application.Couriers.Queries.PreviewCourierAvailability;

namespace Schoolera.Api.Controllers;

[Route("api/admin/integrations/{integrationId:guid}/courier")]
[Authorize(Policy = SchooleraPolicies.PlatformAdminOnly)]
public sealed class AdminCourierController(
    ISender mediator,
    ILogger<AdminCourierController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> Get(Guid integrationId, CancellationToken ct) =>
        Send(new GetCourierAdminDetailQuery(integrationId), ct);
    [HttpPut("profile"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> Profile(Guid integrationId, UpdateCourierProfileRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.UpdateProfile, Profile: body), ct);
    [HttpPost("services"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> CreateService(Guid integrationId, UpsertCourierServiceRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.CreateService, Service: body), ct);
    [HttpPut("services/{id:guid}"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> UpdateService(Guid integrationId, Guid id, UpsertCourierServiceRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.UpdateService, id, Service: body), ct);
    [HttpPatch("services/{id:guid}/active"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> ServiceActive(Guid integrationId, Guid id, SetCourierChildActiveRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.SetServiceActive, id, Active: body), ct);
    [HttpPost("coverage"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> CreateCoverage(Guid integrationId, UpsertCourierCoverageRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.CreateCoverage, Coverage: body), ct);
    [HttpPut("coverage/{id:guid}"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> UpdateCoverage(Guid integrationId, Guid id, UpsertCourierCoverageRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.UpdateCoverage, id, Coverage: body), ct);
    [HttpPatch("coverage/{id:guid}/active"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> CoverageActive(Guid integrationId, Guid id, SetCourierChildActiveRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.SetCoverageActive, id, Active: body), ct);
    [HttpPost("windows"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> CreateWindow(Guid integrationId, UpsertCourierWindowRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.CreateWindow, Window: body), ct);
    [HttpPut("windows/{id:guid}"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> UpdateWindow(Guid integrationId, Guid id, UpsertCourierWindowRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.UpdateWindow, id, Window: body), ct);
    [HttpPatch("windows/{id:guid}/active"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> WindowActive(Guid integrationId, Guid id, SetCourierChildActiveRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.SetWindowActive, id, Active: body), ct);
    [HttpPost("slas"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> CreateSla(Guid integrationId, UpsertCourierSlaRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.CreateSla, Sla: body), ct);
    [HttpPut("slas/{id:guid}"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> UpdateSla(Guid integrationId, Guid id, UpsertCourierSlaRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.UpdateSla, id, Sla: body), ct);
    [HttpPatch("slas/{id:guid}/active"), ValidateAntiForgeryToken]
    public Task<ActionResult<Result<CourierAdminDetailDto>>> SlaActive(Guid integrationId, Guid id, SetCourierChildActiveRequest body, CancellationToken ct) =>
        Send(new MutateCourierCommand(integrationId, CourierMutationKind.SetSlaActive, id, Active: body), ct);
    [HttpGet("availability-preview")]
    public Task<ActionResult<Result<IReadOnlyList<CourierAvailabilityOptionDto>>>> Preview(
        Guid integrationId, Guid countryId, Guid? governorateId, Guid? cityId, Guid? districtId, CancellationToken ct) =>
        Send(new PreviewCourierAvailabilityQuery(integrationId, countryId, governorateId, cityId, districtId), ct);
    [HttpGet("health-history")]
    public Task<ActionResult<Result<IReadOnlyList<CourierHealthDto>>>> Health(Guid integrationId, int take = 50, CancellationToken ct = default) =>
        Send(new GetCourierHealthHistoryQuery(integrationId, take), ct);

    private async Task<ActionResult<Result<T>>> Send<T>(IRequest<Result<T>> request, CancellationToken ct) =>
        FromResult(await Mediator.Send(request, ct));
}
