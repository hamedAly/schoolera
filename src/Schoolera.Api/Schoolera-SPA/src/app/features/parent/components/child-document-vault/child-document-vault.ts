import { Component, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  ChildDocumentDto,
  ChildDocumentType,
} from '../../../../core/api-client/SwaggerClient.service';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import { ParentApi } from '../../data-access/parent.api';

export const CHILD_DOCUMENT_MAX_BYTES = 10 * 1024 * 1024;

@Component({
  selector: 'se-child-document-vault',
  imports: [ConfirmationDialog, TranslocoPipe],
  templateUrl: './child-document-vault.html',
  styleUrl: './child-document-vault.scss',
})
export class ChildDocumentVault implements OnInit {
  readonly childId = input.required<string>();

  private readonly api = inject(ParentApi);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly documents = signal<ChildDocumentDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly uploadPercent = signal(0);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly pendingDelete = signal<ChildDocumentDto | null>(null);

  protected readonly documentTypes = [
    { value: ChildDocumentType._1, labelKey: 'parent.documentVault.types.birthCertificate' },
    { value: ChildDocumentType._2, labelKey: 'parent.documentVault.types.childPhoto' },
    { value: ChildDocumentType._3, labelKey: 'parent.documentVault.types.previousSchoolCertificate' },
    { value: ChildDocumentType._4, labelKey: 'parent.documentVault.types.medicalReport' },
    { value: ChildDocumentType._99, labelKey: 'parent.documentVault.types.otherApproved' },
  ] as const;

  ngOnInit(): void {
    this.load();
  }

  protected documentsOfType(type: ChildDocumentType): ChildDocumentDto[] {
    return this.documents().filter((document) => document.documentType === type);
  }

  protected formatBytes(bytes: number | undefined): string {
    const value = bytes ?? 0;
    if (value < 1024) {
      return `${value} B`;
    }
    if (value < 1024 * 1024) {
      return `${(value / 1024).toFixed(1)} KB`;
    }
    return `${(value / (1024 * 1024)).toFixed(1)} MB`;
  }

  protected selectUpload(type: ChildDocumentType, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || this.busy() || !this.validateFile(file)) {
      return;
    }

    this.runUpload(this.api.uploadChildDocumentWithProgress(this.childId(), file, type));
  }

  protected selectReplacement(document: ChildDocumentDto, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (
      !file ||
      !document.id ||
      document.documentType === undefined ||
      this.busy() ||
      !this.validateFile(file)
    ) {
      return;
    }

    this.runUpload(
      this.api.replaceChildDocumentWithProgress(
        this.childId(),
        document.id,
        file,
        document.documentType,
      ),
    );
  }

  protected download(document: ChildDocumentDto): void {
    if (!document.id || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.api
      .downloadChildDocument(
        this.childId(),
        document.id,
        document.originalFileName ?? 'document',
      )
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () => this.toast.error(this.transloco.translate('parent.errors.generic')),
      });
  }

  protected confirmDelete(): void {
    const document = this.pendingDelete();
    if (!document?.id || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.api
      .deleteChildDocument(this.childId(), document.id)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.pendingDelete.set(null);
        if (!result.succeeded) {
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.documents.update((items) => items.filter((item) => item.id !== document.id));
        this.toast.success(this.transloco.translate('parent.documentVault.deleteSuccess'));
      });
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .listChildDocuments(this.childId())
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.documents.set(result.data ?? []);
          return;
        }
        this.errorKey.set('parent.documentVault.loadFailed');
      });
  }

  private validateFile(file: File): boolean {
    this.errorKey.set(null);
    if (file.size > CHILD_DOCUMENT_MAX_BYTES) {
      this.errorKey.set('parent.documentVault.fileTooLarge');
      return false;
    }
    if (!/^(application\/pdf|image\/(jpeg|png|webp))$/i.test(file.type)) {
      this.errorKey.set('parent.documentVault.invalidFileType');
      return false;
    }
    return true;
  }

  private runUpload(request: ReturnType<ParentApi['uploadChildDocumentWithProgress']>): void {
    this.busy.set(true);
    this.uploadPercent.set(0);
    request
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (event) => {
          if (event.kind === 'progress') {
            this.uploadPercent.set(event.percent);
            return;
          }
          if (!event.result.succeeded || !event.result.data) {
            this.toast.error(translateParentErrorCodes(this.transloco, event.result.errorCodes));
            return;
          }
          const saved = event.result.data;
          this.documents.update((items) => [
            ...items.filter((item) => item.id !== saved.id),
            saved,
          ]);
          this.toast.success(this.transloco.translate('parent.documentVault.uploadSuccess'));
        },
        error: () => this.toast.error(this.transloco.translate('parent.errors.generic')),
      });
  }
}
