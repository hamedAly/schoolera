import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { LanguageSwitcher } from '../../../../core/layout/language-switcher/language-switcher';
import { PortalContextService } from '../../data-access/portal-context.service';
import { PortalPermissionKey } from '../../data-access/school-portal-permissions.models';
import { localizedBilingualName } from '../../utils/localized-name';
import { StatusBadge } from '../../components/status-badge/status-badge';

interface PortalNavItem {
  readonly path: string;
  readonly labelKey: string;
  readonly permissions: readonly PortalPermissionKey[];
}

const PORTAL_NAV: readonly PortalNavItem[] = [
  { path: 'overview', labelKey: 'portal.nav.overview', permissions: ['canViewDashboard'] },
  { path: 'profile', labelKey: 'portal.nav.profile', permissions: ['canViewProfile', 'canManageProfile', 'canManageContent'] },
  { path: 'branches', labelKey: 'portal.nav.branches', permissions: ['canManageBranches'] },
  { path: 'stages', labelKey: 'portal.nav.stages', permissions: ['canManageOfferings'] },
  { path: 'fees', labelKey: 'portal.nav.fees', permissions: ['canViewFees', 'canManageFees'] },
  { path: 'facilities', labelKey: 'portal.nav.facilities', permissions: ['canManageFacilities', 'canManageContent'] },
  { path: 'gallery', labelKey: 'portal.nav.gallery', permissions: ['canManageGallery', 'canManageContent'] },
  { path: 'services', labelKey: 'portal.nav.services', permissions: ['canManageServices', 'canManageContent'] },
  { path: 'team', labelKey: 'portal.nav.team', permissions: ['canViewTeam', 'canManageTeam'] },
  { path: 'applications', labelKey: 'portal.nav.applications', permissions: ['canViewApplications'] },
  { path: 'admission-requirements', labelKey: 'portal.nav.admissionRequirements', permissions: ['canManageAdmissionRequirements'] },
  { path: 'admission-questions', labelKey: 'portal.nav.admissionQuestions', permissions: ['canManageAdmissionQuestions'] },
  { path: 'age-eligibility-rules', labelKey: 'portal.nav.ageEligibilityRules', permissions: ['canManageAdmissionRequirements'] },
  {
    path: 'interview-assessment-policies',
    labelKey: 'portal.nav.interviewAssessmentPolicies',
    permissions: ['canManageAdmissionRequirements'],
  },
  {
    path: 'interview-assessment-slots',
    labelKey: 'portal.nav.interviewAssessmentSlots',
    permissions: ['canManageAdmissionRequirements'],
  },
  { path: 'interview-faqs', labelKey: 'portal.nav.interviewFaqs', permissions: ['canManageContent'] },
] as const;

@Component({
  selector: 'se-school-portal-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslocoPipe,
    LanguageSwitcher,
    StatusBadge,
  ],
  templateUrl: './school-portal-layout.html',
  styleUrl: './school-portal-layout.scss',
})
export class SchoolPortalLayout implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly context = inject(PortalContextService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly navItems = computed(() => {
    const permissions = this.context.currentPermissions();
    return PORTAL_NAV.filter((item) => item.permissions.some((permission) => !!permissions?.[permission]));
  });
  protected readonly mobileNavOpen = signal(false);
  protected readonly userMenuOpen = signal(false);

  protected readonly authUser = this.auth.currentUser;
  protected readonly dashboard = this.context.currentDashboard;
  protected readonly schools = this.context.accessibleSchools;
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly schoolId = computed(() => this.route.snapshot.paramMap.get('schoolId') ?? '');
  protected readonly schoolName = computed(() => {
    const dashboard = this.dashboard();
    if (dashboard) {
      return localizedBilingualName(dashboard.nameAr ?? '', dashboard.nameEn, this.activeLang());
    }
    const school = this.context.currentSchool();
    return school
      ? localizedBilingualName(school.nameAr ?? '', school.nameEn, this.activeLang())
      : '';
  });

  protected readonly logoUrl = computed(
    () => this.dashboard()?.logoUrl ?? this.context.currentSchool()?.logoUrl ?? null,
  );

  ngOnInit(): void {
    const schoolId = this.schoolId();
    if (schoolId) {
      this.context.loadDashboard(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }
  }

  protected navLink(schoolId: string, segment: string): string[] {
    return ['/school', schoolId, segment];
  }

  protected schoolLabel(nameAr?: string | null, nameEn?: string | null): string {
    return localizedBilingualName(nameAr, nameEn, this.activeLang());
  }

  protected toggleMobileNav(): void {
    this.mobileNavOpen.update((open) => !open);
  }

  protected closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  protected closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  protected onSchoolChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const nextId = select.value;
    if (!nextId || nextId === this.schoolId()) {
      return;
    }
    const currentChild = this.route.snapshot.firstChild?.routeConfig?.path ?? 'overview';
    void this.router.navigate(['/school', nextId, currentChild]);
    this.closeMobileNav();
  }

  protected logout(): void {
    this.auth.logout().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      void this.router.navigate(['/']);
    });
  }
}
