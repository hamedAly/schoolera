/**
 * Named helpers over NSwag-generated school-portal team/permission contracts.
 * Generated enums use `_1`/`_2` style names — keep friendly constants here.
 */

import {
  SchoolBranchScopeMode as GeneratedSchoolBranchScopeMode,
  SchoolPortalPermissionsDto,
  SchoolTeamMemberDto,
  SchoolTeamMemberDtoResult,
  SchoolTeamRole as GeneratedSchoolTeamRole,
  TransferSchoolOwnershipRequest,
  UpdateSchoolTeamMemberRequest,
  UpsertSchoolTeamMemberRequest,
  AccessibleSchoolDto,
} from '../../../core/api-client/SwaggerClient.service';

export type {
  AccessibleSchoolDto,
  SchoolPortalPermissionsDto,
  TransferSchoolOwnershipRequest,
  UpdateSchoolTeamMemberRequest,
  UpsertSchoolTeamMemberRequest,
};

export type SchoolTeamRole = GeneratedSchoolTeamRole;
export type SchoolBranchScopeMode = GeneratedSchoolBranchScopeMode;
export type PortalTeamMemberDto = SchoolTeamMemberDto;
export type PortalTeamMemberResult = SchoolTeamMemberDtoResult;

/** Friendly names for generated `SchoolTeamRole` (`_1`…`_4`). */
export const SchoolTeamRole = {
  SchoolAdmin: GeneratedSchoolTeamRole._1 as GeneratedSchoolTeamRole,
  AdmissionOfficer: GeneratedSchoolTeamRole._2 as GeneratedSchoolTeamRole,
  FinanceOfficer: GeneratedSchoolTeamRole._3 as GeneratedSchoolTeamRole,
  ContentModerator: GeneratedSchoolTeamRole._4 as GeneratedSchoolTeamRole,
};

/** Friendly names for generated `SchoolBranchScopeMode` (`_1`…`_2`). */
export const SchoolBranchScopeMode = {
  AllBranches: GeneratedSchoolBranchScopeMode._1 as GeneratedSchoolBranchScopeMode,
  SelectedBranches: GeneratedSchoolBranchScopeMode._2 as GeneratedSchoolBranchScopeMode,
};

export type PortalPermissionKey =
  | 'canViewDashboard'
  | 'canViewTeam'
  | 'canManageTeam'
  | 'canTransferOwnership'
  | 'canViewProfile'
  | 'canManageProfile'
  | 'canManageBranches'
  | 'canManageOfferings'
  | 'canManageFacilities'
  | 'canManageGallery'
  | 'canManageServices'
  | 'canManagePublicContact'
  | 'canViewApplications'
  | 'canManageApplicationReview'
  | 'canDownloadApplicationAttachments'
  | 'canExportApplications'
  | 'canManageAdmissionRequirements'
  | 'canManageAdmissionQuestions'
  | 'canViewFees'
  | 'canManageFees'
  | 'canManageContent';
