using MediatR;
using Microsoft.AspNetCore.Mvc;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Integrations.Constants;
using Schoolera.Application.Favorites.Constants;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.Payments.Constants;

namespace Schoolera.Api.Controllers;

public abstract class ApiControllerBase(ISender mediator, ILogger logger) : ControllerBase
{
    protected ISender Mediator { get; } = mediator;

    protected ILogger Logger { get; } = logger;

    protected static Result<T> Success<T>(T data)
    {
        return Result<T>.Success(data);
    }

    protected static Result<T> Failure<T>(string error)
    {
        return Result<T>.Failure(new[] { error });
    }

    protected static Result<T> Failure<T>(IEnumerable<string> errors)
    {
        return Result<T>.Failure(errors);
    }

    protected ActionResult<Result<T>> FromResult<T>(Result<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return StatusCode(ResolveStatusCode(result.ErrorCodes), result);
    }

    private static int ResolveStatusCode(IReadOnlyCollection<string> errorCodes)
    {
        if (errorCodes.Contains(AuthErrorCodes.Unauthorized) ||
            errorCodes.Contains(ErrorCodes.Unauthorized))
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (errorCodes.Contains(AuthErrorCodes.Forbidden) ||
            errorCodes.Contains(ErrorCodes.Forbidden) ||
            errorCodes.Contains(OnboardingErrorCodes.Forbidden) ||
            errorCodes.Contains(OnboardingErrorCodes.OwnerRoleRequired) ||
            errorCodes.Contains(SchoolPortalErrorCodes.AccessDenied) ||
            errorCodes.Contains(SchoolPortalErrorCodes.OwnerRequired) ||
            errorCodes.Contains(SchoolPortalErrorCodes.CannotModifyOwner) ||
            errorCodes.Contains(SchoolPortalErrorCodes.CannotChangeStatus) ||
            errorCodes.Contains(SchoolPortalErrorCodes.BranchOutOfScope) ||
            errorCodes.Contains(SchoolPortalErrorCodes.BranchScopeDenied) ||
            errorCodes.Contains(AdminErrorCodes.Forbidden) ||
            errorCodes.Contains(AdminErrorCodes.CannotModifySelf) ||
            errorCodes.Contains(ParentErrorCodes.Forbidden) ||
            errorCodes.Contains(ParentErrorCodes.OwnershipDenied) ||
            errorCodes.Contains(AdmissionErrorCodes.Forbidden) ||
            errorCodes.Contains(AdmissionErrorCodes.ReviewSchoolAccessDenied) ||
            errorCodes.Contains(CmsErrorCodes.Forbidden) ||
            errorCodes.Contains(IntegrationErrorCodes.Forbidden) ||
            errorCodes.Contains(FavoriteErrorCodes.Forbidden) ||
            errorCodes.Contains(SupportTicketErrorCodes.Forbidden) ||
            errorCodes.Contains(PaymentErrorCodes.Forbidden))
        {
            return StatusCodes.Status403Forbidden;
        }

        if (errorCodes.Contains(AuthErrorCodes.RateLimited) ||
            errorCodes.Contains(ContactErrorCodes.RateLimited))
        {
            return StatusCodes.Status429TooManyRequests;
        }

        if (errorCodes.Contains(ErrorCodes.NotFound) ||
            errorCodes.Contains(SchoolErrorCodes.NotFound) ||
            errorCodes.Contains(TaxonomyErrorCodes.NotFound) ||
            errorCodes.Contains(OnboardingErrorCodes.NotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.SchoolNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.BranchNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.OfferingNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.FeeNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.ServiceNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.ImageNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.TeamMemberNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.FacilityNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.AdmissionRequirementNotFound) ||
            errorCodes.Contains(SchoolPortalErrorCodes.AdmissionQuestionNotFound) ||
            errorCodes.Contains(AdminErrorCodes.SchoolNotFound) ||
            errorCodes.Contains(AdminErrorCodes.UserNotFound) ||
            errorCodes.Contains(ParentErrorCodes.ProfileNotFound) ||
            errorCodes.Contains(ParentErrorCodes.ChildNotFound) ||
            errorCodes.Contains(ParentErrorCodes.DocumentNotFound) ||
            errorCodes.Contains(ParentErrorCodes.NotificationNotFound) ||
            errorCodes.Contains(ParentErrorCodes.SubscriptionNotFound) ||
            errorCodes.Contains(ParentErrorCodes.SchoolNotFound) ||
            errorCodes.Contains(AdmissionErrorCodes.NotFound) ||
            errorCodes.Contains(AdmissionErrorCodes.ReviewNotFound) ||
            errorCodes.Contains(AdmissionErrorCodes.AttachmentNotFound) ||
            errorCodes.Contains(AdmissionErrorCodes.RequirementSnapshotNotFound) ||
            errorCodes.Contains(AdmissionErrorCodes.QuestionSnapshotNotFound) ||
            errorCodes.Contains(AdmissionErrorCodes.StudentNotOwned) ||
            errorCodes.Contains(CmsErrorCodes.PageNotFound) ||
            errorCodes.Contains(CmsErrorCodes.FaqNotFound) ||
            errorCodes.Contains(CmsErrorCodes.HomeNotFound) ||
            errorCodes.Contains(ContactErrorCodes.NotFound) ||
            errorCodes.Contains(IntegrationErrorCodes.NotFound) ||
            errorCodes.Contains(IntegrationErrorCodes.TemplateNotFound) ||
            errorCodes.Contains(IntegrationErrorCodes.TemplateVersionNotFound) ||
            errorCodes.Contains(FavoriteErrorCodes.NotFound) ||
            errorCodes.Contains(FavoriteErrorCodes.SchoolNotFound) ||
            errorCodes.Contains(SupportTicketErrorCodes.NotFound) ||
            errorCodes.Contains(SupportTicketErrorCodes.AttachmentNotFound) ||
            errorCodes.Contains(SupportTicketErrorCodes.ContactNotFound) ||
            errorCodes.Contains(SupportTicketErrorCodes.AdmissionNotFound) ||
            errorCodes.Contains(SupportTicketErrorCodes.AgentNotFound) ||
            errorCodes.Contains(PaymentErrorCodes.NotFound) ||
            errorCodes.Contains(PaymentErrorCodes.PayableNotFound) ||
            errorCodes.Contains(PaymentErrorCodes.ReceiptNotFound) ||
            errorCodes.Contains(PaymentErrorCodes.FinancingNotFound) ||
            errorCodes.Contains(PaymentErrorCodes.ReconciliationNotFound))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errorCodes.Contains(OnboardingErrorCodes.ConcurrentUpdate) ||
            errorCodes.Contains(SchoolPortalErrorCodes.ConcurrentUpdate) ||
            errorCodes.Contains(AdmissionErrorCodes.ConcurrentUpdate) ||
            errorCodes.Contains(AdmissionErrorCodes.ReviewConcurrentUpdate) ||
            errorCodes.Contains(CmsErrorCodes.PageConcurrentUpdate) ||
            errorCodes.Contains(IntegrationErrorCodes.Concurrency) ||
            errorCodes.Contains(ParentErrorCodes.PreferenceConcurrency) ||
            errorCodes.Contains(ParentErrorCodes.DuplicateSubscription) ||
            errorCodes.Contains(SupportTicketErrorCodes.ConcurrentUpdate) ||
            errorCodes.Contains(PaymentErrorCodes.ConcurrencyConflict) ||
            errorCodes.Contains(PaymentErrorCodes.IdempotencyConflict) ||
            errorCodes.Contains(PaymentErrorCodes.ActiveIntentExists) ||
            errorCodes.Contains(PaymentErrorCodes.AlreadyPaid))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
