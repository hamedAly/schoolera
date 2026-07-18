using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolAdditionalService;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolBranch;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolFeeInstallmentDisplay;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Commands.ActivateSchoolStageOffering;
using Schoolera.Application.SchoolPortal.Commands.ActivateTuitionFee;
using Schoolera.Application.SchoolPortal.Commands.AddSchoolAdmin;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdditionalService;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolBranch;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolFeeInstallmentDisplay;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Commands.CreateSchoolStageOffering;
using Schoolera.Application.SchoolPortal.Commands.CreateTuitionFee;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdditionalService;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolBranch;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolFeeInstallmentDisplay;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolStageOffering;
using Schoolera.Application.SchoolPortal.Commands.DeactivateTuitionFee;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolFeeInstallmentDisplay;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Commands.PublishSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Commands.PublishTuitionFee;
using Schoolera.Application.SchoolPortal.Commands.RemoveSchoolAdmin;
using Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdditionalServices;
using Schoolera.Application.SchoolPortal.Commands.ReplaceSchoolFacilities;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolFeeInstallmentDisplay;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Commands.UnpublishTuitionFee;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdditionalService;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolBranch;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFeeInstallmentDisplay;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFeeVisibility;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolPortalProfile;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolStageOffering;
using Schoolera.Application.SchoolPortal.Commands.UpdateTuitionFee;
using Schoolera.Application.SchoolPortal.Commands.TransferSchoolOwnership;
using Schoolera.Application.SchoolPortal.Commands.UpdateSchoolTeamMember;
using Schoolera.Application.SchoolPortal.Commands.UpsertSchoolTeamMember;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolBranch;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolDashboard;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolFacilities;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolFinancialNote;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolPortalProfile;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolPublishedDiscount;
using Schoolera.Application.SchoolPortal.Queries.GetSchoolTeam;
using Schoolera.Application.SchoolPortal.Queries.GetTuitionFee;
using Schoolera.Application.SchoolPortal.Queries.ListAccessibleSchools;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolAdditionalServices;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolBranches;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolFinancialNotes;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolPublishedDiscounts;
using Schoolera.Application.SchoolPortal.Queries.ListSchoolStageOfferings;
using Schoolera.Application.SchoolPortal.Queries.ListTuitionFees;

namespace Schoolera.Api.Controllers;

