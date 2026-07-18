import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  FaqCategoryAdminDto,
  FaqItemAdminDto,
  FaqOwnershipScope,
  InterviewFaqCategory,
} from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { sanitizeHtml } from '../../../../shared/utils/sanitize-html';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { localizedBilingualName } from '../../../school-portal/utils/localized-name';
import { AdminCmsApi } from '../../data-access/admin-cms.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';

type PanelMode = 'none' | 'category' | 'item' | 'interviewItem';
type ViewMode = 'general' | 'interview';

@Component({
  selector: 'se-admin-cms-faq-page',
  imports: [
    BilingualFieldGroup,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './admin-cms-faq-page.html',
  styleUrl: './admin-cms-faq-page.scss',
})
export class AdminCmsFaqPage implements OnInit {
  private readonly api = inject(AdminCmsApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly InterviewCategory = InterviewFaqCategory;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly categories = signal<FaqCategoryAdminDto[]>([]);
  protected readonly interviewItems = signal<FaqItemAdminDto[]>([]);
  protected readonly selectedCategoryId = signal<string | null>(null);
  protected readonly panelMode = signal<PanelMode>('none');
  protected readonly viewMode = signal<ViewMode>('general');
  protected readonly editingCategoryId = signal<string | null>(null);
  protected readonly editingItemId = signal<string | null>(null);
  protected readonly editingRowVersion = signal<string | null>(null);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly selectedCategory = computed(() => {
    const id = this.selectedCategoryId();
    return this.categories().find((c) => c.id === id) ?? null;
  });

  protected readonly items = computed(() => this.selectedCategory()?.items ?? []);

  protected readonly categoryForm = this.fb.group({
    nameAr: ['', Validators.required],
    nameEn: ['', Validators.required],
    slug: ['', Validators.required],
  });

  protected readonly itemForm = this.fb.group({
    questionAr: ['', Validators.required],
    questionEn: ['', Validators.required],
    answerAr: ['', Validators.required],
    answerEn: ['', Validators.required],
  });

  protected readonly interviewItemForm = this.fb.group({
    questionAr: ['', Validators.required],
    questionEn: ['', Validators.required],
    answerAr: ['', Validators.required],
    answerEn: ['', Validators.required],
    interviewCategory: [InterviewFaqCategory._1 as number, Validators.required],
  });

  protected readonly interviewFilterForm = this.fb.nonNullable.group({
    interviewCategory: [''],
    isPublished: [''],
    isActive: [''],
    search: [''],
  });

  ngOnInit(): void {
    this.load();
    this.interviewFilterForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.viewMode() === 'interview') {
        this.loadInterviewItems();
      }
    });
  }

  protected setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.closePanel();
    this.errorMessage.set(null);
    if (mode === 'interview') {
      this.loadInterviewItems();
    } else {
      this.load();
    }
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listFaqCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded) {
            const list = [...(result.data ?? [])].sort(
              (a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0),
            );
            this.categories.set(list);
            if (!this.selectedCategoryId() && list[0]?.id) {
              this.selectedCategoryId.set(list[0].id);
            }
            return;
          }
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected loadInterviewItems(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    const raw = this.interviewFilterForm.getRawValue();
    this.api
      .listInterviewFaqItems(
        raw.interviewCategory ? (Number(raw.interviewCategory) as InterviewFaqCategory) : undefined,
        raw.isPublished === '' ? undefined : raw.isPublished === 'true',
        raw.isActive === '' ? undefined : raw.isActive === 'true',
        raw.search.trim() || undefined,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded) {
            const list = [...(result.data ?? [])].sort(
              (a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0),
            );
            this.interviewItems.set(list);
            return;
          }
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected selectCategory(id: string): void {
    this.selectedCategoryId.set(id);
    this.closePanel();
  }

  protected openCreateCategory(): void {
    this.editingCategoryId.set(null);
    this.categoryForm.reset({ nameAr: '', nameEn: '', slug: '' });
    this.panelMode.set('category');
  }

  protected openEditCategory(cat: FaqCategoryAdminDto): void {
    this.editingCategoryId.set(cat.id ?? null);
    this.categoryForm.reset({
      nameAr: cat.nameAr ?? '',
      nameEn: cat.nameEn ?? '',
      slug: cat.slug ?? '',
    });
    this.panelMode.set('category');
  }

  protected openCreateItem(): void {
    this.editingItemId.set(null);
    this.itemForm.reset({
      questionAr: '',
      questionEn: '',
      answerAr: '',
      answerEn: '',
    });
    this.panelMode.set('item');
  }

  protected openEditItem(item: FaqItemAdminDto): void {
    this.editingItemId.set(item.id ?? null);
    this.itemForm.reset({
      questionAr: item.questionAr ?? '',
      questionEn: item.questionEn ?? '',
      answerAr: item.answerAr ?? '',
      answerEn: item.answerEn ?? '',
    });
    this.panelMode.set('item');
  }

  protected openCreateInterviewItem(): void {
    this.editingItemId.set(null);
    this.editingRowVersion.set(null);
    this.interviewItemForm.reset({
      questionAr: '',
      questionEn: '',
      answerAr: '',
      answerEn: '',
      interviewCategory: InterviewFaqCategory._1,
    });
    this.panelMode.set('interviewItem');
  }

  protected openEditInterviewItem(item: FaqItemAdminDto): void {
    this.editingItemId.set(item.id ?? null);
    this.editingRowVersion.set(item.rowVersion ?? null);
    this.interviewItemForm.reset({
      questionAr: item.questionAr ?? '',
      questionEn: item.questionEn ?? '',
      answerAr: item.answerAr ?? '',
      answerEn: item.answerEn ?? '',
      interviewCategory: item.interviewCategory ?? InterviewFaqCategory._1,
    });
    this.panelMode.set('interviewItem');
  }

  protected closePanel(): void {
    this.panelMode.set('none');
    this.editingCategoryId.set(null);
    this.editingItemId.set(null);
    this.editingRowVersion.set(null);
  }

  protected saveCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const value = this.categoryForm.getRawValue();
    this.saving.set(true);
    const id = this.editingCategoryId();
    const request = id
      ? this.api.updateFaqCategory(id, {
          nameAr: value.nameAr?.trim(),
          nameEn: value.nameEn?.trim(),
          slug: value.slug?.trim(),
        })
      : this.api.createFaqCategory({
          nameAr: value.nameAr?.trim(),
          nameEn: value.nameEn?.trim(),
          slug: value.slug?.trim(),
        });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.saving.set(false);
        if (result.succeeded) {
          this.toast.success(this.transloco.translate('admin.cms.faq.saved'));
          this.closePanel();
          this.load();
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => {
        this.saving.set(false);
        this.toast.error(this.transloco.translate('admin.errors.generic'));
      },
    });
  }

  protected saveItem(): void {
    const categoryId = this.selectedCategoryId();
    if (!categoryId || this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const value = this.itemForm.getRawValue();
    this.saving.set(true);
    const id = this.editingItemId();
    const request = id
      ? this.api.updateFaqItem(id, {
          categoryId,
          questionAr: value.questionAr?.trim(),
          questionEn: value.questionEn?.trim(),
          answerAr: value.answerAr?.trim(),
          answerEn: value.answerEn?.trim(),
        })
      : this.api.createFaqItem({
          categoryId,
          questionAr: value.questionAr?.trim(),
          questionEn: value.questionEn?.trim(),
          answerAr: value.answerAr?.trim(),
          answerEn: value.answerEn?.trim(),
        });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.saving.set(false);
        if (result.succeeded) {
          this.toast.success(this.transloco.translate('admin.cms.faq.saved'));
          this.closePanel();
          this.load();
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => {
        this.saving.set(false);
        this.toast.error(this.transloco.translate('admin.errors.generic'));
      },
    });
  }

  protected saveInterviewItem(): void {
    if (this.interviewItemForm.invalid) {
      this.interviewItemForm.markAllAsTouched();
      return;
    }

    const value = this.interviewItemForm.getRawValue();
    const interviewCategory = Number(value.interviewCategory) as InterviewFaqCategory;
    this.saving.set(true);
    const id = this.editingItemId();
    const request = id
      ? this.api.updateFaqItem(id, {
          questionAr: value.questionAr?.trim(),
          questionEn: value.questionEn?.trim(),
          answerAr: value.answerAr?.trim(),
          answerEn: value.answerEn?.trim(),
          interviewCategory,
          rowVersion: this.editingRowVersion() ?? undefined,
        })
      : this.api.createFaqItem({
          questionAr: value.questionAr?.trim(),
          questionEn: value.questionEn?.trim(),
          answerAr: value.answerAr?.trim(),
          answerEn: value.answerEn?.trim(),
          ownershipScope: FaqOwnershipScope._1,
          interviewCategory,
        });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.saving.set(false);
        if (result.succeeded) {
          this.toast.success(this.transloco.translate('admin.cms.faq.saved'));
          this.closePanel();
          this.loadInterviewItems();
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => {
        this.saving.set(false);
        this.toast.error(this.transloco.translate('admin.errors.generic'));
      },
    });
  }

  protected toggleCategoryPublish(cat: FaqCategoryAdminDto): void {
    if (!cat.id) {
      return;
    }
    const request = cat.isPublished
      ? this.api.unpublishFaqCategory(cat.id)
      : this.api.publishFaqCategory(cat.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (result.succeeded) {
          this.load();
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
    });
  }

  protected toggleItemPublish(item: FaqItemAdminDto): void {
    if (!item.id) {
      return;
    }
    const request = item.isPublished
      ? this.api.unpublishFaqItem(item.id)
      : this.api.publishFaqItem(item.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (result.succeeded) {
          if (this.viewMode() === 'interview') {
            this.loadInterviewItems();
          } else {
            this.load();
          }
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
    });
  }

  protected toggleItemActive(item: FaqItemAdminDto): void {
    if (!item.id) {
      return;
    }
    const request =
      item.isActive === false
        ? this.api.activateFaqItem(item.id)
        : this.api.deactivateFaqItem(item.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (result.succeeded) {
          this.loadInterviewItems();
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
    });
  }

  protected moveCategory(index: number, delta: number): void {
    const list = [...this.categories()];
    const target = index + delta;
    if (target < 0 || target >= list.length) {
      return;
    }
    const [item] = list.splice(index, 1);
    list.splice(target, 0, item);
    const orderedIds = list.map((c) => c.id!).filter(Boolean);
    this.api
      .reorderFaqCategories({ orderedIds })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded) {
            this.load();
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
      });
  }

  protected moveItem(index: number, delta: number): void {
    const categoryId = this.selectedCategoryId();
    if (!categoryId) {
      return;
    }
    const list = [...this.items()];
    const target = index + delta;
    if (target < 0 || target >= list.length) {
      return;
    }
    const [item] = list.splice(index, 1);
    list.splice(target, 0, item);
    const orderedIds = list.map((i) => i.id!).filter(Boolean);
    this.api
      .reorderFaqItems({ categoryId, orderedIds })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded) {
            this.load();
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
      });
  }

  protected categoryLabel(cat: FaqCategoryAdminDto): string {
    return localizedBilingualName(cat.nameAr, cat.nameEn, this.activeLang());
  }

  protected itemLabel(item: FaqItemAdminDto): string {
    return localizedBilingualName(item.questionAr, item.questionEn, this.activeLang());
  }

  protected interviewCategoryLabel(category: number | undefined): string {
    switch (category) {
      case InterviewFaqCategory._1:
        return this.transloco.translate('admin.cms.faq.interview.categories.interview');
      case InterviewFaqCategory._2:
        return this.transloco.translate('admin.cms.faq.interview.categories.assessment');
      case InterviewFaqCategory._3:
        return this.transloco.translate('admin.cms.faq.interview.categories.interviewAndAssessment');
      default:
        return '—';
    }
  }

  protected previewAnswer(item: FaqItemAdminDto): string {
    const html =
      this.activeLang() === 'en' ? (item.answerEn ?? '') : (item.answerAr ?? '');
    return sanitizeHtml(this.sanitizer, html);
  }
}
