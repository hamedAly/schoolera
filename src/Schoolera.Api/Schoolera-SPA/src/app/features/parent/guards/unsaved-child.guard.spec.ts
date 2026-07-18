import { TestBed } from '@angular/core/testing';
import { TranslocoService } from '@jsverse/transloco';
import { vi } from 'vitest';

import { unsavedChildGuard } from './unsaved-child.guard';

describe('unsavedChildGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: TranslocoService,
          useValue: { translate: vi.fn(() => 'Unsaved child changes') },
        },
      ],
    });
  });

  it('allows navigation when the component is clean', () => {
    const result = TestBed.runInInjectionContext(() =>
      unsavedChildGuard({ hasUnsavedChanges: () => false }, {} as never, {} as never, {} as never),
    );
    expect(result).toBe(true);
  });

  it('asks for confirmation when the child form is dirty', () => {
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const result = TestBed.runInInjectionContext(() =>
      unsavedChildGuard({ hasUnsavedChanges: () => true }, {} as never, {} as never, {} as never),
    );
    expect(confirm).toHaveBeenCalledWith('Unsaved child changes');
    expect(result).toBe(false);
  });
});
