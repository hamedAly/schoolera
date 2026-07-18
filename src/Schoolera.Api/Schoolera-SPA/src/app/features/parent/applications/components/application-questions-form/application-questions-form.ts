import { Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { debounceTime, distinctUntilChanged, finalize, Subject } from 'rxjs';

import { AdmissionApplicationDetailDto } from '../../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { FormField } from '../../../../../shared/ui/form-field/form-field';
import { ToastService } from '../../../../../shared/ui/toast/toast.service';
import { translateAdmissionErrorCodes } from '../../../data-access/admission-errors';
import {
  AdmissionQuestionChecklistItem,
  AdmissionQuestionType,
  selectedOptionLabels,
} from '../../../data-access/admission-questions.models';
import { ParentApi } from '../../../data-access/parent.api';

@Component({
  selector: 'se-application-questions-form',
  imports: [FormField, FormsModule, TranslocoPipe],
  templateUrl: './application-questions-form.html',
  styleUrl: './application-questions-form.scss',
})
export class ApplicationQuestionsForm {
  readonly applicationId = input.required<string>();
  readonly questions = input<readonly AdmissionQuestionChecklistItem[]>([]);
  readonly canEdit = input(true);
  readonly readOnly = input(false);
  readonly canUpload = input(false);

  readonly applicationUpdated = output<AdmissionApplicationDetailDto>();

  private readonly api = inject(ParentApi);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly textSave$ = new Subject<{ snapshotId: string; value: string }>();

  protected readonly QuestionType = AdmissionQuestionType;
  protected readonly savingSnapshotId = signal<string | null>(null);
  protected readonly uploadingSnapshotId = signal<string | null>(null);
  protected readonly uploadPercent = signal(0);

  protected readonly sortedQuestions = computed(() =>
    [...this.questions()].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
  );

  constructor() {
    this.textSave$
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ snapshotId, value }) => {
        this.persistAnswer(snapshotId, { textValue: value.trim() || null });
      });
  }

  ensureSnapshots(): void {
    if (this.readOnly() || !this.applicationId()) {
      return;
    }
    this.api
      .ensureQuestionSnapshots(this.applicationId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.applicationUpdated.emit(result.data);
        } else if (result.errorCodes?.length) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
        }
      });
  }

  protected typeKey(type: number | undefined): string {
    switch (type) {
      case AdmissionQuestionType.ShortText:
        return 'parent.applications.wizard.questions.types.shortText';
      case AdmissionQuestionType.LongText:
        return 'parent.applications.wizard.questions.types.longText';
      case AdmissionQuestionType.SingleChoice:
        return 'parent.applications.wizard.questions.types.singleChoice';
      case AdmissionQuestionType.MultipleChoice:
        return 'parent.applications.wizard.questions.types.multipleChoice';
      case AdmissionQuestionType.Date:
        return 'parent.applications.wizard.questions.types.date';
      case AdmissionQuestionType.YesNo:
        return 'parent.applications.wizard.questions.types.yesNo';
      case AdmissionQuestionType.File:
        return 'parent.applications.wizard.questions.types.file';
      default:
        return 'parent.applications.wizard.questions.types.other';
    }
  }

  protected reasonKey(reasonCode: string | null | undefined): string | null {
    if (!reasonCode) {
      return null;
    }
    return `parent.applications.wizard.questions.reasons.${reasonCode.replace(/\./g, '_')}`;
  }

  protected displayAnswer(question: AdmissionQuestionChecklistItem): string {
    switch (question.questionType) {
      case AdmissionQuestionType.ShortText:
      case AdmissionQuestionType.LongText:
        return question.textValue?.trim() || '—';
      case AdmissionQuestionType.SingleChoice:
      case AdmissionQuestionType.MultipleChoice: {
        const labels = selectedOptionLabels(question);
        return labels.length ? labels.join(', ') : '—';
      }
      case AdmissionQuestionType.Date:
        return question.dateValue ? this.localeFormat.formatDate(question.dateValue) : '—';
      case AdmissionQuestionType.YesNo:
        if (question.booleanValue === true) {
          return this.transloco.translate('parent.applications.wizard.questions.yes');
        }
        if (question.booleanValue === false) {
          return this.transloco.translate('parent.applications.wizard.questions.no');
        }
        return '—';
      case AdmissionQuestionType.File:
        return question.linkedAttachmentId
          ? this.transloco.translate('parent.applications.wizard.questions.fileAttached')
          : '—';
      default:
        return '—';
    }
  }

  protected extensionsLabel(question: AdmissionQuestionChecklistItem): string {
    const extensions = question.allowedFileExtensions ?? [];
    return extensions.length ? extensions.join(', ') : '—';
  }

  protected onTextInput(question: AdmissionQuestionChecklistItem, value: string): void {
    if (!question.snapshotId || this.readOnly() || !this.canEdit()) {
      return;
    }
    this.textSave$.next({ snapshotId: question.snapshotId, value });
  }

  protected onSingleChoice(question: AdmissionQuestionChecklistItem, optionCode: string): void {
    if (!question.snapshotId || this.readOnly() || !this.canEdit()) {
      return;
    }
    this.persistAnswer(question.snapshotId, { selectedOptionCodes: [optionCode] });
  }

  protected onMultipleChoice(
    question: AdmissionQuestionChecklistItem,
    optionCode: string,
    checked: boolean,
  ): void {
    if (!question.snapshotId || this.readOnly() || !this.canEdit()) {
      return;
    }
    const current = new Set<string>(question.selectedOptionCodes ?? []);
    if (checked) {
      current.add(optionCode);
    } else {
      current.delete(optionCode);
    }
    this.persistAnswer(question.snapshotId, { selectedOptionCodes: [...current] });
  }

  protected isOptionSelected(question: AdmissionQuestionChecklistItem, optionCode: string): boolean {
    return (question.selectedOptionCodes ?? []).includes(optionCode);
  }

  protected onDateChange(question: AdmissionQuestionChecklistItem, value: string): void {
    if (!question.snapshotId || this.readOnly() || !this.canEdit()) {
      return;
    }
    this.persistAnswer(question.snapshotId, { dateValue: value || null });
  }

  protected onYesNo(question: AdmissionQuestionChecklistItem, value: boolean): void {
    if (!question.snapshotId || this.readOnly() || !this.canEdit()) {
      return;
    }
    this.persistAnswer(question.snapshotId, { booleanValue: value });
  }

  protected onFileSelected(event: Event, question: AdmissionQuestionChecklistItem): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = '';
    if (!file || !question.snapshotId || this.readOnly() || !this.canEdit() || !this.canUpload()) {
      return;
    }
    if (this.uploadingSnapshotId()) {
      return;
    }

    this.uploadingSnapshotId.set(question.snapshotId);
    this.uploadPercent.set(0);
    this.api
      .uploadQuestionAttachmentWithProgress(this.applicationId(), file, question.snapshotId)
      .pipe(
        finalize(() => this.uploadingSnapshotId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (uploadEvent) => {
          if (uploadEvent.kind === 'progress') {
            this.uploadPercent.set(uploadEvent.percent);
            return;
          }
          if (!uploadEvent.result.succeeded || !uploadEvent.result.data) {
            this.toast.error(
              translateAdmissionErrorCodes(this.transloco, uploadEvent.result.errorCodes),
            );
            return;
          }
          this.applicationUpdated.emit(uploadEvent.result.data);
          this.toast.success(this.transloco.translate('parent.applications.wizard.uploadSuccess'));
        },
        error: () => this.toast.error(this.transloco.translate('parent.errors.generic')),
      });
  }

  private persistAnswer(
    snapshotId: string,
    patch: {
      textValue?: string | null;
      selectedOptionCodes?: string[];
      dateValue?: string | null;
      booleanValue?: boolean | null;
    },
  ): void {
    if (!this.canEdit()) {
      return;
    }
    this.savingSnapshotId.set(snapshotId);
    this.api
      .upsertAdmissionAnswer(this.applicationId(), {
        questionSnapshotId: snapshotId,
        textValue: patch.textValue ?? undefined,
        selectedOptionCodes: patch.selectedOptionCodes,
        dateValue: patch.dateValue ?? undefined,
        booleanValue: patch.booleanValue ?? undefined,
      })
      .pipe(
        finalize(() => this.savingSnapshotId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.applicationUpdated.emit(result.data);
      });
  }
}