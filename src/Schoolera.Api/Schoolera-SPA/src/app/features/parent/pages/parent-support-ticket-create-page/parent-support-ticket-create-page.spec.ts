import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { ParentSupportTicketsApi } from '../../data-access/parent-support-tickets.api';
import { ParentSupportTicketCreatePage } from './parent-support-ticket-create-page';

describe('ParentSupportTicketCreatePage application prefill', () => {
  const validId = '123e4567-e89b-42d3-a456-426614174000';

  async function create(queryValue: string | null) {
    await TestBed.configureTestingModule({
      imports: [
        ParentSupportTicketCreatePage,
        TranslocoTestingModule.forRoot({
          langs: { en: { parent: { supportTickets: { create: {} }, common: {} } } },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => queryValue } } } },
        { provide: ParentSupportTicketsApi, useValue: { create: vi.fn(() => of({ succeeded: true })) } },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    }).compileComponents();
    return TestBed.createComponent(ParentSupportTicketCreatePage).componentInstance;
  }

  it('prefills only a valid GUID query parameter', async () => {
    expect((await create(validId)).form.controls.admissionApplicationId.value).toBe(validId);
    TestBed.resetTestingModule();
    expect((await create('not-a-guid')).form.controls.admissionApplicationId.value).toBe('');
  });
});
