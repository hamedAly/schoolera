import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  AdminUserListItemDto,
  AdminUserListItemDtoPagedResult,
} from '../../../../core/api-client/SwaggerClient.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-admin-users-page',
  imports: [
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    TranslocoPipe,
  ],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.scss',
})
export class AdminUsersPage implements OnInit {
  private readonly api = inject(AdminPlatformApi);
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly actionLoading = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly rowMessage = signal<{ userId: string; message: string; isError: boolean } | null>(
    null,
  );
  protected readonly page = signal<AdminUserListItemDtoPagedResult | null>(null);
  protected readonly pageNumber = signal(1);

  protected searchTerm = '';
  protected roleFilter = '';
  protected accountStatusFilter = '';

  protected readonly items = computed(() => this.page()?.items ?? []);
  protected readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? '');

  protected readonly roleOptions = [
    '',
    'Parent',
    'SchoolOwner',
    'SchoolAdmin',
    'PlatformAdmin',
    'SupportAgent',
  ] as const;

  protected readonly accountStatusOptions = ['', 'Active', 'Suspended', 'PendingVerification'] as const;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listUsers(
        this.searchTerm.trim() || undefined,
        this.roleFilter || undefined,
        this.accountStatusFilter || undefined,
        this.pageNumber(),
        PAGE_SIZE,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.page.set(result.data);
          return;
        }
        this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected applyFilters(): void {
    this.pageNumber.set(1);
    this.load();
  }

  protected previousPage(): void {
    if (this.page()?.hasPreviousPage) {
      this.pageNumber.update((n) => n - 1);
      this.load();
    }
  }

  protected nextPage(): void {
    if (this.page()?.hasNextPage) {
      this.pageNumber.update((n) => n + 1);
      this.load();
    }
  }

  protected isSelf(user: AdminUserListItemDto): boolean {
    return !!user.id && user.id === this.currentUserId();
  }

  protected canSuspend(user: AdminUserListItemDto): boolean {
    return !this.isSelf(user) && user.accountStatus === 'Active';
  }

  protected canActivate(user: AdminUserListItemDto): boolean {
    return !this.isSelf(user) && user.accountStatus === 'Suspended';
  }

  protected suspendUser(user: AdminUserListItemDto): void {
    this.updateStatus(user, 'Suspended');
  }

  protected activateUser(user: AdminUserListItemDto): void {
    this.updateStatus(user, 'Active');
  }

  protected accountStatusKey(status: string | undefined): string {
    return status ? `admin.accountStatus.${status}` : 'admin.common.unknown';
  }

  private updateStatus(user: AdminUserListItemDto, accountStatus: string): void {
    if (!user.id || this.isSelf(user)) {
      return;
    }

    this.actionLoading.set(user.id);
    this.rowMessage.set(null);

    this.api
      .updateUserStatus(user.id, { accountStatus })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.actionLoading.set(null);
        if (result.succeeded) {
          this.load();
          this.rowMessage.set({
            userId: user.id!,
            message: this.transloco.translate('admin.users.statusUpdated'),
            isError: false,
          });
          return;
        }
        this.rowMessage.set({
          userId: user.id!,
          message: translateAdminErrorCodes(this.transloco, result.errorCodes),
          isError: true,
        });
      });
  }
}
