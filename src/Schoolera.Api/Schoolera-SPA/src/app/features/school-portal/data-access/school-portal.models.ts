import type { ApiResult } from '../../../core/http/api-result';

export type {
  AccessibleSchoolDto,
  AccessibleSchoolDtoIReadOnlyListResult,
  AddSchoolAdminRequest,
  CreateSchoolAdditionalServiceRequest,
  CreateSchoolBranchRequest,
  CreateSchoolStageOfferingRequest,
  CreateTuitionFeeRequest,
  ReplaceSchoolFacilitiesRequest,
  ReorderSchoolAdditionalServicesRequest,
  ReorderSchoolGalleryImagesRequest,
  SchoolAdditionalServiceDto,
  SchoolAdditionalServiceDtoIReadOnlyListResult,
  SchoolAdditionalServiceDtoResult,
  SchoolBranchDto,
  SchoolBranchDtoIReadOnlyListResult,
  SchoolBranchDtoResult,
  SchoolDashboardDto,
  SchoolDashboardDtoResult,
  SchoolFacilityListItemDto,
  SchoolFacilityListItemDtoIReadOnlyListResult,
  SchoolGalleryImageDto,
  SchoolGalleryImageDtoIReadOnlyListResult,
  SchoolGalleryImageDtoResult,
  SchoolMediaDto,
  SchoolMediaDtoResult,
  SchoolPortalProfileDto,
  SchoolPortalProfileDtoResult,
  SchoolStageOfferingDto,
  SchoolStageOfferingDtoIReadOnlyListResult,
  SchoolStageOfferingDtoResult,
  SchoolTeamMemberDto,
  SchoolTeamMemberDtoIReadOnlyListResult,
  SchoolTeamMemberDtoResult,
  TuitionFeeDto,
  TuitionFeeDtoIReadOnlyListResult,
  TuitionFeeDtoResult,
  UpdateSchoolAdditionalServiceRequest,
  UpdateSchoolBranchRequest,
  UpdateSchoolGalleryImageRequest,
  UpdateSchoolPortalProfileRequest,
  UpdateSchoolStageOfferingRequest,
  UpdateTuitionFeeRequest,
} from '../../../core/api-client/SwaggerClient.service';

export { SchoolStatus } from '../../../core/api-client/SwaggerClient.service';

import type {
  AccessibleSchoolDtoIReadOnlyListResult,
  SchoolAdditionalServiceDtoIReadOnlyListResult,
  SchoolAdditionalServiceDtoResult,
  SchoolBranchDtoIReadOnlyListResult,
  SchoolBranchDtoResult,
  SchoolDashboardDtoResult,
  SchoolFacilityListItemDtoIReadOnlyListResult,
  SchoolGalleryImageDtoIReadOnlyListResult,
  SchoolGalleryImageDtoResult,
  SchoolMediaDtoResult,
  SchoolPortalProfileDtoResult,
  SchoolStageOfferingDtoIReadOnlyListResult,
  SchoolStageOfferingDtoResult,
  SchoolTeamMemberDtoIReadOnlyListResult,
  SchoolTeamMemberDtoResult,
  TuitionFeeDtoIReadOnlyListResult,
  TuitionFeeDtoResult,
} from '../../../core/api-client/SwaggerClient.service';

export type AccessibleSchoolListResult = AccessibleSchoolDtoIReadOnlyListResult;
export type SchoolDashboardResult = SchoolDashboardDtoResult;
export type SchoolPortalProfileResult = SchoolPortalProfileDtoResult;
export type SchoolBranchListResult = SchoolBranchDtoIReadOnlyListResult;
export type SchoolBranchResult = SchoolBranchDtoResult;
export type SchoolStageOfferingListResult = SchoolStageOfferingDtoIReadOnlyListResult;
export type SchoolStageOfferingResult = SchoolStageOfferingDtoResult;
export type TuitionFeeListResult = TuitionFeeDtoIReadOnlyListResult;
export type TuitionFeeResult = TuitionFeeDtoResult;
export type SchoolFacilityListResult = SchoolFacilityListItemDtoIReadOnlyListResult;
export type SchoolMediaResult = SchoolMediaDtoResult;
export type SchoolGalleryImageResult = SchoolGalleryImageDtoResult;
export type SchoolGalleryImageListResult = SchoolGalleryImageDtoIReadOnlyListResult;
export type SchoolAdditionalServiceListResult = SchoolAdditionalServiceDtoIReadOnlyListResult;
export type SchoolAdditionalServiceResult = SchoolAdditionalServiceDtoResult;
export type SchoolTeamListResult = SchoolTeamMemberDtoIReadOnlyListResult;
export type SchoolTeamMemberResult = SchoolTeamMemberDtoResult;

