import type { AdmissionApplicationDetailDto } from '../../../core/api-client/SwaggerClient.service';

/** UI view model until NSwag includes admission question checklist DTOs. */
export interface AdmissionQuestionOptionSnapshot {
  readonly optionCode?: string;
  readonly label?: string;
  readonly sortOrder?: number;
}

export interface AdmissionQuestionChecklistItem {
  readonly snapshotId?: string;
  readonly questionCode?: string;
  readonly questionType?: number;
  readonly label?: string;
  readonly help?: string | null;
  readonly isRequired?: boolean;
  readonly sortOrder?: number;
  readonly isComplete?: boolean;
  readonly reasonCode?: string | null;
  readonly wizardSection?: string;
  readonly minLength?: number | null;
  readonly maxLength?: number | null;
  readonly minSelectedOptions?: number | null;
  readonly maxSelectedOptions?: number | null;
  readonly minDate?: string | null;
  readonly maxDate?: string | null;
  readonly allowedFileExtensions?: readonly string[];
  readonly maxFileSizeBytes?: number | null;
  readonly allowChildVaultCopy?: boolean;
  readonly options?: readonly AdmissionQuestionOptionSnapshot[];
  readonly textValue?: string | null;
  readonly selectedOptionCodes?: readonly string[];
  readonly dateValue?: string | null;
  readonly booleanValue?: boolean | null;
  readonly linkedAttachmentId?: string | null;
}

export interface MissingAdmissionQuestion {
  readonly questionSnapshotId?: string;
  readonly questionCode?: string;
  readonly questionType?: number;
  readonly displayName?: string;
  readonly reasonCode?: string;
  readonly wizardSection?: string;
}

export type AdmissionApplicationWithQuestions = AdmissionApplicationDetailDto & {
  readonly requirements?: readonly import('./admission-requirements.models').AdmissionRequirementChecklistItem[];
  readonly questions?: readonly AdmissionQuestionChecklistItem[];
};

export const AdmissionQuestionType = {
  ShortText: 1,
  LongText: 2,
  SingleChoice: 3,
  MultipleChoice: 4,
  Date: 5,
  YesNo: 6,
  File: 7,
} as const;

export const AdmissionQuestionPublicationStatus = {
  Draft: 1,
  Published: 2,
} as const;

export function isChoiceQuestionType(type: number | undefined): boolean {
  return (
    type === AdmissionQuestionType.SingleChoice || type === AdmissionQuestionType.MultipleChoice
  );
}

export function isTextQuestionType(type: number | undefined): boolean {
  return type === AdmissionQuestionType.ShortText || type === AdmissionQuestionType.LongText;
}

export function selectedOptionLabels(
  question: AdmissionQuestionChecklistItem,
): readonly string[] {
  const codes = new Set(question.selectedOptionCodes ?? []);
  return (question.options ?? [])
    .filter((opt) => opt.optionCode && codes.has(opt.optionCode))
    .map((opt) => opt.label ?? opt.optionCode ?? '')
    .filter(Boolean);
}
