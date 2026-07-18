import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { TaxonomiesApi } from '../../../../taxonomies/data-access/taxonomies.api';
import { ParentApi } from '../../../data-access/parent.api';
import { CourierAvailability } from './courier-availability';

describe('CourierAvailability', () => {
  let fixture: ComponentFixture<CourierAvailability>;
  const api = {
    getOwnedApplicationCourierAvailability: vi.fn(() => of({
      succeeded: true,
      data: {
        destinationBranch: { nameEn: 'Main branch', addressEn: 'School address' },
        options: [{
          providerCode: 'Simulated',
          providerNameEn: 'Safe Courier',
          serviceCode: 'HomePickup',
          serviceNameEn: 'Home pickup',
          earliestPickupAtUtc: '2026-08-01T08:00:00Z',
          timeZoneId: 'Africa/Cairo',
          isSimulatedWarning: true,
          sla: { deliveryToSchoolTargetMinutes: 1440 },
        }],
        reasonCodes: [],
      },
    })),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CourierAvailability,
        TranslocoTestingModule.forRoot({
          langs: { en: { parent: { applications: { courier: {
            title: 'Courier pickup availability',
            readOnlyNotice: 'Preview only',
            preview: 'Preview availability',
            loading: 'Checking',
            destinationBranch: 'Destination',
            simulatedWarning: 'Simulated',
            service: 'Service',
            serviceDescription: 'Details',
            earliestPickup: 'Earliest',
            timezone: 'Time zone',
            coverageNotes: 'Coverage',
            elapsedMinutes: 'elapsed minutes',
            terms: 'Terms',
            privacy: 'Privacy',
            sla: { acceptance: 'Acceptance', scheduling: 'Scheduling', pickup: 'Pickup', delivery: 'Delivery' },
          } } } } },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
      providers: [
        { provide: ParentApi, useValue: api },
        { provide: TaxonomiesApi, useValue: { getCountries: () => of({ succeeded: true, data: [] }) } },
        { provide: LocaleFormatService, useValue: { formatDateTime: (value: string) => value } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourierAvailability);
    fixture.componentRef.setInput('applicationId', 'app-1');
    (fixture.componentInstance as any).location.set({ countryId: 'country-1' });
    (fixture.componentInstance as any).preview();
    fixture.detectChanges();
  });

  it('renders safe availability without shipment or booking actions', () => {
    expect(api.getOwnedApplicationCourierAvailability).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Safe Courier');
    const buttonText = [...fixture.nativeElement.querySelectorAll('button')]
      .map((button: HTMLButtonElement) => button.textContent.trim().toLowerCase());
    expect(buttonText).toEqual(['preview availability']);
    expect(buttonText.join(' ')).not.toMatch(/create|confirm|shipment|tracking|book/);
  });
});
