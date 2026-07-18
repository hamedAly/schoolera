import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import {
  AccessibleSchoolDto,
  AccessibleSchoolListResult,
  SchoolDashboardDto,
  SchoolDashboardResult,
} from './school-portal.models';
import {
  PortalPermissionKey,
  SchoolPortalPermissionsDto,
} from './school-portal-permissions.models';
import { hasPortalPermission } from './portal-permissions';
import { SchoolPortalApi } from './school-portal.api';

/** Shared portal state: accessible schools, active school, and permission signals. */
@Injectable({
  providedIn: 'root',
})
export class PortalContextService {
  private readonly api = inject(SchoolPortalApi);

  private readonly schools = signal<readonly AccessibleSchoolDto[]>([]);
  private readonly schoolsLoaded = signal(false);
  private readonly activeSchoolId = signal<string | null>(null);
  private readonly dashboard = signal<SchoolDashboardDto | null>(null);
  private readonly dashboardLoading = signal(false);

  readonly accessibleSchools = this.schools.asReadonly();
  readonly schoolsReady = this.schoolsLoaded.asReadonly();
  readonly currentSchoolId = this.activeSchoolId.asReadonly();
  readonly currentDashboard = this.dashboard.asReadonly();
  readonly dashboardBusy = this.dashboardLoading.asReadonly();

  readonly currentSchool = computed(() => {
    const id = this.activeSchoolId();
    if (!id) {
      return null;
    }
    return this.schools().find((school) => school.id === id) ?? null;
  });

  readonly currentPermissions = computed(
    (): SchoolPortalPermissionsDto | null => this.currentSchool()?.permissions ?? null,
  );

  readonly canViewDashboard = computed(() => this.flag('canViewDashboard'));
  readonly canViewTeam = computed(() => this.flag('canViewTeam'));
  readonly canManageTeam = computed(() => {
    const permissions = this.currentPermissions();
    if (permissions != null) {
      return !!permissions.canManageTeam;
    }
    return !!this.currentSchool()?.isOwner;
  });
  readonly canTransferOwnership = computed(() => {
    const permissions = this.currentPermissions();
    if (permissions != null) {
      return !!permissions.canTransferOwnership;
    }
    return !!this.currentSchool()?.isOwner;
  });
  readonly canViewProfile = computed(() => this.flag('canViewProfile'));
  readonly canManageProfile = computed(() => this.flag('canManageProfile'));
  readonly canManageBranches = computed(() => this.flag('canManageBranches'));
  readonly canManageOfferings = computed(() => this.flag('canManageOfferings'));
  readonly canManageFacilities = computed(() => this.flag('canManageFacilities'));
  readonly canManageGallery = computed(() => this.flag('canManageGallery'));
  readonly canManageServices = computed(() => this.flag('canManageServices'));
  readonly canManagePublicContact = computed(() => this.flag('canManagePublicContact'));
  readonly canViewApplications = computed(() => this.flag('canViewApplications'));
  readonly canManageApplicationReview = computed(() => this.flag('canManageApplicationReview'));
  readonly canDownloadApplicationAttachments = computed(() =>
    this.flag('canDownloadApplicationAttachments'),
  );
  readonly canExportApplications = computed(() => this.flag('canExportApplications'));
  readonly canManageAdmissionRequirements = computed(() =>
    this.flag('canManageAdmissionRequirements'),
  );
  readonly canManageAdmissionQuestions = computed(() => this.flag('canManageAdmissionQuestions'));
  readonly canViewFees = computed(() => this.flag('canViewFees'));
  readonly canManageFees = computed(() => this.flag('canManageFees'));
  readonly canManageContent = computed(() => this.flag('canManageContent'));

  loadAccessibleSchools(): Observable<AccessibleSchoolListResult> {
    return this.api.listAccessibleSchools().pipe(
      tap((result) => {
        this.schoolsLoaded.set(true);
        if (result.succeeded && result.data) {
          this.schools.set(result.data as readonly AccessibleSchoolDto[]);
        } else {
          this.schools.set([]);
        }
      }),
    );
  }

  setActiveSchoolId(schoolId: string | null): void {
    this.activeSchoolId.set(schoolId);
    if (!schoolId) {
      this.dashboard.set(null);
    }
  }

  loadDashboard(schoolId: string): Observable<SchoolDashboardResult> {
    this.dashboardLoading.set(true);
    return this.api.getDashboard(schoolId).pipe(
      tap((result) => {
        this.dashboardLoading.set(false);
        if (result.succeeded && result.data) {
          this.dashboard.set(result.data);
          this.activeSchoolId.set(schoolId);
        }
      }),
    );
  }

  refreshDashboard(): Observable<SchoolDashboardResult> | null {
    const schoolId = this.activeSchoolId();
    return schoolId ? this.loadDashboard(schoolId) : null;
  }

  isSchoolAccessible(schoolId: string): boolean {
    return this.schools().some((school) => school.id === schoolId);
  }

  private flag(key: PortalPermissionKey): boolean {
    return hasPortalPermission(this.currentPermissions(), key);
  }
}