export interface PortalUploadProgress {
  readonly kind: 'progress';
  readonly percent: number;
}

export interface PortalUploadComplete<T> {
  readonly kind: 'complete';
  readonly result: ApiResult<T>;
}

export type PortalUploadEvent<T> = PortalUploadProgress | PortalUploadComplete<T>;

/** UI view model aligned with SchoolAdmissionRequirement* DTOs (NSwag). */
export interface SchoolAdmissionRequirementListItem {
  readonly id?: string;
  readonly requirementCode?: string;
  readonly kind?: number;
  readonly nameAr?: string;
  readonly nameEn?: string;
  readonly isRequired?: boolean;
  readonly sortOrder?: number;
  readonly publicationStatus?: number;
  readonly isActive?: boolean;
  readonly schoolBranchId?: string | null;
  readonly educationalStageId?: string | null;
  readonly gradeId?: string | null;
  readonly academicYearId?: string | null;
  readonly updatedAtUtc?: string;
}

export interface SchoolAdmissionRequirementDetail extends SchoolAdmissionRequirementListItem {
  readonly descriptionAr?: string | null;
  readonly descriptionEn?: string | null;
  readonly scopeKey?: string;
  readonly specificityScore?: number;
  readonly documentCode?: number | null;
  readonly profileFieldCode?: number | null;
  readonly allowedFileExtensions?: readonly string[];
  readonly maxFileSizeBytes?: number | null;
  readonly allowChildVaultCopy?: boolean;
  readonly createdAtUtc?: string;
  readonly publishedAtUtc?: string | null;
}

export interface CreateSchoolAdmissionRequirementBody {
  requirementCode: string;
  kind: number;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string;
  descriptionEn?: string;
  isRequired: boolean;
  sortOrder: number;
  schoolBranchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  profileFieldCode?: number;
  documentCode?: number;
  allowedFileExtensions?: string[];
  maxFileSizeBytes?: number;
  allowChildVaultCopy: boolean;
}

export interface UpdateSchoolAdmissionRequirementBody {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string;
  descriptionEn?: string;
  isRequired: boolean;
  sortOrder: number;
  schoolBranchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  profileFieldCode?: number;
  documentCode?: number;
  allowedFileExtensions?: string[];
  maxFileSizeBytes?: number;
  allowChildVaultCopy: boolean;
}

export interface ListSchoolAdmissionRequirementsParams {
  branchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  kind?: number;
  publicationStatus?: number;
  isActive?: boolean;
}

export interface ReorderSchoolAdmissionRequirementsBody {
  orderedRequirementIds: readonly string[];
}

export type SchoolAdmissionRequirementResult = {
  succeeded?: boolean;
  data?: SchoolAdmissionRequirementDetail;
  errors?: string[];
  errorCodes?: string[];
};

export type SchoolAdmissionRequirementListResult = {
  succeeded?: boolean;
  data?: SchoolAdmissionRequirementListItem[];
  errors?: string[];
  errorCodes?: string[];
};

/** UI view models aligned with SchoolAdmissionQuestion* DTOs (HttpClient until NSwag). */
export interface SchoolAdmissionQuestionOption {
  readonly optionCode: string;
  readonly labelAr: string;
  readonly labelEn: string;
  readonly sortOrder: number;
  readonly isActive: boolean;
}

export interface SchoolAdmissionQuestionListItem {
  readonly id?: string;
  readonly questionCode?: string;
  readonly questionType?: number;
  readonly labelAr?: string;
  readonly labelEn?: string;
  readonly isRequired?: boolean;
  readonly sortOrder?: number;
  readonly publicationStatus?: number;
  readonly isActive?: boolean;
  readonly schoolBranchId?: string | null;
  readonly educationalStageId?: string | null;
  readonly gradeId?: string | null;
  readonly academicYearId?: string | null;
  readonly updatedAtUtc?: string;
}

export interface SchoolAdmissionQuestionDetail extends SchoolAdmissionQuestionListItem {
  readonly helpAr?: string | null;
  readonly helpEn?: string | null;
  readonly scopeKey?: string;
  readonly specificityScore?: number;
  readonly minLength?: number | null;
  readonly maxLength?: number | null;
  readonly minSelectedOptions?: number | null;
  readonly maxSelectedOptions?: number | null;
  readonly minDate?: string | null;
  readonly maxDate?: string | null;
  readonly allowedFileExtensions?: readonly string[];
  readonly maxFileSizeBytes?: number | null;
  readonly allowChildVaultCopy?: boolean;
  readonly options?: readonly SchoolAdmissionQuestionOption[];
  readonly createdAtUtc?: string;
  readonly publishedAtUtc?: string | null;
}

