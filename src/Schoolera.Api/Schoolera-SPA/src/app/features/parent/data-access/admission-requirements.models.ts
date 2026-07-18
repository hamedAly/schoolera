import type { AdmissionApplicationDetailDto } from '../../../core/api-client/SwaggerClient.service';

/** UI view model until NSwag includes admission requirement checklist DTOs. */
export interface AdmissionRequirementChecklistItem {
  readonly snapshotId?: string;
  readonly requirementCode?: string;
  readonly kind?: number;
  readonly name?: string;
  readonly description?: string | null;
  readonly isRequired?: boolean;
  readonly sortOrder?: number;
  readonly isComplete?: boolean;
  readonly reasonCode?: string | null;
  readonly wizardSection?: string;
  readonly profileFieldCode?: number | null;
  readonly documentCode?: number | null;
  readonly allowedFileExtensions?: readonly string[];
  readonly maxFileSizeBytes?: number | null;
  readonly allowChildVaultCopy?: boolean;
  readonly linkedAttachmentId?: string | null;
}

export type AdmissionApplicationWithRequirements = AdmissionApplicationDetailDto & {
  readonly requirements?: readonly AdmissionRequirementChecklistItem[];
  readonly questions?: readonly import('./admission-questions.models').AdmissionQuestionChecklistItem[];
};

export interface PublicAdmissionRequirementSummary {
  readonly requirementCode?: string;
  readonly kind?: number;
  readonly name?: string;
  readonly description?: string | null;
  readonly isRequired?: boolean;
  readonly sortOrder?: number;
  readonly documentCode?: number | null;
  readonly allowedFileExtensions?: readonly string[];
  readonly maxFileSizeBytes?: number | null;
  readonly allowChildVaultCopy?: boolean;
}

export const AdmissionRequirementKind = {
  InformationalText: 1,
  ParentProfileField: 2,
  ChildProfileField: 3,
  ApplicationDocument: 4,
} as const;

export const AdmissionRequirementPublicationStatus = {
  Draft: 1,
  Published: 2,
} as const;

export const PARENT_PROFILE_FIELD_CODES = new Set([
  1, 2, 10, 11, 12, 13, 20, 21, 22, 23,
]);

export const CHILD_PROFILE_FIELD_CODES = new Set([30, 31, 32, 33, 34, 35, 36, 37]);

export function profileFieldEditLink(
  profileFieldCode: number | null | undefined,
  childProfileId: string | null | undefined,
  returnUrl: string,
): { route: string[]; queryParams: { returnUrl: string } } | null {
  if (profileFieldCode == null) {
    return null;
  }

  if (PARENT_PROFILE_FIELD_CODES.has(profileFieldCode)) {
    return { route: ['/parent/profile'], queryParams: { returnUrl } };
  }

  if (CHILD_PROFILE_FIELD_CODES.has(profileFieldCode) && childProfileId) {
    return {
      route: ['/parent/children', childProfileId, 'edit'],
      queryParams: { returnUrl },
    };
  }

  return null;
}
