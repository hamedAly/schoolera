import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { describe, expect, it, beforeEach } from 'vitest';

import { AdmissionQuestionType } from '../../../data-access/admission-questions.models';
import { ApplicationQuestionsForm } from './application-questions-form';

describe('ApplicationQuestionsForm', () => {
  let fixture: ComponentFixture<ApplicationQuestionsForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ApplicationQuestionsForm,
        TranslocoTestingModule.forRoot({
          langs: {
            en: {
              parent: {
                applications: {
                  wizard: {
                    questions: {
                      types: { shortText: 'Short text' },
                      yes: 'Yes',
                      no: 'No',
                    },
                  },
                },
              },
            },
          },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationQuestionsForm);
    fixture.componentRef.setInput('applicationId', 'app-1');
    fixture.componentRef.setInput('questions', [
      {
        snapshotId: 'q1',
        label: 'Why this school?',
        questionType: AdmissionQuestionType.ShortText,
        isRequired: true,
        isComplete: false,
        sortOrder: 0,
      },
    ]);
    fixture.componentRef.setInput('readOnly', true);
    fixture.detectChanges();
  });

  it('renders question label in read-only mode', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Why this school?');
  });
});
