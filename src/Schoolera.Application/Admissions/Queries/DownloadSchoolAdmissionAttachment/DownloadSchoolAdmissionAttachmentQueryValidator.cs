using FluentValidation;

namespace Schoolera.Application.Admissions.Queries.DownloadSchoolAdmissionAttachment;

public sealed class DownloadSchoolAdmissionAttachmentQueryValidator
    : AbstractValidator<DownloadSchoolAdmissionAttachmentQuery>
{
    public DownloadSchoolAdmissionAttachmentQueryValidator()
    {
        RuleFor(query => query.SchoolId).NotEmpty();
        RuleFor(query => query.ApplicationId).NotEmpty();
        RuleFor(query => query.AttachmentId).NotEmpty();
    }
}
