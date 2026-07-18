using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

public sealed class SchoolAdmissionQuestionRepository(SchooleraDbContext dbContext)
    : ISchoolAdmissionQuestionRepository
{
    public Task<SchoolAdmissionQuestion?> GetByIdAsync(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionQuestions
            .AsNoTracking()
            .Include(question => question.Options)
            .FirstOrDefaultAsync(
                question => question.SchoolId == schoolId && question.Id == questionId,
                cancellationToken);

    public Task<SchoolAdmissionQuestion?> GetByIdForUpdateAsync(
        Guid schoolId,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionQuestions
            .Include(question => question.Options)
            .FirstOrDefaultAsync(
                question => question.SchoolId == schoolId && question.Id == questionId,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolAdmissionQuestion>> ListAsync(
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId,
        AdmissionQuestionType? questionType,
        AdmissionQuestionPublicationStatus? publicationStatus,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SchoolAdmissionQuestions
            .AsNoTracking()
            .Include(question => question.Options)
            .Where(question => question.SchoolId == schoolId);

        if (branchId is { } branch)
        {
            query = query.Where(question =>
                question.SchoolBranchId == null || question.SchoolBranchId == branch);
        }

        if (stageId is { } stage)
        {
            query = query.Where(question =>
                question.EducationalStageId == null || question.EducationalStageId == stage);
        }

        if (gradeId is { } grade)
        {
            query = query.Where(question => question.GradeId == null || question.GradeId == grade);
        }

        if (academicYearId is { } year)
        {
            query = query.Where(question =>
                question.AcademicYearId == null || question.AcademicYearId == year);
        }

        if (questionType is { } type)
        {
            query = query.Where(question => question.QuestionType == type);
        }

        if (publicationStatus is { } status)
        {
            query = query.Where(question => question.PublicationStatus == status);
        }

        if (isActive is { } active)
        {
            query = query.Where(question => question.IsActive == active);
        }

        return await query
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.QuestionCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolAdmissionQuestion>> ListPublishedActiveAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SchoolAdmissionQuestions
            .AsNoTracking()
            .Include(question => question.Options)
            .Where(question =>
                question.SchoolId == schoolId &&
                question.IsActive &&
                question.PublicationStatus == AdmissionQuestionPublicationStatus.Published)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.QuestionCode)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPublishedConflictAsync(
        Guid schoolId,
        string questionCode,
        string scopeKey,
        Guid? excludeQuestionId,
        CancellationToken cancellationToken = default)
    {
        var code = questionCode.Trim().ToLowerInvariant();
        return dbContext.SchoolAdmissionQuestions.AnyAsync(
            question =>
                question.SchoolId == schoolId &&
                question.QuestionCode == code &&
                question.ScopeKey == scopeKey &&
                question.IsActive &&
                question.PublicationStatus == AdmissionQuestionPublicationStatus.Published &&
                (excludeQuestionId == null || question.Id != excludeQuestionId),
            cancellationToken);
    }

    public Task AddAsync(SchoolAdmissionQuestion question, CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionQuestions.AddAsync(question, cancellationToken).AsTask();

    public Task AddAuditAsync(SchoolAdmissionQuestionAudit audit, CancellationToken cancellationToken = default) =>
        dbContext.SchoolAdmissionQuestionAudits.AddAsync(audit, cancellationToken).AsTask();

    public Task<bool> HasApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationQuestionSnapshots.AnyAsync(
            snapshot => snapshot.AdmissionApplicationId == admissionApplicationId,
            cancellationToken);

    public Task<bool> HasApplicationAnswersAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAnswers.AnyAsync(
            answer => answer.AdmissionApplicationId == admissionApplicationId,
            cancellationToken);

    public async Task<IReadOnlyList<AdmissionApplicationQuestionSnapshot>> ListApplicationSnapshotsAsync(
        Guid admissionApplicationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.AdmissionApplicationQuestionSnapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Options)
            .Where(snapshot => snapshot.AdmissionApplicationId == admissionApplicationId)
            .OrderBy(snapshot => snapshot.SortOrder)
            .ThenBy(snapshot => snapshot.QuestionCode)
            .ToListAsync(cancellationToken);
}
