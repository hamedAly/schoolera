import { ApiResult } from '../../../core/http/api-result';

/** Mirrors Domain `SupportTicketStatus`. */
export const SupportTicketStatus = {
  Open: 1,
  InProgress: 2,
  WaitingForCustomer: 3,
  Resolved: 4,
  Closed: 5,
} as const;
export type SupportTicketStatus = (typeof SupportTicketStatus)[keyof typeof SupportTicketStatus];

/** Mirrors Domain `SupportTicketPriority`. */
export const SupportTicketPriority = {
  Low: 1,
  Normal: 2,
  High: 3,
  Urgent: 4,
} as const;
export type SupportTicketPriority =
  (typeof SupportTicketPriority)[keyof typeof SupportTicketPriority];

/** Mirrors Domain `SupportTicketCategory`. */
export const SupportTicketCategory = {
  GeneralSupport: 1,
  Account: 2,
  SchoolInformation: 3,
  AdmissionApplication: 4,
  Documents: 5,
  TechnicalIssue: 6,
  Other: 7,
} as const;
export type SupportTicketCategory =
  (typeof SupportTicketCategory)[keyof typeof SupportTicketCategory];

/** Mirrors Domain `SupportTicketMessageVisibility`. */
export const SupportTicketMessageVisibility = {
  CustomerVisible: 1,
  InternalSupportNote: 2,
} as const;
export type SupportTicketMessageVisibility =
  (typeof SupportTicketMessageVisibility)[keyof typeof SupportTicketMessageVisibility];

/** Mirrors Domain `SupportTicketAuthorType`. */
export const SupportTicketAuthorType = {
  Parent: 1,
  SupportAgent: 2,
  PlatformAdmin: 3,
  System: 4,
} as const;
export type SupportTicketAuthorType =
  (typeof SupportTicketAuthorType)[keyof typeof SupportTicketAuthorType];

export interface CreateSupportTicketRequest {
  subject: string;
  body: string;
  category: number;
  priority?: number;
  admissionApplicationId?: string | null;
}

export interface SupportTicketReplyRequest {
  body: string;
}

export interface SupportTicketStatusChangeRequest {
  status: number;
}

export interface SupportTicketPriorityChangeRequest {
  priority: number;
}

export interface SupportTicketCategoryChangeRequest {
  category: number;
}

export interface SupportTicketAssignRequest {
  supportAgentUserId: string;
}

export interface SupportTicketAttachmentDto {
  id: string;
  messageId?: string | null;
  visibility: number;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  createdAtUtc: string;
}

export interface SupportTicketMessageDto {
  id: string;
  authorUserId: string;
  authorType: number;
  visibility: number;
  body: string;
  createdAtUtc: string;
  attachments?: SupportTicketAttachmentDto[];
}

export interface SupportTicketHistoryDto {
  id: string;
  action: number;
  actorUserId?: string | null;
  fromValue?: string | null;
  toValue?: string | null;
  summary?: string | null;
  createdAtUtc: string;
}

export interface SupportTicketListItemDto {
  id: string;
  reference: string;
  subject: string;
  status: number;
  priority: number;
  category: number;
  parentUserId: string;
  assignedSupportAgentUserId?: string | null;
  admissionApplicationId?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  firstResponseAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  firstResponseDueAtUtc: string;
  resolutionDueAtUtc: string;
  isFirstResponseOverdue: boolean;
  isResolutionOverdue: boolean;
}

export interface SupportTicketParentDetailDto {
  id: string;
  reference: string;
  subject: string;
  status: number;
  priority: number;
  category: number;
  admissionApplicationId?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  firstResponseAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  closedAtUtc?: string | null;
  messages: SupportTicketMessageDto[];
  attachments: SupportTicketAttachmentDto[];
}

export interface SupportTicketSupportDetailDto {
  id: string;
  reference: string;
  subject: string;
  status: number;
  priority: number;
  category: number;
  parentUserId: string;
  parentDisplayName?: string | null;
  parentEmail?: string | null;
  assignedSupportAgentUserId?: string | null;
  admissionApplicationId?: string | null;
  sourceContactRequestId?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  firstResponseAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  closedAtUtc?: string | null;
  firstResponseDueAtUtc: string;
  resolutionDueAtUtc: string;
  isFirstResponseOverdue: boolean;
  isResolutionOverdue: boolean;
  messages: SupportTicketMessageDto[];
  attachments: SupportTicketAttachmentDto[];
  history: SupportTicketHistoryDto[];
}

export interface SupportTicketPagedResult {
  items: SupportTicketListItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export type SupportTicketListResult = ApiResult<SupportTicketPagedResult>;
export type SupportTicketParentDetailResult = ApiResult<SupportTicketParentDetailDto>;
export type SupportTicketSupportDetailResult = ApiResult<SupportTicketSupportDetailDto>;
export type SupportTicketHistoryListResult = ApiResult<SupportTicketHistoryDto[]>;

export interface SupportTicketListFilters {
  search?: string;
  status?: number;
  priority?: number;
  category?: number;
  assignedSupportAgentUserId?: string;
  unassignedOnly?: boolean;
  firstResponseOverdueOnly?: boolean;
  resolutionOverdueOnly?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export const SUPPORT_TICKET_STATUS_OPTIONS = [
  { value: SupportTicketStatus.Open, labelKey: 'status.open' },
  { value: SupportTicketStatus.InProgress, labelKey: 'status.inProgress' },
  { value: SupportTicketStatus.WaitingForCustomer, labelKey: 'status.waitingForCustomer' },
  { value: SupportTicketStatus.Resolved, labelKey: 'status.resolved' },
  { value: SupportTicketStatus.Closed, labelKey: 'status.closed' },
] as const;

export const SUPPORT_TICKET_PRIORITY_OPTIONS = [
  { value: SupportTicketPriority.Low, labelKey: 'priority.low' },
  { value: SupportTicketPriority.Normal, labelKey: 'priority.normal' },
  { value: SupportTicketPriority.High, labelKey: 'priority.high' },
  { value: SupportTicketPriority.Urgent, labelKey: 'priority.urgent' },
] as const;

export const SUPPORT_TICKET_CATEGORY_OPTIONS = [
  { value: SupportTicketCategory.GeneralSupport, labelKey: 'category.generalSupport' },
  { value: SupportTicketCategory.Account, labelKey: 'category.account' },
  { value: SupportTicketCategory.SchoolInformation, labelKey: 'category.schoolInformation' },
  { value: SupportTicketCategory.AdmissionApplication, labelKey: 'category.admissionApplication' },
  { value: SupportTicketCategory.Documents, labelKey: 'category.documents' },
  { value: SupportTicketCategory.TechnicalIssue, labelKey: 'category.technicalIssue' },
  { value: SupportTicketCategory.Other, labelKey: 'category.other' },
] as const;
