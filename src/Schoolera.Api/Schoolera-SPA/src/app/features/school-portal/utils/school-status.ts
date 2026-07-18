import { SchoolStatus } from '../data-access/school-portal.models';

export function schoolStatusTranslationKey(status: SchoolStatus): string {
  switch (status) {
    case SchoolStatus._2:
      return 'portal.status.published';
    case SchoolStatus._3:
      return 'portal.status.unpublished';
    case SchoolStatus._4:
      return 'portal.status.suspended';
    case SchoolStatus._1:
    default:
      return 'portal.status.draft';
  }
}

export function schoolStatusBadgeVariant(status: SchoolStatus): 'draft' | 'published' | 'unpublished' | 'suspended' {
  switch (status) {
    case SchoolStatus._2:
      return 'published';
    case SchoolStatus._3:
      return 'unpublished';
    case SchoolStatus._4:
      return 'suspended';
    case SchoolStatus._1:
    default:
      return 'draft';
  }
}
