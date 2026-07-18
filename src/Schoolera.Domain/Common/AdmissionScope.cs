namespace Schoolera.Domain.Common;

/// <summary>
/// Shared deterministic school-catalog scope scoring used by Admission Requirements,
/// Dynamic Questions, and Interview/Assessment Policies.
/// Higher score = more specific. Score 0 = unsupported/incomplete combination (invalid for publish).
/// </summary>
public static class AdmissionScope
{
    /// <summary>
    /// 7 Branch+Grade+Year, 6 Branch+Stage+Year, 5 Grade+Year, 4 Stage+Year,
    /// 3 Branch+Year, 2 Year only, 1 School default.
    /// </summary>
    public static int ComputeSpecificityScore(
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId)
    {
        var hasBranch = branchId.HasValue;
        var hasStage = stageId.HasValue;
        var hasGrade = gradeId.HasValue;
        var hasYear = academicYearId.HasValue;

        if (hasBranch && hasGrade && hasYear)
        {
            return 7;
        }

        if (hasBranch && hasStage && hasYear && !hasGrade)
        {
            return 6;
        }

        if (!hasBranch && hasGrade && hasYear)
        {
            return 5;
        }

        if (!hasBranch && hasStage && hasYear && !hasGrade)
        {
            return 4;
        }

        if (hasBranch && !hasStage && !hasGrade && hasYear)
        {
            return 3;
        }

        if (!hasBranch && !hasStage && !hasGrade && hasYear)
        {
            return 2;
        }

        if (!hasBranch && !hasStage && !hasGrade && !hasYear)
        {
            return 1;
        }

        return 0;
    }

    public static string BuildScopeKey(
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? academicYearId) =>
        $"{Format(branchId)}|{Format(stageId)}|{Format(gradeId)}|{Format(academicYearId)}";

    public static bool MatchesScope(
        Guid? definitionBranchId,
        Guid? definitionStageId,
        Guid? definitionGradeId,
        Guid? definitionAcademicYearId,
        Guid branchId,
        Guid stageId,
        Guid gradeId,
        Guid academicYearId)
    {
        if (definitionBranchId is { } branch && branch != branchId)
        {
            return false;
        }

        if (definitionStageId is { } stage && stage != stageId)
        {
            return false;
        }

        if (definitionGradeId is { } grade && grade != gradeId)
        {
            return false;
        }

        if (definitionAcademicYearId is { } year && year != academicYearId)
        {
            return false;
        }

        return ComputeSpecificityScore(
            definitionBranchId,
            definitionStageId,
            definitionGradeId,
            definitionAcademicYearId) > 0;
    }

    private static string Format(Guid? id) => id?.ToString("N") ?? "-";
}
