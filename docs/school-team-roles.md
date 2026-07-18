# School Team Roles and Permissions (Phase 1)

Audit date: 2026-07-17  
Feature: School-scoped membership roles, central permission matrix, optional branch scope

This document is the authoritative Phase 1 matrix for School Portal team roles. It extends [school-portal.md](./school-portal.md). It does **not** describe a generic IAM designer, custom roles, or multi-school organizations.

---

## 1. Membership roles

School membership roles are scoped to one `School`. A global Identity role alone never grants school access.

| Membership role | Storage | Notes |
|-----------------|---------|--------|
| Owner | `School.OwnerUserId` only | Never duplicated as a `SchoolTeamMember` row |
| SchoolAdmin | `SchoolTeamRole.SchoolAdmin = 1` | School-wide |
| AdmissionOfficer | `SchoolTeamRole.AdmissionOfficer = 2` | Optional branch scope |
| FinanceOfficer | `SchoolTeamRole.FinanceOfficer = 3` | Optional branch scope |
| ContentModerator | `SchoolTeamRole.ContentModerator = 4` | School-wide public content |

Matching Identity roles (`SchoolAdmin`, `AdmissionOfficer`, `FinanceOfficer`, `ContentModerator`, `SchoolOwner`) are required for the `SchoolPortal` cookie policy gate and for eligibility when linking memberships. Access to a specific school still requires ownership or an **active** membership for that school.

---

## 2. Branch scope

Applies only to **AdmissionOfficer** and **FinanceOfficer**.

| Mode | Meaning |
|------|---------|
| `AllBranches = 1` | All current and future branches of the school |
| `SelectedBranches = 2` | Explicit allowlist in `SchoolTeamMemberBranches` |

Rules:

- Empty selected branch IDs are rejected (never treated as unrestricted).
- Selected branches must belong to the membership’s school.
- Owner, SchoolAdmin, and ContentModerator remain school-wide.
- Scope changes take effect on later requests; out-of-scope records become inaccessible without deletion.
- Out-of-scope access returns `schoolPortal.branchOutOfScope` (HTTP 403).

---

## 3. Central permission matrix

Implementation: `SchoolPortalPermission` + `SchoolPortalPermissionMatrix` + `ISchoolPortalAccess`.

Handlers call `RequirePermission` / `RequireEditablePermission` / `RequireOwner` / `RequireBranch`. Controllers stay thin.

| Permission | Owner | SchoolAdmin | AdmissionOfficer | FinanceOfficer | ContentModerator |
|------------|:-----:|:-----------:|:----------------:|:--------------:|:----------------:|
| ViewDashboard | ✓ | ✓ | ✓ | ✓ | ✓ |
| ViewTeam | ✓ | ✓ | | | |
| ManageTeam | ✓ | | | | |
| TransferOwnership | ✓ | | | | |
| ViewProfile / ManageProfile | ✓ | ✓ | | | ✓ |
| ManageBranches / ManageOfferings | ✓ | ✓ | | | |
| ManageFacilities / Gallery / Services / PublicContact / Content | ✓ | ✓ | | | ✓ |
| ViewApplications / ManageApplicationReview / Attachments / Export | ✓ | ✓ | ✓ | | |
| ManageAdmissionRequirements / Questions | ✓ | ✓ | ✓ | | |
| ViewFees / ManageFees | ✓ | ✓ | | ✓ | |

SchoolAdmin must not manage team or transfer ownership. ContentModerator must not access applications, fees, or team management.

Server-calculated `SchoolPortalPermissionsDto` is returned on accessible-school rows for Angular navigation and action UX. Angular must not submit permissions.

---

## 4. Owner safety

- A school always retains at least one active Owner (`OwnerUserId`).
- Ownership transfer is explicit (`POST .../team/transfer-ownership`), atomic, and Owner-only.
- The new Owner must be an eligible `SchoolOwner` Identity user; prior membership for that user on the school is deactivated when ownership is assigned.
- SchoolAdmin and other roles cannot promote themselves to Owner.
- Concurrent transfers use the existing unit-of-work / concurrency patterns; zero-owner outcomes are rejected.

---

## 5. Team APIs

| Method | Path | Auth |
|--------|------|------|
| GET | `/api/school-portal/schools/{schoolId}/team` | ViewTeam |
| POST | `/api/school-portal/schools/{schoolId}/team/members` | Owner + ManageTeam |
| PUT | `/api/school-portal/schools/{schoolId}/team/members/{membershipId}` | Owner + ManageTeam |
| POST | `/api/school-portal/schools/{schoolId}/team/transfer-ownership` | Owner + TransferOwnership |
| POST | `/api/school-portal/schools/{schoolId}/team/school-admins` | Legacy → upsert SchoolAdmin |
| DELETE | `/api/school-portal/schools/{schoolId}/team/school-admins/{membershipId}` | Legacy deactivate SchoolAdmin |

Membership linking reuses exact-email account linking (no invitation delivery in Phase 1).

Audited actions (via `ISchoolPortalAuditWriter` → admin audit events): member upsert, activate/deactivate, role/scope change, ownership transfer. No passwords, cookies, or applicant PII in audit payloads.

---

## 6. Migration

`20260717022747_AddSchoolTeamRolesAndBranchScope`

- Adds `SchoolTeamMembers.BranchScopeMode` (default `AllBranches`)
- Adds `SchoolTeamMemberBranches` join table
- Preserves existing SchoolAdmin memberships and Owner linkage
