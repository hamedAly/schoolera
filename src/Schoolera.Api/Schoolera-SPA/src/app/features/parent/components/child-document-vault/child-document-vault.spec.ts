import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { ParentApi } from '../../data-access/parent.api';
import { ChildDocumentVault } from './child-document-vault';

describe('ChildDocumentVault', () => {
  const upload = vi.fn(() =>
    of({ kind: 'complete' as const, result: { succeeded: true, data: { id: 'doc-2', documentType: 1 }, errors: [] } }),
  );

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ChildDocumentVault,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                documentVault: {
                  title: 'الخزنة',
                  description: 'وصف',
                  loading: 'تحميل',
                  upload: 'رفع',
                  replace: 'استبدال',
                  download: 'تنزيل',
                  emptyType: 'فارغ',
                  progress: '{{percent}}',
                  deleteConfirmTitle: 'حذف؟',
                  deleteConfirmMessage: 'تأكيد الحذف',
                  types: {
                    birthCertificate: 'ميلاد',
                    childPhoto: 'صورة',
                    previousSchoolCertificate: 'شهادة',
                    medicalReport: 'تقرير',
                    otherApproved: 'آخر',
                  },
                },
                common: { delete: 'حذف' },
                confirm: { cancel: 'إلغاء' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        {
          provide: ParentApi,
          useValue: {
            listChildDocuments: vi.fn(() =>
              of({
                succeeded: true,
                data: [
                  {
                    id: 'doc-1',
                    documentType: 1,
                    originalFileName: 'birth.pdf',
                    fileSizeBytes: 1024,
                  },
                ],
              }),
            ),
            uploadChildDocumentWithProgress: upload,
            replaceChildDocumentWithProgress: vi.fn(),
            downloadChildDocument: vi.fn(() => of(undefined)),
            deleteChildDocument: vi.fn(() => of({ succeeded: true, data: true })),
          },
        },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    }).compileComponents();
  });

  it('loads and groups documents without exposing storage keys', () => {
    const fixture = TestBed.createComponent(ChildDocumentVault);
    fixture.componentRef.setInput('childId', 'child-1');
    fixture.detectChanges();

    const html = fixture.nativeElement as HTMLElement;
    expect(html.textContent).toContain('birth.pdf');
    expect(html.textContent).toContain('ميلاد');
    expect(html.textContent).not.toContain('storageKey');
  });

  it('uploads an accepted file with its selected document type', () => {
    const fixture = TestBed.createComponent(ChildDocumentVault);
    fixture.componentRef.setInput('childId', 'child-1');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('#vault-upload-1') as HTMLInputElement;
    Object.defineProperty(input, 'files', {
      configurable: true,
      value: [new File(['pdf'], 'new.pdf', { type: 'application/pdf' })],
    });
    input.dispatchEvent(new Event('change'));

    expect(upload).toHaveBeenCalledWith('child-1', expect.any(File), 1);
  });
});
