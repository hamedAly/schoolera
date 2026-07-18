import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { describe, expect, it, beforeEach } from 'vitest';

import { ApplicationRequirementsChecklist } from './application-requirements-checklist';
import { AdmissionRequirementKind } from '../../../data-access/admission-requirements.models';

describe('ApplicationRequirementsChecklist', () => {
  let fixture: ComponentFixture<ApplicationRequirementsChecklist>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ApplicationRequirementsChecklist,
        TranslocoTestingModule.forRoot({
          langs: {
            en: {
              parent: {
                applications: {
                  wizard: {
                    requirements: {
                      upload: 'Upload',
                      incomplete: 'Incomplete',
                      completeProfile: 'Complete profile',
                      allowedExtensions: 'Allowed extensions',
                      kinds: { informational: 'Info', document: 'Document' },
                    },
                  },
                },
                common: { select: 'Select' },
              },
            },
          },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationRequirementsChecklist);
    fixture.componentRef.setInput('requirements', [
      {
        snapshotId: 's1',
        name: 'Birth certificate',
        kind: AdmissionRequirementKind.ApplicationDocument,
        isRequired: true,
        isComplete: false,
        sortOrder: 0,
        allowedFileExtensions: ['.pdf'],
      },
    ]);
    fixture.componentRef.setInput('canUpload', true);
    fixture.detectChanges();
  });

  it('renders requirement name', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Birth certificate');
  });
});
