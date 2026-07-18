using Schoolera.Application.Parent.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Common;

internal static class ChildDocumentMapping
{
    public static ChildDocumentDto ToDto(ChildDocument document) =>
        new(
            document.Id,
            document.DocumentType,
            document.OriginalFileName,
            document.ContentType,
            document.FileSizeBytes,
            document.CreatedAtUtc,
            document.UpdatedAtUtc);

    public static string SafeOriginalFileName(string? originalFileName)
    {
        var name = Path.GetFileName(originalFileName ?? string.Empty);
        return string.IsNullOrWhiteSpace(name) ? "document" : name;
    }

    public static AdmissionRequiredDocumentCode? ToAdmissionRequiredDocumentCode(ChildDocumentType documentType) =>
        documentType switch
        {
            ChildDocumentType.BirthCertificate => AdmissionRequiredDocumentCode.BirthCertificate,
            ChildDocumentType.ChildPhoto => AdmissionRequiredDocumentCode.ChildPhoto,
            ChildDocumentType.PreviousSchoolCertificate => AdmissionRequiredDocumentCode.PreviousSchoolCertificate,
            ChildDocumentType.MedicalReport => AdmissionRequiredDocumentCode.MedicalReport,
            ChildDocumentType.OtherApproved => AdmissionRequiredDocumentCode.OtherApproved,
            _ => null,
        };

    public static AdmissionAttachmentType ToAdmissionAttachmentType(ChildDocumentType documentType) =>
        documentType switch
        {
            ChildDocumentType.BirthCertificate => AdmissionAttachmentType.BirthCertificate,
            ChildDocumentType.PreviousSchoolCertificate => AdmissionAttachmentType.PreviousSchoolReport,
            ChildDocumentType.ChildPhoto => AdmissionAttachmentType.SupportingDocument,
            ChildDocumentType.MedicalReport => AdmissionAttachmentType.SupportingDocument,
            ChildDocumentType.OtherApproved => AdmissionAttachmentType.Other,
            _ => AdmissionAttachmentType.Other,
        };
}
