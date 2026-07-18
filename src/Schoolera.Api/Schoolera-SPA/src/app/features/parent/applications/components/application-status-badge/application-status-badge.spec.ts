import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';

import { AdmissionApplicationStatus } from '../../../../../core/api-client/SwaggerClient.service';
import { ApplicationStatusBadge } from './application-status-badge';

describe('ApplicationStatusBadge', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ApplicationStatusBadge,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                applications: {
                  status: {
                    draft: 'مسودة',
                    descriptions: { draft: 'مسودة — لم يُقدَّم بعد' },
                  },
                },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
    }).compileComponents();
  });

  it('renders draft label and description', () => {
    const fixture = TestBed.createComponent(ApplicationStatusBadge);
    fixture.componentRef.setInput('status', AdmissionApplicationStatus._1);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('مسودة');
    expect(text).toContain('مسودة — لم يُقدَّم بعد');
  });
});
