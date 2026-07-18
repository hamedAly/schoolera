import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { TaxonomyItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { sanitizeHtml } from '../../../../shared/utils/sanitize-html';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import {
  CreateSchoolInterviewFaqRequest,
  InterviewFaqCategory,
  ListSchoolInterviewFaqsParams,
  SchoolInterviewFaqDetail,
  UpdateSchoolInterviewFaqRequest,
} from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

@Component({
  selector: 'se-portal-interview-faqs-page',
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
  templateUrl: './portal-interview-faqs-page.html',
  styleUrl: './portal-interview-faqs-page.scss',
})
export class PortalInterviewFaqsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly InterviewCategory = InterviewFaqCategory;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<readonly SchoolInterviewFaqDetail[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly editingRowVersion = signal<string | null>(null);
  protected readonly branches = signal<readonly { id: string; name: string }[]>([]);
  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);
  protected readonly academicYears = signal<TaxonomyItemDto[]>([]);
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly filterForm = this.formBuilder.nonNullable.group({
    interviewCategory: [''],
    branchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
    isPublished: [''],
    isActive: [''],
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    questionAr: ['', Validators.required],
    questionEn: ['', Validators.required],
    answerAr: ['', Validators.required],
    answerEn: ['', Validators.required],
    interviewCategory: [InterviewFaqCategory._1 as number, Validators.required],
    schoolBranchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
  });

  protected readonly isEditMode = computed(() => !!this.editingId());
  protected readonly sortedItems = computed(() =>
    [...this.items()].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
  );

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
  }

  protected label(item: SchoolInterviewFaqDetail): string {
    return localizedBilingualName(item.questionAr ?? '', item.questionEn ?? '', this.activeLang());
  }

  protected categoryLabel(category: number | undefined): string {
    switch (category) {
      case InterviewFaqCategory._1:
        return this.transloco.translate('portal.interviewFaqs.categories.interview');
      case InterviewFaqCategory._2:
        return this.transloco.translate('portal.interviewFaqs.categories.assessment');
      case InterviewFaqCategory._3:
        return this.transloco.translate('portal.interviewFaqs.categories.interviewAndAssessment');
      default:
        return '—';
    }
  }

  protected statusKey(item: SchoolInterviewFaqDetail): string {
    if (item.isActive === false) {
      return 'portal.interviewFaqs.inactive';
    }
    return item.isPublished ? 'portal.interviewFaqs.published' : 'portal.interviewFaqs.draft';
  }

  protected previewAnswer(item: SchoolInterviewFaqDetail): string {
    const html =
      this.activeLang() === 'en' ? (item.answerEn ?? '') : (item.answerAr ?? '');
    return sanitizeHtml(this.sanitizer, html);
  }

  protected openCreate(): void {
    this.editingId.set(null);
    this.editingRowVersion.set(null);
    this.form.reset({
      questionAr: '',
      questionEn: '',
      answerAr: '',
      answerEn: '',
      interviewCategory: InterviewFaqCategory._1,
      schoolBranchId: '',
      educationalStageId: '',
      gradeId: '',
      academicYearId: '',
    });
    this.showForm.set(true);
    this.errorMessage.set(null);
  }

  protected openEdit(item: SchoolInterviewFaqDetail): void {
    const schoolId = this.schoolId();
    if (!schoolId || !item.id) {
      return;
    }
    this.api
      .getInterviewFaq(schoolId, item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.patchFormFromDetail(result.data);
        this.editingId.set(item.id!);
        this.editingRowVersion.set(result.data.rowVersion ?? null);
        this.showForm.set(true);
        this.errorMessage.set(null);
      });
  }

  protected cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
    this.editingRowVersion.set(null);
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
      ? this.api.updateInterviewFaq(schoolId, editingId, body as UpdateSchoolInterviewFaqRequest)
      : this.api.createInterviewFaq(schoolId, body as CreateSchoolInterviewFaqRequest);
    request$
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.cancelForm();
          this.loadItems();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected publish(item: SchoolInterviewFaqDetail): void {
    this.runMutation((schoolId) => this.api.publishInterviewFaq(schoolId, item.id!));
  }

  protected unpublish(item: SchoolInterviewFaqDetail): void {
    this.runMutation((schoolId) => this.api.unpublishInterviewFaq(schoolId, item.id!));
  }

  protected activate(item: SchoolInterviewFaqDetail): void {
    this.runMutation((schoolId) => this.api.activateInterviewFaq(schoolId, item.id!));
  }

  protected deactivate(item: SchoolInterviewFaqDetail): void {
    this.runMutation((schoolId) => this.api.deactivateInterviewFaq(schoolId, item.id!));
  }

  protected moveItem(item: SchoolInterviewFaqDetail, direction: -1 | 1): void {
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
      .reorderInterviewFaqs(schoolId, {
        orderedIds: reordered.map((entry) => entry.id!).filter(Boolean),
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

  private loadItems(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api
      .listInterviewFaqs(schoolId, this.buildListParams())
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.items.set(result.data);
          this.errorMessage.set(null);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private buildListParams(): ListSchoolInterviewFaqsParams {
    const raw = this.filterForm.getRawValue();
    return {
      interviewCategory: raw.interviewCategory ? Number(raw.interviewCategory) : undefined,
      branchId: raw.branchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
      isPublished: raw.isPublished === '' ? undefined : raw.isPublished === 'true',
      isActive: raw.isActive === '' ? undefined : raw.isActive === 'true',
    };
  }

  private patchFormFromDetail(detail: SchoolInterviewFaqDetail): void {
    if (detail.educationalStageId) {
      this.taxonomiesApi.getGradesByStage(detail.educationalStageId).subscribe((result) => {
        if (result.succeeded && result.data) {
          this.grades.set(result.data);
        }
      });
    }
    this.form.patchValue({
      questionAr: detail.questionAr ?? '',
      questionEn: detail.questionEn ?? '',
      answerAr: detail.answerAr ?? '',
      answerEn: detail.answerEn ?? '',
      interviewCategory: detail.interviewCategory ?? InterviewFaqCategory._1,
      schoolBranchId: detail.schoolBranchId ?? '',
      educationalStageId: detail.educationalStageId ?? '',
      gradeId: detail.gradeId ?? '',
      academicYearId: detail.academicYearId ?? '',
    });
  }

  private buildBodyFromForm(): CreateSchoolInterviewFaqRequest | UpdateSchoolInterviewFaqRequest {
    const raw = this.form.getRawValue();
    const shared = {
      questionAr: raw.questionAr.trim(),
      questionEn: raw.questionEn.trim(),
      answerAr: raw.answerAr.trim(),
      answerEn: raw.answerEn.trim(),
      interviewCategory: Number(raw.interviewCategory) as InterviewFaqCategory,
      schoolBranchId: raw.schoolBranchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
    };
    if (this.editingId()) {
      return {
        ...shared,
        rowVersion: this.editingRowVersion() ?? undefined,
      };
    }
    return shared;
  }
}
