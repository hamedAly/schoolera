import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';
import {
  CreateSchoolAdmissionQuestionRequest,
  SchoolAdmissionQuestionDetailDto,
  SchoolAdmissionQuestionListItemDto,
  SchoolAdmissionQuestionOptionDto,
  TaxonomyItemDto,
  UpdateSchoolAdmissionQuestionRequest,
} from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import {
  AdmissionQuestionPublicationStatus,
  AdmissionQuestionType,
} from '../../../parent/data-access/admission-questions.models';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { ListSchoolAdmissionQuestionsParams } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

const DEFAULT_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png', '.webp'];

@Component({
  selector: 'se-portal-admission-questions-page',
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
  templateUrl: './portal-admission-questions-page.html',
  styleUrl: './portal-admission-questions-page.scss',
})
export class PortalAdmissionQuestionsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly QuestionType = AdmissionQuestionType;
  protected readonly PublicationStatus = AdmissionQuestionPublicationStatus;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<readonly SchoolAdmissionQuestionListItemDto[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingId = signal<string | null>(null);
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
    questionType: [''],
    publicationStatus: [''],
    isActive: [''],
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    questionCode: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
    labelAr: ['', Validators.required],
    labelEn: ['', Validators.required],
    helpAr: [''],
    helpEn: [''],
    questionType: [AdmissionQuestionType.ShortText as number, Validators.required],
    isRequired: [true],
    sortOrder: [0, Validators.min(0)],
    schoolBranchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
    minLength: [null as number | null],
    maxLength: [null as number | null],
    minSelectedOptions: [null as number | null],
    maxSelectedOptions: [null as number | null],
    minDate: [''],
    maxDate: [''],
    allowedFileExtensions: ['.pdf,.jpg,.jpeg,.png,.webp'],
    maxFileSizeBytes: [10 * 1024 * 1024],
    allowChildVaultCopy: [true],
    options: this.formBuilder.array([]),
  });

  protected readonly isEditMode = computed(() => !!this.editingId());
  protected readonly sortedItems = computed(() =>
    [...this.items()].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
  );

  protected get optionsArray(): FormArray {
    return this.form.controls.options;
  }

  ngOnInit(): void {
    this.loadLookups();
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
    this.form.controls.questionType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        if (
          type === AdmissionQuestionType.SingleChoice ||
          type === AdmissionQuestionType.MultipleChoice
        ) {
          if (!this.optionsArray.length) {
            this.addOption();
          }
        }
      });
  }

  protected label(item: SchoolAdmissionQuestionListItemDto): string {
    return localizedBilingualName(item.labelAr ?? '', item.labelEn ?? '', this.activeLang());
  }

  protected typeLabel(type: number | undefined): string {
    const key = this.typeKey(type);
    return key ? this.transloco.translate(key) : '—';
  }

  protected statusKey(item: SchoolAdmissionQuestionListItemDto): string {
    if (item.isActive === false) {
      return 'portal.admissionQuestions.inactive';
    }
    return item.publicationStatus === AdmissionQuestionPublicationStatus.Published
      ? 'portal.admissionQuestions.published'
      : 'portal.admissionQuestions.draft';
  }

  protected openCreate(): void {
    this.editingId.set(null);
    this.clearOptions();
    this.form.reset({
      questionType: AdmissionQuestionType.ShortText,
      isRequired: true,
      sortOrder: this.items().length,
      allowedFileExtensions: DEFAULT_EXTENSIONS.join(','),
      maxFileSizeBytes: 10 * 1024 * 1024,
      allowChildVaultCopy: true,
    });
    this.form.controls.questionCode.enable();
    this.showForm.set(true);
    this.errorMessage.set(null);
  }

  protected openEdit(item: SchoolAdmissionQuestionListItemDto): void {
    const schoolId = this.schoolId();
    if (!schoolId || !item.id) {
      return;
    }
    this.api
      .getAdmissionQuestion(schoolId, item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.patchFormFromDetail(result.data);
        this.editingId.set(item.id!);
        this.form.controls.questionCode.disable();
        this.showForm.set(true);
        this.errorMessage.set(null);
      });
  }

  protected cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
    this.form.controls.questionCode.enable();
    this.clearOptions();
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
      ? this.api.updateAdmissionQuestion(
          schoolId,
          editingId,
          body as UpdateSchoolAdmissionQuestionRequest,
        )
      : this.api.createAdmissionQuestion(schoolId, body as CreateSchoolAdmissionQuestionRequest);
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

  protected publish(item: SchoolAdmissionQuestionListItemDto): void {
    this.runMutation((schoolId) => this.api.publishAdmissionQuestion(schoolId, item.id!));
  }

  protected unpublish(item: SchoolAdmissionQuestionListItemDto): void {
    this.runMutation((schoolId) => this.api.unpublishAdmissionQuestion(schoolId, item.id!));
  }

  protected deactivate(item: SchoolAdmissionQuestionListItemDto): void {
    this.runMutation((schoolId) => this.api.deactivateAdmissionQuestion(schoolId, item.id!));
  }

  protected moveItem(item: SchoolAdmissionQuestionListItemDto, direction: -1 | 1): void {
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
      .reorderAdmissionQuestions(schoolId, {
        orderedQuestionIds: reordered.map((entry) => entry.id!).filter(Boolean),
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

  protected retry(): void {
    this.errorMessage.set(null);
    this.loadItems();
  }

  protected showTextFields(): boolean {
    const type = this.form.controls.questionType.value;
    return type === AdmissionQuestionType.ShortText || type === AdmissionQuestionType.LongText;
  }

  protected showChoiceFields(): boolean {
    const type = this.form.controls.questionType.value;
    return (
      type === AdmissionQuestionType.SingleChoice || type === AdmissionQuestionType.MultipleChoice
    );
  }

  protected showDateFields(): boolean {
    return this.form.controls.questionType.value === AdmissionQuestionType.Date;
  }

  protected showFileFields(): boolean {
    return this.form.controls.questionType.value === AdmissionQuestionType.File;
  }

  protected addOption(): void {
    this.optionsArray.push(
      this.formBuilder.nonNullable.group({
        optionCode: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
        labelAr: ['', Validators.required],
        labelEn: ['', Validators.required],
        sortOrder: [this.optionsArray.length],
        isActive: [true],
      }),
    );
  }

  protected removeOption(index: number): void {
    this.optionsArray.removeAt(index);
    this.reindexOptions();
  }

  protected moveOption(index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= this.optionsArray.length) {
      return;
    }
    const current = this.optionsArray.at(index);
    this.optionsArray.removeAt(index);
    this.optionsArray.insert(target, current);
    this.reindexOptions();
  }

  private typeKey(type: number | undefined): string | null {
    switch (type) {
      case AdmissionQuestionType.ShortText:
        return 'portal.admissionQuestions.types.shortText';
      case AdmissionQuestionType.LongText:
        return 'portal.admissionQuestions.types.longText';
      case AdmissionQuestionType.SingleChoice:
        return 'portal.admissionQuestions.types.singleChoice';
      case AdmissionQuestionType.MultipleChoice:
        return 'portal.admissionQuestions.types.multipleChoice';
      case AdmissionQuestionType.Date:
        return 'portal.admissionQuestions.types.date';
      case AdmissionQuestionType.YesNo:
        return 'portal.admissionQuestions.types.yesNo';
      case AdmissionQuestionType.File:
        return 'portal.admissionQuestions.types.file';
      default:
        return null;
    }
  }

  private runMutation(
    factory: (schoolId: string) => ReturnType<SchoolPortalApi['publishAdmissionQuestion']>,
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

  private loadItems(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api
      .listAdmissionQuestions(schoolId, this.buildListParams())
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

  private buildListParams(): ListSchoolAdmissionQuestionsParams {
    const raw = this.filterForm.getRawValue();
    return {
      branchId: raw.branchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
      questionType: raw.questionType ? Number(raw.questionType) : undefined,
      publicationStatus: raw.publicationStatus ? Number(raw.publicationStatus) : undefined,
      isActive: raw.isActive === '' ? undefined : raw.isActive === 'true',
    };
  }

  private patchFormFromDetail(detail: SchoolAdmissionQuestionDetailDto): void {
    if (detail.educationalStageId) {
      this.taxonomiesApi.getGradesByStage(detail.educationalStageId).subscribe((result) => {
        if (result.succeeded && result.data) {
          this.grades.set(result.data);
        }
      });
    }
    this.clearOptions();
    for (const option of detail.options ?? []) {
      this.optionsArray.push(
        this.formBuilder.nonNullable.group({
          optionCode: [
            option.optionCode ?? '',
            [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)],
          ],
          labelAr: [option.labelAr ?? '', Validators.required],
          labelEn: [option.labelEn ?? '', Validators.required],
          sortOrder: [option.sortOrder ?? 0],
          isActive: [option.isActive ?? true],
        }),
      );
    }
    this.form.patchValue({
      questionCode: detail.questionCode ?? '',
      labelAr: detail.labelAr ?? '',
      labelEn: detail.labelEn ?? '',
      helpAr: detail.helpAr ?? '',
      helpEn: detail.helpEn ?? '',
      questionType: detail.questionType ?? AdmissionQuestionType.ShortText,
      isRequired: detail.isRequired ?? true,
      sortOrder: detail.sortOrder ?? 0,
      schoolBranchId: detail.schoolBranchId ?? '',
      educationalStageId: detail.educationalStageId ?? '',
      gradeId: detail.gradeId ?? '',
      academicYearId: detail.academicYearId ?? '',
      minLength: detail.minLength ?? null,
      maxLength: detail.maxLength ?? null,
      minSelectedOptions: detail.minSelectedOptions ?? null,
      maxSelectedOptions: detail.maxSelectedOptions ?? null,
      minDate: detail.minDate ?? '',
      maxDate: detail.maxDate ?? '',
      allowedFileExtensions: (detail.allowedFileExtensions ?? DEFAULT_EXTENSIONS).join(','),
      maxFileSizeBytes: detail.maxFileSizeBytes ?? 10 * 1024 * 1024,
      allowChildVaultCopy: detail.allowChildVaultCopy ?? true,
    });
  }

  private buildBodyFromForm(): CreateSchoolAdmissionQuestionRequest | UpdateSchoolAdmissionQuestionRequest {
    const raw = this.form.getRawValue();
    const questionType = Number(raw.questionType);
    const extensions = raw.allowedFileExtensions
      .split(',')
      .map((value) => value.trim())
      .filter(Boolean);
    const options: SchoolAdmissionQuestionOptionDto[] = this.optionsArray.controls.map((ctrl) => {
      const optionRaw = ctrl.getRawValue();
      return {
        optionCode: optionRaw.optionCode.trim(),
        labelAr: optionRaw.labelAr.trim(),
        labelEn: optionRaw.labelEn.trim(),
        sortOrder: optionRaw.sortOrder,
        isActive: optionRaw.isActive,
      };
    });
    const shared = {
      labelAr: raw.labelAr.trim(),
      labelEn: raw.labelEn.trim(),
      helpAr: raw.helpAr.trim() || undefined,
      helpEn: raw.helpEn.trim() || undefined,
      isRequired: raw.isRequired,
      sortOrder: raw.sortOrder,
      schoolBranchId: raw.schoolBranchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
      minLength: this.showTextFields() && raw.minLength != null ? raw.minLength : undefined,
      maxLength: this.showTextFields() && raw.maxLength != null ? raw.maxLength : undefined,
      minSelectedOptions:
        this.showChoiceFields() && raw.minSelectedOptions != null
          ? raw.minSelectedOptions
          : undefined,
      maxSelectedOptions:
        this.showChoiceFields() && raw.maxSelectedOptions != null
          ? raw.maxSelectedOptions
          : undefined,
      minDate: this.showDateFields() && raw.minDate ? raw.minDate : undefined,
      maxDate: this.showDateFields() && raw.maxDate ? raw.maxDate : undefined,
      allowedFileExtensions: this.showFileFields() ? extensions : undefined,
      maxFileSizeBytes: this.showFileFields() ? raw.maxFileSizeBytes : undefined,
      allowChildVaultCopy: raw.allowChildVaultCopy,
      options: this.showChoiceFields() ? options : undefined,
    };

    if (this.editingId()) {
      return shared;
    }
    return {
      questionCode: raw.questionCode.trim(),
      questionType,
      ...shared,
    };
  }

  private clearOptions(): void {
    while (this.optionsArray.length) {
      this.optionsArray.removeAt(0);
    }
  }

  private reindexOptions(): void {
    this.optionsArray.controls.forEach((ctrl, index) => {
      ctrl.patchValue({ sortOrder: index });
    });
  }
}