export interface CreateSchoolAdmissionQuestionBody {
  questionCode: string;
  questionType: number;
  labelAr: string;
  labelEn: string;
  helpAr?: string;
  helpEn?: string;
  isRequired: boolean;
  sortOrder: number;
  schoolBranchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  minLength?: number;
  maxLength?: number;
  minSelectedOptions?: number;
  maxSelectedOptions?: number;
  minDate?: string;
  maxDate?: string;
  allowedFileExtensions?: string[];
  maxFileSizeBytes?: number;
  allowChildVaultCopy: boolean;
  options?: SchoolAdmissionQuestionOption[];
}

export interface UpdateSchoolAdmissionQuestionBody {
  labelAr: string;
  labelEn: string;
  helpAr?: string;
  helpEn?: string;
  isRequired: boolean;
  sortOrder: number;
  schoolBranchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  minLength?: number;
  maxLength?: number;
  minSelectedOptions?: number;
  maxSelectedOptions?: number;
  minDate?: string;
  maxDate?: string;
  allowedFileExtensions?: string[];
  maxFileSizeBytes?: number;
  allowChildVaultCopy: boolean;
  options?: SchoolAdmissionQuestionOption[];
}

export interface ListSchoolAdmissionQuestionsParams {
  branchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  questionType?: number;
  publicationStatus?: number;
  isActive?: boolean;
}

export interface ReorderSchoolAdmissionQuestionsBody {
  orderedQuestionIds: readonly string[];
}

export type SchoolAdmissionQuestionResult = ApiResult<SchoolAdmissionQuestionDetail>;
export type SchoolAdmissionQuestionListResult = ApiResult<SchoolAdmissionQuestionListItem[]>;

/** Official NSwag age-eligibility contracts. */
export type {
  CloneSchoolChildAgeEligibilityRuleRequest,
  CreateSchoolChildAgeEligibilityRuleRequest,
  GrantAdmissionAgeEligibilityExceptionRequest,
  PreviewSchoolChildAgeEligibilityDto,
  PreviewSchoolChildAgeEligibilityRequest,
  SchoolChildAgeEligibilityRuleDetailDto,
  SchoolChildAgeEligibilityRuleListItemDto,
  UpdateSchoolChildAgeEligibilityRuleRequest,
} from '../../../core/api-client/SwaggerClient.service';

export {
  ChildAgeEligibilityExceptionReasonCode,
  ChildAgeEligibilityPublicationStatus,
  ChildAgeEligibilityResultCode,
  ChildAgeReferenceDateMode,
} from '../../../core/api-client/SwaggerClient.service';

export interface ListSchoolChildAgeEligibilityRulesParams {
  branchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  publicationStatus?: number;
  isActive?: boolean;
}

/** NSwag aliases for interview/assessment policy portal APIs. */
export type {
  SchoolInterviewAssessmentPolicyListItemDto as SchoolInterviewAssessmentPolicyListItem,
  SchoolInterviewAssessmentPolicyDetailDto as SchoolInterviewAssessmentPolicyDetail,
  CreateSchoolInterviewAssessmentPolicyRequest,
  UpdateSchoolInterviewAssessmentPolicyRequest,
  CloneSchoolInterviewAssessmentPolicyRequest,
  SafeMeetingProviderOptionDto,
  PreviewSchoolInterviewAssessmentPolicyApplicabilityDto,
  PreviewSchoolInterviewAssessmentPolicyApplicabilityRequest,
  SafeInterviewAssessmentPolicySummaryDto,
} from '../../../core/api-client/SwaggerClient.service';

export {
  HybridDeliverySelectionAuthority,
  InterviewAssessmentDeliveryMode,
  InterviewAssessmentPolicyPublicationStatus,
  InterviewAssessmentRequiredParticipants,
  InterviewAssessmentRequirementMode,
} from '../../../core/api-client/SwaggerClient.service';

export interface ListSchoolInterviewAssessmentPoliciesParams {
  branchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  publicationStatus?: number;
  isActive?: boolean;
}

/** NSwag aliases for school interview FAQ portal APIs. */
export type {
  SchoolInterviewFaqDetailDto as SchoolInterviewFaqDetail,
  CreateSchoolInterviewFaqRequest,
  UpdateSchoolInterviewFaqRequest,
  ReorderSchoolInterviewFaqsRequest,
  PublicInterviewFaqItemDto,
} from '../../../core/api-client/SwaggerClient.service';

export { FaqOwnershipScope, InterviewFaqCategory } from '../../../core/api-client/SwaggerClient.service';

export interface ListSchoolInterviewFaqsParams {
  interviewCategory?: number;
  branchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
  isPublished?: boolean;
  isActive?: boolean;
}
