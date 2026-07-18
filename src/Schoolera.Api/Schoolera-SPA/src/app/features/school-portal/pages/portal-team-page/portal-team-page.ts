import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { ConfirmationDialog } from '../../components/confirmation-dialog/confirmation-dialog';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { PortalContextService } from '../../data-access/portal-context.service';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import {
  branchScopeLabelKey,
  roleSupportsBranchScope,
  teamRoleLabelKey,
} from '../../data-access/portal-permissions';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import {
  PortalTeamMemberDto,
  SchoolBranchScopeMode,
  SchoolTeamRole,
} from '../../data-access/school-portal-permissions.models';
import { SchoolBranchDto } from '../../data-access/school-portal.models';
import { localizedBilingualName } from '../../utils/localized-name';

type ConfirmAction =
  | { kind: 'deactivate'; member: PortalTeamMemberDto }
  | { kind: 'activate'; member: PortalTeamMemberDto }
  | { kind: 'saveEdit'; member: PortalTeamMemberDto }
  | { kind: 'transfer' };

const TEAM_ROLES: readonly SchoolTeamRole[] = [
  SchoolTeamRole.SchoolAdmin,
  SchoolTeamRole.AdmissionOfficer,
  SchoolTeamRole.FinanceOfficer,
  SchoolTeamRole.ContentModerator,
];

