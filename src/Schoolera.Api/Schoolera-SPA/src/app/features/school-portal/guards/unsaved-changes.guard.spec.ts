import { TestBed } from '@angular/core/testing';
import { TranslocoService } from '@jsverse/transloco';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { vi } from 'vitest';

import { unsavedChangesGuard } from './unsaved-changes.guard';
import { HasUnsavedPortalChanges } from './unsaved-changes.models';

describe('unsavedChangesGuard', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TranslocoTestingModule.forRoot({
          langs: {
            ar: { portal: { unsaved: { confirmLeave: 'تغييرات غير محفوظة' } } },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
    }).compileComponents();
  });

  it('allows navigation when there are no unsaved changes', () => {
    const component: HasUnsavedPortalChanges = { hasUnsavedChanges: () => false };
    const result = TestBed.runInInjectionContext(() => unsavedChangesGuard(component, {} as never, {} as never, {} as never));
    expect(result).toBe(true);
  });

  it('prompts before leaving when there are unsaved changes', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const component: HasUnsavedPortalChanges = { hasUnsavedChanges: () => true };

    const result = TestBed.runInInjectionContext(() => unsavedChangesGuard(component, {} as never, {} as never, {} as never));

    expect(confirmSpy).toHaveBeenCalledWith(
      TestBed.inject(TranslocoService).translate('portal.unsaved.confirmLeave'),
    );
    expect(result).toBe(false);
  });
});
