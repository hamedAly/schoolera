import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslocoTestingModule } from '@jsverse/transloco';

import { BilingualFieldGroup } from './bilingual-field-group';

describe('BilingualFieldGroup', () => {
  let fixture: ComponentFixture<BilingualFieldGroup>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        BilingualFieldGroup,
        ReactiveFormsModule,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              common: { bilingual: { arabicLabel: 'العربية', englishLabel: 'English' } },
            },
            en: {
              common: { bilingual: { arabicLabel: 'Arabic', englishLabel: 'English' } },
            },
          },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: 'ar',
          },
          preloadLangs: true,
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BilingualFieldGroup);
    const form = new FormBuilder().nonNullable.group({
      nameAr: [''],
      nameEn: [''],
    });
    fixture.componentRef.setInput('formGroup', form);
    fixture.componentRef.setInput('arabicControlName', 'nameAr');
    fixture.componentRef.setInput('englishControlName', 'nameEn');
    fixture.detectChanges();
  });

  it('renders Arabic and English columns with explicit directions', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const arabicInput = compiled.querySelector('input[lang="ar"]') as HTMLInputElement | null;
    const englishInput = compiled.querySelector('input[lang="en"]') as HTMLInputElement | null;

    expect(arabicInput).toBeTruthy();
    expect(englishInput).toBeTruthy();
    expect(arabicInput?.getAttribute('dir')).toBe('rtl');
    expect(englishInput?.getAttribute('dir')).toBe('ltr');
    expect(compiled.textContent).toContain('العربية');
  });
});
