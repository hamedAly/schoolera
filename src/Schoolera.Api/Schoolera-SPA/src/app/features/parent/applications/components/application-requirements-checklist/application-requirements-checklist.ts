import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { ChildDocumentDto } from '../../../../../core/api-client/SwaggerClient.service';
import {
  AdmissionRequirementChecklistItem,
  AdmissionRequirementKind,
  profileFieldEditLink,
} from '../../../data-access/admission-requirements.models';

@Component({
  selector: 'se-application-requirements-checklist',
  imports: [RouterLink, TranslocoPipe],
  templateUrl: './application-requirements-checklist.html',
  styleUrl: './application-requirements-checklist.scss',
})
export class ApplicationRequirementsChecklist {
  readonly requirements = input<readonly AdmissionRequirementChecklistItem[]>([]);
  readonly childProfileId = input<string | null>(null);
  readonly returnUrl = input('/parent/applications');
  readonly canUpload = input(false);
  readonly uploading = input(false);
  readonly readOnly = input(false);
  readonly vaultDocuments = input<readonly ChildDocumentDto[]>([]);
  readonly copyingFromVault = input(false);

  readonly uploadRequested = output<{
    file: File;
    requirement: AdmissionRequirementChecklistItem;
  }>();
  readonly copyFromVaultRequested = output<{
    childDocumentId: string;
    requirement: AdmissionRequirementChecklistItem;
  }>();

  protected readonly sortedRequirements = computed(() =>
    [...this.requirements()].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
  );

  protected readonly RequirementKind = AdmissionRequirementKind;

  protected profileLink(
    requirement: AdmissionRequirementChecklistItem,
  ): { route: string[]; queryParams: { returnUrl: string } } | null {
    if (requirement.isComplete) {
      return null;
    }

    return profileFieldEditLink(
      requirement.profileFieldCode,
      this.childProfileId(),
      this.returnUrl(),
    );
  }

  protected kindKey(kind: number | undefined): string {
    switch (kind) {
      case AdmissionRequirementKind.InformationalText:
        return 'parent.applications.wizard.requirements.kinds.informational';
      case AdmissionRequirementKind.ParentProfileField:
        return 'parent.applications.wizard.requirements.kinds.parentProfile';
      case AdmissionRequirementKind.ChildProfileField:
        return 'parent.applications.wizard.requirements.kinds.childProfile';
      case AdmissionRequirementKind.ApplicationDocument:
        return 'parent.applications.wizard.requirements.kinds.document';
      default:
        return 'parent.applications.wizard.requirements.kinds.other';
    }
  }

  protected reasonKey(reasonCode: string | null | undefined): string | null {
    if (!reasonCode) {
      return null;
    }

    const key = `parent.applications.wizard.requirements.reasons.${reasonCode.replace(/\./g, '_')}`;
    return key;
  }

  protected extensionsLabel(requirement: AdmissionRequirementChecklistItem): string {
    const extensions = requirement.allowedFileExtensions ?? [];
    return extensions.length ? extensions.join(', ') : '—';
  }

  protected onFileSelected(event: Event, requirement: AdmissionRequirementChecklistItem): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = '';
    if (!file || this.uploading()) {
      return;
    }

    this.uploadRequested.emit({ file, requirement });
  }

  protected copyFromVault(
    requirement: AdmissionRequirementChecklistItem,
    childDocumentId: string,
  ): void {
    if (!childDocumentId || this.copyingFromVault()) {
      return;
    }

    this.copyFromVaultRequested.emit({ childDocumentId, requirement });
  }
}