[Route("api/school-portal")]
[Authorize(Policy = SchooleraPolicies.SchoolPortal)]
public sealed class SchoolPortalController(
    ISender mediator,
    ILogger<SchoolPortalController> logger) : ApiControllerBase(mediator, logger)
{
    [HttpGet("schools")]
    public async Task<ActionResult<Result<IReadOnlyList<AccessibleSchoolDto>>>> ListSchools(
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new ListAccessibleSchoolsQuery(), cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/dashboard")]
    public async Task<ActionResult<Result<SchoolDashboardDto>>> GetDashboard(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolDashboardQuery(schoolId), cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/profile")]
    public async Task<ActionResult<Result<SchoolPortalProfileDto>>> GetProfile(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolPortalProfileQuery(schoolId), cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/profile")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPortalProfileDto>>> UpdateProfile(
        Guid schoolId,
        [FromBody] UpdateSchoolPortalProfileRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolPortalProfileCommand(schoolId, body),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/branches")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolBranchDto>>>> ListBranches(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new ListSchoolBranchesQuery(schoolId), cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/branches/{branchId:guid}")]
    public async Task<ActionResult<Result<SchoolBranchDto>>> GetBranch(
        Guid schoolId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolBranchQuery(schoolId, branchId), cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/branches")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolBranchDto>>> CreateBranch(
        Guid schoolId,
        [FromBody] CreateSchoolBranchRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolBranchCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/branches/{branchId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolBranchDto>>> UpdateBranch(
        Guid schoolId,
        Guid branchId,
        [FromBody] UpdateSchoolBranchRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolBranchCommand(schoolId, branchId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/branches/{branchId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolBranchDto>>> DeactivateBranch(
        Guid schoolId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolBranchCommand(schoolId, branchId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/branches/{branchId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolBranchDto>>> ActivateBranch(
        Guid schoolId,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolBranchCommand(schoolId, branchId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/offerings")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolStageOfferingDto>>>> ListOfferings(
        Guid schoolId,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ListSchoolStageOfferingsQuery(schoolId, branchId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/offerings")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolStageOfferingDto>>> CreateOffering(
        Guid schoolId,
        [FromBody] CreateSchoolStageOfferingRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolStageOfferingCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/offerings/{offeringId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolStageOfferingDto>>> UpdateOffering(
        Guid schoolId,
        Guid offeringId,
        [FromBody] UpdateSchoolStageOfferingRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolStageOfferingCommand(schoolId, offeringId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/offerings/{offeringId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolStageOfferingDto>>> DeactivateOffering(
        Guid schoolId,
        Guid offeringId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolStageOfferingCommand(schoolId, offeringId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/offerings/{offeringId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolStageOfferingDto>>> ActivateOffering(
        Guid schoolId,
        Guid offeringId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolStageOfferingCommand(schoolId, offeringId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/tuition-fees")]
    public async Task<ActionResult<Result<IReadOnlyList<TuitionFeeDto>>>> ListTuitionFees(
        Guid schoolId,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ListTuitionFeesQuery(schoolId, branchId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/tuition-fees/{feeId:guid}")]
    public async Task<ActionResult<Result<TuitionFeeDto>>> GetTuitionFee(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetTuitionFeeQuery(schoolId, feeId), cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<TuitionFeeDto>>> CreateTuitionFee(
        Guid schoolId,
        [FromBody] CreateTuitionFeeRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateTuitionFeeCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/tuition-fees/{feeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<TuitionFeeDto>>> UpdateTuitionFee(
        Guid schoolId,
        Guid feeId,
        [FromBody] UpdateTuitionFeeRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateTuitionFeeCommand(schoolId, feeId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<TuitionFeeDto>>> DeactivateTuitionFee(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateTuitionFeeCommand(schoolId, feeId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<TuitionFeeDto>>> ActivateTuitionFee(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateTuitionFeeCommand(schoolId, feeId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<TuitionFeeDto>>> PublishTuitionFee(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishTuitionFeeCommand(schoolId, feeId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<TuitionFeeDto>>> UnpublishTuitionFee(
        Guid schoolId,
        Guid feeId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishTuitionFeeCommand(schoolId, feeId),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/fee-visibility")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeVisibilityDto>>> UpdateFeeVisibility(
        Guid schoolId,
        [FromBody] UpdateSchoolFeeVisibilityRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolFeeVisibilityCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/installments")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeInstallmentDisplayDto>>> CreateInstallment(
        Guid schoolId,
        Guid feeId,
        [FromBody] CreateSchoolFeeInstallmentDisplayRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolFeeInstallmentDisplayCommand(schoolId, feeId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/installments/{installmentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeInstallmentDisplayDto>>> UpdateInstallment(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        [FromBody] UpdateSchoolFeeInstallmentDisplayRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolFeeInstallmentDisplayCommand(schoolId, feeId, installmentId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/installments/{installmentId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeInstallmentDisplayDto>>> ActivateInstallment(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolFeeInstallmentDisplayCommand(schoolId, feeId, installmentId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/installments/{installmentId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeInstallmentDisplayDto>>> DeactivateInstallment(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolFeeInstallmentDisplayCommand(schoolId, feeId, installmentId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/installments/{installmentId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeInstallmentDisplayDto>>> PublishInstallment(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolFeeInstallmentDisplayCommand(schoolId, feeId, installmentId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/tuition-fees/{feeId:guid}/installments/{installmentId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFeeInstallmentDisplayDto>>> UnpublishInstallment(
        Guid schoolId,
        Guid feeId,
        Guid installmentId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolFeeInstallmentDisplayCommand(schoolId, feeId, installmentId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/published-discounts")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolPublishedDiscountDto>>>> ListPublishedDiscounts(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ListSchoolPublishedDiscountsQuery(schoolId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/published-discounts/{discountId:guid}")]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> GetPublishedDiscount(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolPublishedDiscountQuery(schoolId, discountId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/published-discounts")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> CreatePublishedDiscount(
        Guid schoolId,
        [FromBody] CreateSchoolPublishedDiscountRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolPublishedDiscountCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/published-discounts/{discountId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> UpdatePublishedDiscount(
        Guid schoolId,
        Guid discountId,
        [FromBody] UpdateSchoolPublishedDiscountRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolPublishedDiscountCommand(schoolId, discountId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/published-discounts/{discountId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> ActivatePublishedDiscount(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolPublishedDiscountCommand(schoolId, discountId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/published-discounts/{discountId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> DeactivatePublishedDiscount(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolPublishedDiscountCommand(schoolId, discountId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/published-discounts/{discountId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> PublishPublishedDiscount(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolPublishedDiscountCommand(schoolId, discountId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/published-discounts/{discountId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolPublishedDiscountDto>>> UnpublishPublishedDiscount(
        Guid schoolId,
        Guid discountId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolPublishedDiscountCommand(schoolId, discountId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/financial-notes")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolFinancialNoteDto>>>> ListFinancialNotes(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ListSchoolFinancialNotesQuery(schoolId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/financial-notes/{noteId:guid}")]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> GetFinancialNote(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new GetSchoolFinancialNoteQuery(schoolId, noteId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/financial-notes")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> CreateFinancialNote(
        Guid schoolId,
        [FromBody] CreateSchoolFinancialNoteRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolFinancialNoteCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/financial-notes/{noteId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> UpdateFinancialNote(
        Guid schoolId,
        Guid noteId,
        [FromBody] UpdateSchoolFinancialNoteRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolFinancialNoteCommand(schoolId, noteId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/financial-notes/{noteId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> ActivateFinancialNote(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolFinancialNoteCommand(schoolId, noteId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/financial-notes/{noteId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> DeactivateFinancialNote(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolFinancialNoteCommand(schoolId, noteId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/financial-notes/{noteId:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> PublishFinancialNote(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new PublishSchoolFinancialNoteCommand(schoolId, noteId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/financial-notes/{noteId:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolFinancialNoteDto>>> UnpublishFinancialNote(
        Guid schoolId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UnpublishSchoolFinancialNoteCommand(schoolId, noteId),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/facilities")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolFacilityListItemDto>>>> GetFacilities(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolFacilitiesQuery(schoolId), cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/facilities")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolFacilityListItemDto>>>> ReplaceFacilities(
        Guid schoolId,
        [FromBody] ReplaceSchoolFacilitiesRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ReplaceSchoolFacilitiesCommand(schoolId, body),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/services")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolAdditionalServiceDto>>>> ListServices(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ListSchoolAdditionalServicesQuery(schoolId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/services")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdditionalServiceDto>>> CreateService(
        Guid schoolId,
        [FromBody] CreateSchoolAdditionalServiceRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new CreateSchoolAdditionalServiceCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/services/{serviceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdditionalServiceDto>>> UpdateService(
        Guid schoolId,
        Guid serviceId,
        [FromBody] UpdateSchoolAdditionalServiceRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolAdditionalServiceCommand(schoolId, serviceId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/services/{serviceId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdditionalServiceDto>>> DeactivateService(
        Guid schoolId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new DeactivateSchoolAdditionalServiceCommand(schoolId, serviceId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/services/{serviceId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolAdditionalServiceDto>>> ActivateService(
        Guid schoolId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ActivateSchoolAdditionalServiceCommand(schoolId, serviceId),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/services/order")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolAdditionalServiceDto>>>> ReorderServices(
        Guid schoolId,
        [FromBody] ReorderSchoolAdditionalServicesRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new ReorderSchoolAdditionalServicesCommand(schoolId, body),
            cancellationToken));
    }

    [HttpGet("schools/{schoolId:guid}/team")]
    public async Task<ActionResult<Result<IReadOnlyList<SchoolTeamMemberDto>>>> GetTeam(
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(new GetSchoolTeamQuery(schoolId), cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/team/school-admins")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolTeamMemberDto>>> AddSchoolAdmin(
        Guid schoolId,
        [FromBody] AddSchoolAdminRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new AddSchoolAdminCommand(schoolId, body),
            cancellationToken));
    }

    [HttpDelete("schools/{schoolId:guid}/team/school-admins/{membershipId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolTeamMemberDto>>> RemoveSchoolAdmin(
        Guid schoolId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new RemoveSchoolAdminCommand(schoolId, membershipId),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/team/members")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolTeamMemberDto>>> UpsertTeamMember(
        Guid schoolId,
        [FromBody] UpsertSchoolTeamMemberRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpsertSchoolTeamMemberCommand(schoolId, body),
            cancellationToken));
    }

    [HttpPut("schools/{schoolId:guid}/team/members/{membershipId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolTeamMemberDto>>> UpdateTeamMember(
        Guid schoolId,
        Guid membershipId,
        [FromBody] UpdateSchoolTeamMemberRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new UpdateSchoolTeamMemberCommand(schoolId, membershipId, body),
            cancellationToken));
    }

    [HttpPost("schools/{schoolId:guid}/team/transfer-ownership")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<SchoolTeamMemberDto>>> TransferOwnership(
        Guid schoolId,
        [FromBody] TransferSchoolOwnershipRequest body,
        CancellationToken cancellationToken)
    {
        return FromResult(await Mediator.Send(
            new TransferSchoolOwnershipCommand(schoolId, body),
            cancellationToken));
    }
}
