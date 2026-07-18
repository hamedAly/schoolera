import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TaxonomyItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import {
  AdmissionRequirementKind,
  AdmissionRequirementPublicationStatus,
  PublicAdmissionRequirementSummary,
} from '../../../parent/data-access/admission-requirements.models';
import { SchoolsApi } from '../../../schools/data-access/schools.api';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import {
  CreateSchoolAdmissionRequirementBody,
  ListSchoolAdmissionRequirementsParams,
  SchoolAdmissionRequirementDetail,
  SchoolAdmissionRequirementListItem,
  UpdateSchoolAdmissionRequirementBody,
} from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';
const DEFAULT_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png', '.webp'];
@Component({
  selector: 'se-portal-admission-requirements-page',
  imports: [
    BilingualFieldGroup,
    Button,
    FormField,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-admission-requirements-page.html',
  styleUrl: './portal-admission-requirements-page.scss',
})
export class PortalAdmissionRequirementsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly schoolsApi = inject(SchoolsApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly RequirementKind = AdmissionRequirementKind;
  protected readonly PublicationStatus = AdmissionRequirementPublicationStatus;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly previewLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<readonly SchoolAdmissionRequirementListItem[]>([]);
  protected readonly previewItems = signal<readonly PublicAdmissionRequirementSummary[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly schoolSlug = signal<string | null>(null);
  protected readonly branches = signal<readonly { id: string; name: string }[]>([]);
  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);
  protected readonly academicYears = signal<TaxonomyItemDto[]>([]);
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());
  protected readonly filterForm = this.formBuilder.nonNullable.group({
    branchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
    kind: [''],
    publicationStatus: [''],
    isActive: [''],
  });
  protected readonly form = this.formBuilder.nonNullable.group({
    requirementCode: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
    nameAr: ['', Validators.required],
    nameEn: ['', Validators.required],
    descriptionAr: [''],
    descriptionEn: [''],
    kind: [AdmissionRequirementKind.ApplicationDocument as number, Validators.required],
    isRequired: [true],
    sortOrder: [0, Validators.min(0)],
    schoolBranchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
    profileFieldCode: [''],
    documentCode: [1],
    allowedFileExtensions: ['.pdf,.jpg,.jpeg,.png,.webp'],
    maxFileSizeBytes: [10 * 1024 * 1024],
    allowChildVaultCopy: [true],
  });
  protected readonly isEditMode = computed(() => !!this.editingId());
  protected readonly sortedItems = computed(() =>
    [...this.items()].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
  );
  ngOnInit(): void {
    this.loadLookups();
    this.loadProfileSlug();
    this.loadItems();
    this.filterForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadItems();
    });
    this.form.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((stageId) => {
        this.form.controls.gradeId.setValue('');
        this.grades.set([]);
        if (stageId) {
          this.taxonomiesApi.getGradesByStage(stageId).subscribe((result) => {
            if (result.succeeded && result.data) {
              this.grades.set(result.data);
            }
          });
        }
      });
  }
  protected label(item: SchoolAdmissionRequirementListItem): string {
    return localizedBilingualName(item.nameAr ?? '', item.nameEn ?? '', this.activeLang());
  }
  protected kindLabel(kind: number | undefined): string {
    switch (kind) {
      case AdmissionRequirementKind.InformationalText:
        return this.transloco.translate('portal.admissionRequirements.kinds.informational');
      case AdmissionRequirementKind.ParentProfileField:
        return this.transloco.translate('portal.admissionRequirements.kinds.parentProfile');
      case AdmissionRequirementKind.ChildProfileField:
        return this.transloco.translate('portal.admissionRequirements.kinds.childProfile');
      case AdmissionRequirementKind.ApplicationDocument:
        return this.transloco.translate('portal.admissionRequirements.kinds.document');
      default:
        return 'â€”';
    }
  }
  protected statusKey(item: SchoolAdmissionRequirementListItem): string {
    if (item.isActive === false) {
      return 'portal.admissionRequirements.inactive';
    }
    return item.publicationStatus === AdmissionRequirementPublicationStatus.Published
      ? 'portal.admissionRequirements.published'
      : 'portal.admissionRequirements.draft';
  }
  protected openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      kind: AdmissionRequirementKind.ApplicationDocument,
      isRequired: true,
      sortOrder: this.items().length,
      documentCode: 1,
      allowedFileExtensions: DEFAULT_EXTENSIONS.join(','),
      maxFileSizeBytes: 10 * 1024 * 1024,
      allowChildVaultCopy: true,
    });
    this.form.controls.requirementCode.enable();
    this.showForm.set(true);
    this.errorMessage.set(null);
  }
  protected openEdit(item: SchoolAdmissionRequirementListItem): void {
    const schoolId = this.schoolId();
    if (!schoolId || !item.id) {
      return;
    }
    this.api
      .getAdmissionRequirement(schoolId, item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.patchFormFromDetail(result.data);
        this.editingId.set(item.id!);
        this.form.controls.requirementCode.disable();
        this.showForm.set(true);
        this.errorMessage.set(null);
      });
  }
  protected cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
    this.form.controls.requirementCode.enable();
  }
  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    const body = this.buildBodyFromForm();
    const editingId = this.editingId();
    this.saving.set(true);
    const request$ = editingId
      ? this.api.updateAdmissionRequirement(schoolId, editingId, body as UpdateSchoolAdmissionRequirementBody)
      : this.api.createAdmissionRequirement(schoolId, body as CreateSchoolAdmissionRequirementBody);
    request$
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.cancelForm();
          this.loadItems();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
  protected publish(item: SchoolAdmissionRequirementListItem): void {
    this.runMutation((schoolId) => this.api.publishAdmissionRequirement(schoolId, item.id!));
  }
  protected unpublish(item: SchoolAdmissionRequirementListItem): void {
    this.runMutation((schoolId) => this.api.unpublishAdmissionRequirement(schoolId, item.id!));
  }
  protected deactivate(item: SchoolAdmissionRequirementListItem): void {
    this.runMutation((schoolId) => this.api.deactivateAdmissionRequirement(schoolId, item.id!));
  }
  protected moveItem(item: SchoolAdmissionRequirementListItem, direction: -1 | 1): void {
    const schoolId = this.schoolId();
    if (!schoolId || !item.id) {
      return;
    }
    const sorted = this.sortedItems();
    const index = sorted.findIndex((entry) => entry.id === item.id);
    const targetIndex = index + direction;
    if (index < 0 || targetIndex < 0 || targetIndex >= sorted.length) {
      return;
    }
    const reordered = [...sorted];
    const [removed] = reordered.splice(index, 1);
    reordered.splice(targetIndex, 0, removed);
    this.api
      .reorderAdmissionRequirements(schoolId, {
        orderedRequirementIds: reordered.map((entry) => entry.id!).filter(Boolean),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.loadItems();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
  protected loadPreview(): void {
    const slug = this.schoolSlug();
    if (!slug) {
      return;
    }
    const filters = this.filterForm.getRawValue();
    this.previewLoading.set(true);
    this.schoolsApi
      .getAdmissionRequirements(slug, {
        branchId: filters.branchId || undefined,
        educationalStageId: filters.educationalStageId || undefined,
        gradeId: filters.gradeId || undefined,
        academicYearId: filters.academicYearId || undefined,
      })
      .pipe(finalize(() => this.previewLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.previewItems.set(result.data ?? []);
          return;
        }
        this.errorMessage.set(this.transloco.translate('portal.admissionRequirements.previewFailed'));
      });
  }
  protected retry(): void {
    this.errorMessage.set(null);
    this.loadItems();
  }
  protected showProfileField(): boolean {
    const kind = this.form.controls.kind.value;
    return (
      kind === AdmissionRequirementKind.ParentProfileField ||
      kind === AdmissionRequirementKind.ChildProfileField
    );
  }
  protected showDocumentFields(): boolean {
    return this.form.controls.kind.value === AdmissionRequirementKind.ApplicationDocument;
  }
  private runMutation(
    factory: (
      schoolId: string,
    ) => Observable<{ succeeded?: boolean; errorCodes?: string[] | undefined }>,
  ): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    factory(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadItems();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }
  private schoolId(): string | null {
    return this.route.parent?.snapshot.paramMap.get('schoolId') ?? null;
  }
  private loadLookups(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    this.api.listBranches(schoolId).subscribe((result) => {
      if (result.succeeded && result.data) {
        this.branches.set(
          result.data
            .filter((branch) => branch.id)
            .map((branch) => ({
              id: branch.id!,
              name: localizedBilingualName(branch.nameAr ?? '', branch.nameEn, this.activeLang()),
            })),
        );
      }
    });
    this.taxonomiesApi.getEducationalStages().subscribe((result) => {
      if (result.succeeded && result.data) {
        this.stages.set(result.data);
      }
    });
    this.taxonomiesApi.getAcademicYears().subscribe((result) => {
      if (result.succeeded && result.data) {
        this.academicYears.set(result.data);
      }
    });
  }
  private loadProfileSlug(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    this.api.getProfile(schoolId).subscribe((result) => {
      if (result.succeeded && result.data?.slug) {
        this.schoolSlug.set(result.data.slug);
      }
    });
  }
  private loadItems(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api
      .listAdmissionRequirements(schoolId, this.buildListParams())
      .pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.items.set(result.data);
          this.errorMessage.set(null);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
  private buildListParams(): ListSchoolAdmissionRequirementsParams {
    const raw = this.filterForm.getRawValue();
    return {
      branchId: raw.branchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
      kind: raw.kind ? Number(raw.kind) : undefined,
      publicationStatus: raw.publicationStatus ? Number(raw.publicationStatus) : undefined,
      isActive: raw.isActive === '' ? undefined : raw.isActive === 'true',
    };
  }
  private patchFormFromDetail(detail: SchoolAdmissionRequirementDetail): void {
    if (detail.educationalStageId) {
      this.taxonomiesApi.getGradesByStage(detail.educationalStageId).subscribe((result) => {
        if (result.succeeded && result.data) {
          this.grades.set(result.data);
        }
      });
    }
    this.form.patchValue({
      requirementCode: detail.requirementCode ?? '',
      nameAr: detail.nameAr ?? '',
      nameEn: detail.nameEn ?? '',
      descriptionAr: detail.descriptionAr ?? '',
      descriptionEn: detail.descriptionEn ?? '',
      kind: detail.kind ?? AdmissionRequirementKind.ApplicationDocument,
      isRequired: detail.isRequired ?? true,
      sortOrder: detail.sortOrder ?? 0,
      schoolBranchId: detail.schoolBranchId ?? '',
      educationalStageId: detail.educationalStageId ?? '',
      gradeId: detail.gradeId ?? '',
      academicYearId: detail.academicYearId ?? '',
      profileFieldCode: detail.profileFieldCode != null ? String(detail.profileFieldCode) : '',
      documentCode: detail.documentCode ?? 1,
      allowedFileExtensions: (detail.allowedFileExtensions ?? DEFAULT_EXTENSIONS).join(','),
      maxFileSizeBytes: detail.maxFileSizeBytes ?? 10 * 1024 * 1024,
      allowChildVaultCopy: detail.allowChildVaultCopy ?? true,
    });
  }
  private buildBodyFromForm(): CreateSchoolAdmissionRequirementBody | UpdateSchoolAdmissionRequirementBody {
    const raw = this.form.getRawValue();
    const kind = Number(raw.kind);
    const extensions = raw.allowedFileExtensions
      .split(',')
      .map((value) => value.trim())
      .filter(Boolean);
    const shared = {
      nameAr: raw.nameAr.trim(),
      nameEn: raw.nameEn.trim(),
      descriptionAr: raw.descriptionAr.trim() || undefined,
      descriptionEn: raw.descriptionEn.trim() || undefined,
      isRequired: raw.isRequired,
      sortOrder: raw.sortOrder,
      schoolBranchId: raw.schoolBranchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
      profileFieldCode:
        kind === AdmissionRequirementKind.ParentProfileField ||
        kind === AdmissionRequirementKind.ChildProfileField
          ? raw.profileFieldCode
            ? Number(raw.profileFieldCode)
            : undefined
          : undefined,
      documentCode:
        kind === AdmissionRequirementKind.ApplicationDocument ? Number(raw.documentCode) : undefined,
      allowedFileExtensions:
        kind === AdmissionRequirementKind.ApplicationDocument ? extensions : undefined,
      maxFileSizeBytes:
        kind === AdmissionRequirementKind.ApplicationDocument ? raw.maxFileSizeBytes : undefined,
      allowChildVaultCopy: raw.allowChildVaultCopy,
    };
    if (this.editingId()) {
      return shared;
    }
    return {
      requirementCode: raw.requirementCode.trim(),
      kind,
      ...shared,
    };
  }
}
