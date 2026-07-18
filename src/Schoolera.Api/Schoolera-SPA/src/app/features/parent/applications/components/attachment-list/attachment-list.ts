import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';

import {
  AdmissionAttachmentDto,
  AdmissionAttachmentType,
  ChildDocumentDto,
  ChildDocumentType,
} from '../../../../../core/api-client/SwaggerClient.service';

/** Client-side convenience limit; backend PrivateFileStorage remains authoritative (10 MB). */
export const ADMISSION_ATTACHMENT_MAX_BYTES = 10 * 1024 * 1024;

@Component({
  selector: 'se-attachment-list',
  imports: [FormsModule, TranslocoPipe],
  templateUrl: './attachment-list.html',
  styleUrl: './attachment-list.scss',
})
export class AttachmentList {
  readonly attachments = input<ReadonlyArray<AdmissionAttachmentDto>>([]);
  readonly canUpload = input(false);
  readonly canRemove = input(false);
  readonly uploading = input(false);
  readonly uploadPercent = input(0);
  readonly canCopyFromVault = input(false);
  readonly vaultDocuments = input<ReadonlyArray<ChildDocumentDto>>([]);
  readonly copyingFromVault = input(false);

  readonly uploadRequested = output<{ file: File; attachmentType: number }>();
  readonly removeRequested = output<string>();
  readonly downloadRequested = output<AdmissionAttachmentDto>();
  readonly copyFromVaultRequested = output<string>();

  protected readonly selectedType = signal<number>(AdmissionAttachmentType._1);
  protected readonly clientError = signal<string | null>(null);
  protected readonly selectedVaultDocumentId = signal('');

  protected readonly attachmentTypes = [
    { value: AdmissionAttachmentType._1, labelKey: 'parent.applications.attachmentTypes.supportingDocument' },
    { value: AdmissionAttachmentType._2, labelKey: 'parent.applications.attachmentTypes.birthCertificate' },
    { value: AdmissionAttachmentType._3, labelKey: 'parent.applications.attachmentTypes.previousSchoolReport' },
    { value: AdmissionAttachmentType._99, labelKey: 'parent.applications.attachmentTypes.other' },
  ] as const;

  protected typeLabelKey(type: AdmissionAttachmentType | number | undefined): string {
    const match = this.attachmentTypes.find((item) => item.value === type);
    return match?.labelKey ?? 'parent.applications.attachmentTypes.other';
  }

  protected vaultTypeLabelKey(type: ChildDocumentType | number | undefined): string {
    switch (type) {
      case ChildDocumentType._1:
        return 'parent.documentVault.types.birthCertificate';
      case ChildDocumentType._2:
        return 'parent.documentVault.types.childPhoto';
      case ChildDocumentType._3:
        return 'parent.documentVault.types.previousSchoolCertificate';
      case ChildDocumentType._4:
        return 'parent.documentVault.types.medicalReport';
      default:
        return 'parent.documentVault.types.otherApproved';
    }
  }

  protected copyFromVault(): void {
    const id = this.selectedVaultDocumentId();
    if (id && !this.copyingFromVault()) {
      this.copyFromVaultRequested.emit(id);
    }
  }

  protected onFileSelected(event: Event): void {
    this.clientError.set(null);
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || this.uploading()) {
      return;
    }

    if (file.size > ADMISSION_ATTACHMENT_MAX_BYTES) {
      this.clientError.set('parent.applications.wizard.attachmentTooLarge');
      return;
    }

    const allowed = /^(application\/pdf|image\/(jpeg|png|webp))$/i.test(file.type);
    if (!allowed) {
      this.clientError.set('parent.applications.wizard.attachmentInvalidType');
      return;
    }

    this.uploadRequested.emit({ file, attachmentType: this.selectedType() });
  }

  protected remove(id: string | undefined): void {
    if (id && this.canRemove() && !this.uploading()) {
      this.removeRequested.emit(id);
    }
  }

  protected download(attachment: AdmissionAttachmentDto): void {
    this.downloadRequested.emit(attachment);
  }
}