@Component({
  selector: 'se-portal-team-page',
  imports: [
    Button,
    ConfirmationDialog,
    FormField,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    FormsModule,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-team-page.html',
  styleUrl: './portal-team-page.scss',
})
export class PortalTeamPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly context = inject(PortalContextService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly SchoolTeamRole = SchoolTeamRole;
  protected readonly SchoolBranchScopeMode = SchoolBranchScopeMode;
  protected readonly teamRoles = TEAM_ROLES;
  protected readonly teamRoleLabelKey = teamRoleLabelKey;
  protected readonly branchScopeLabelKey = branchScopeLabelKey;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly team = signal<readonly PortalTeamMemberDto[]>([]);
  protected readonly branches = signal<readonly SchoolBranchDto[]>([]);
  protected readonly editingMemberId = signal<string | null>(null);
  protected readonly confirmAction = signal<ConfirmAction | null>(null);
  protected readonly transferOpen = signal(false);

  protected readonly canManageTeam = this.context.canManageTeam;
  protected readonly canTransferOwnership = this.context.canTransferOwnership;
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly addForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: [SchoolTeamRole.SchoolAdmin as SchoolTeamRole, Validators.required],
    branchScopeMode: [SchoolBranchScopeMode.AllBranches as SchoolBranchScopeMode, Validators.required],
    branchIds: [[] as string[]],
  });

  protected readonly editForm = this.formBuilder.nonNullable.group({
    role: [SchoolTeamRole.SchoolAdmin as SchoolTeamRole, Validators.required],
    branchScopeMode: [SchoolBranchScopeMode.AllBranches as SchoolBranchScopeMode, Validators.required],
    branchIds: [[] as string[]],
    isActive: [true],
  });

  protected readonly transferForm = this.formBuilder.nonNullable.group({
    newOwnerEmail: ['', [Validators.required, Validators.email]],
  });

  protected readonly addRole = signal(SchoolTeamRole.SchoolAdmin);
  protected readonly editRole = signal(SchoolTeamRole.SchoolAdmin);
  protected readonly addNeedsBranchScope = computed(() => roleSupportsBranchScope(this.addRole()));
  protected readonly editNeedsBranchScope = computed(() => roleSupportsBranchScope(this.editRole()));

  ngOnInit(): void {
    this.addForm.controls.role.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((role) => {
        this.addRole.set(role);
        this.onRoleChanged(role, 'add');
      });

    this.editForm.controls.role.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((role) => {
        this.editRole.set(role);
        this.onRoleChanged(role, 'edit');
      });

    this.loadPage();
  }

  protected retry(): void {
    this.errorMessage.set(null);
    this.loadPage();
  }

  protected branchLabel(branch: SchoolBranchDto): string {
    return localizedBilingualName(branch.nameAr, branch.nameEn, this.activeLang());
  }

  protected memberBranchSummary(member: PortalTeamMemberDto): string {
    if (member.isOwner) {
      return this.transloco.translate('portal.team.scope.all');
    }
    if (!roleSupportsBranchScope(member.role ?? null)) {
      return this.transloco.translate('portal.team.scope.notApplicable');
    }
    if (member.branchScopeMode === SchoolBranchScopeMode.SelectedBranches) {
      const count = member.allowedBranchIds?.length ?? 0;
      return this.transloco.translate('portal.team.scope.selectedCount', { count });
    }
    return this.transloco.translate('portal.team.scope.all');
  }

  protected toggleAddBranch(branchId: string | undefined, checked: boolean): void {
    if (!branchId) {
      return;
    }
    const current = this.addForm.controls.branchIds.value;
    const next = checked
      ? [...new Set([...current, branchId])]
      : current.filter((id) => id !== branchId);
    this.addForm.controls.branchIds.setValue(next);
  }

  protected toggleEditBranch(branchId: string | undefined, checked: boolean): void {
    if (!branchId) {
      return;
    }
    const current = this.editForm.controls.branchIds.value;
    const next = checked
      ? [...new Set([...current, branchId])]
      : current.filter((id) => id !== branchId);
    this.editForm.controls.branchIds.setValue(next);
  }

  protected isBranchSelected(form: 'add' | 'edit', branchId: string | undefined): boolean {
    if (!branchId) {
      return false;
    }
    const ids = form === 'add' ? this.addForm.controls.branchIds.value : this.editForm.controls.branchIds.value;
    return ids.includes(branchId);
  }

  protected addMember(): void {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }

    const raw = this.addForm.getRawValue();
    const supportsScope = roleSupportsBranchScope(raw.role);
    const branchScopeMode = supportsScope ? raw.branchScopeMode : SchoolBranchScopeMode.AllBranches;
    const branchIds =
      supportsScope && branchScopeMode === SchoolBranchScopeMode.SelectedBranches ? raw.branchIds : [];

    this.saving.set(true);
    this.errorMessage.set(null);
    this.api
      .addTeamMember(schoolId, {
        email: raw.email.trim(),
        role: raw.role,
        branchScopeMode,
        branchIds,
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.addForm.reset({
            email: '',
            role: SchoolTeamRole.SchoolAdmin,
            branchScopeMode: SchoolBranchScopeMode.AllBranches,
            branchIds: [],
          });
          this.loadTeam();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected startEdit(member: PortalTeamMemberDto): void {
    if (!member.membershipId || member.isOwner) {
      return;
    }
    this.editingMemberId.set(member.membershipId);
    const role = member.role ?? SchoolTeamRole.SchoolAdmin;
    this.editRole.set(role);
    this.editForm.reset({
      role,
      branchScopeMode: member.branchScopeMode ?? SchoolBranchScopeMode.AllBranches,
      branchIds: [...(member.allowedBranchIds ?? [])],
      isActive: member.isActive !== false,
    });
  }

  protected cancelEdit(): void {
    this.editingMemberId.set(null);
  }

  protected requestSaveEdit(member: PortalTeamMemberDto): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.confirmAction.set({ kind: 'saveEdit', member });
  }

  protected requestToggleActive(member: PortalTeamMemberDto): void {
    if (!member.membershipId || member.isOwner) {
      return;
    }
    this.confirmAction.set({
      kind: member.isActive === false ? 'activate' : 'deactivate',
      member,
    });
  }

  protected openTransfer(): void {
    this.transferForm.reset({ newOwnerEmail: '' });
    this.transferOpen.set(true);
  }

  protected closeTransfer(): void {
    this.transferOpen.set(false);
  }

  protected requestTransfer(): void {
    if (this.transferForm.invalid) {
      this.transferForm.markAllAsTouched();
      return;
    }
    this.confirmAction.set({ kind: 'transfer' });
  }

  protected confirmDialogTitleKey(): string {
    const action = this.confirmAction();
    switch (action?.kind) {
      case 'activate':
        return 'portal.team.activateConfirmTitle';
      case 'deactivate':
        return 'portal.team.deactivateConfirmTitle';
      case 'saveEdit':
        return 'portal.team.editConfirmTitle';
      case 'transfer':
        return 'portal.team.transferConfirmTitle';
      default:
        return 'portal.confirm.confirm';
    }
  }

  protected confirmDialogMessageKey(): string {
    const action = this.confirmAction();
    switch (action?.kind) {
      case 'activate':
        return 'portal.team.activateConfirmMessage';
      case 'deactivate':
        return 'portal.team.deactivateConfirmMessage';
      case 'saveEdit':
        return 'portal.team.editConfirmMessage';
      case 'transfer':
        return 'portal.team.transferConfirmMessage';
      default:
        return 'portal.confirm.confirm';
    }
  }

  protected runConfirmedAction(): void {
    const action = this.confirmAction();
    const schoolId = this.schoolId();
    if (!action || !schoolId) {
      return;
    }

    if (action.kind === 'transfer') {
      this.executeTransfer(schoolId);
      return;
    }

    const membershipId = action.member.membershipId;
    if (!membershipId) {
      return;
    }

    if (action.kind === 'saveEdit') {
      this.executeUpdate(schoolId, membershipId, this.editForm.getRawValue());
      return;
    }

    const isActive = action.kind === 'activate';
    this.executeUpdate(schoolId, membershipId, {
      role: action.member.role ?? SchoolTeamRole.SchoolAdmin,
      branchScopeMode: action.member.branchScopeMode ?? SchoolBranchScopeMode.AllBranches,
      branchIds: [...(action.member.allowedBranchIds ?? [])],
      isActive,
    });
  }

  private executeUpdate(
    schoolId: string,
    membershipId: string,
    raw: {
      role: SchoolTeamRole;
      branchScopeMode: SchoolBranchScopeMode;
      branchIds: string[];
      isActive: boolean;
    },
  ): void {
    const supportsScope = roleSupportsBranchScope(raw.role);
    const branchScopeMode = supportsScope ? raw.branchScopeMode : SchoolBranchScopeMode.AllBranches;
    const branchIds =
      supportsScope && branchScopeMode === SchoolBranchScopeMode.SelectedBranches ? raw.branchIds : [];

    this.saving.set(true);
    this.errorMessage.set(null);
    this.api
      .updateTeamMember(schoolId, membershipId, {
        role: raw.role,
        branchScopeMode,
        branchIds,
        isActive: raw.isActive,
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.confirmAction.set(null);
        if (result.succeeded) {
          this.editingMemberId.set(null);
          this.loadTeam();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private executeTransfer(schoolId: string): void {
    const email = this.transferForm.getRawValue().newOwnerEmail.trim();
    this.saving.set(true);
    this.errorMessage.set(null);
    this.api
      .transferOwnership(schoolId, { newOwnerEmail: email })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.confirmAction.set(null);
        if (result.succeeded) {
          this.transferOpen.set(false);
          this.context.loadAccessibleSchools().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
          this.loadTeam();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private onRoleChanged(role: SchoolTeamRole, form: 'add' | 'edit'): void {
    const group = form === 'add' ? this.addForm : this.editForm;
    if (!roleSupportsBranchScope(role)) {
      group.controls.branchScopeMode.setValue(SchoolBranchScopeMode.AllBranches, { emitEvent: false });
      group.controls.branchIds.setValue([], { emitEvent: false });
    }
  }

  private loadPage(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.api
      .listBranches(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.branches.set(result.data);
        }
        this.loadTeam();
      });
  }

  private loadTeam(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api.getTeam(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.loading.set(false);
      if (result.succeeded && result.data) {
        this.team.set(result.data as readonly PortalTeamMemberDto[]);
        this.errorMessage.set(null);
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private schoolId(): string | null {
    return this.route.parent?.snapshot.paramMap.get('schoolId') ?? null;
  }
}
