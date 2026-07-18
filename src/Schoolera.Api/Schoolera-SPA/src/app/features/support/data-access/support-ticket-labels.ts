import {
  SupportTicketCategory,
  SupportTicketPriority,
  SupportTicketStatus,
} from '../../support/data-access/support-ticket.models';

export function supportTicketStatusKey(status: number | undefined): string {
  switch (status) {
    case SupportTicketStatus.Open:
      return 'status.open';
    case SupportTicketStatus.InProgress:
      return 'status.inProgress';
    case SupportTicketStatus.WaitingForCustomer:
      return 'status.waitingForCustomer';
    case SupportTicketStatus.Resolved:
      return 'status.resolved';
    case SupportTicketStatus.Closed:
      return 'status.closed';
    default:
      return 'status.unknown';
  }
}

export function supportTicketPriorityKey(priority: number | undefined): string {
  switch (priority) {
    case SupportTicketPriority.Low:
      return 'priority.low';
    case SupportTicketPriority.Normal:
      return 'priority.normal';
    case SupportTicketPriority.High:
      return 'priority.high';
    case SupportTicketPriority.Urgent:
      return 'priority.urgent';
    default:
      return 'priority.unknown';
  }
}

export function supportTicketCategoryKey(category: number | undefined): string {
  switch (category) {
    case SupportTicketCategory.GeneralSupport:
      return 'category.generalSupport';
    case SupportTicketCategory.Account:
      return 'category.account';
    case SupportTicketCategory.SchoolInformation:
      return 'category.schoolInformation';
    case SupportTicketCategory.AdmissionApplication:
      return 'category.admissionApplication';
    case SupportTicketCategory.Documents:
      return 'category.documents';
    case SupportTicketCategory.TechnicalIssue:
      return 'category.technicalIssue';
    case SupportTicketCategory.Other:
      return 'category.other';
    default:
      return 'category.unknown';
  }
}
