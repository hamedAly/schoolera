import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  PreferredContactMethod,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { sanitizeReturnUrl } from '../../../../core/auth/return-url';
import { resolveSchooleraLang } from '../../../../core/i18n/schoolera-lang';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { Button } from '../../../../shared/ui/button/button';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import {
  mapParentErrorSummaryItems,
  translateParentErrorCodes,
} from '../../data-access/parent-errors';
import { ParentApi } from '../../data-access/parent.api';

@Component({
  selector: 'se-parent-profile-page',
  imports: [
    Button,
    FormErrorSummary,
    FormField,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './parent-profile-page.html',
  styleUrl: './parent-profile-page.scss',
})
export class ParentProfilePage implements OnInit {
  private readonly api = inject(ParentApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);
  protected readonly email = signal('');
  protected readonly cities = signal<TaxonomyItemDto[]>([]);
  protected readonly districts = signal<TaxonomyItemDto[]>([]);

  protected readonly contactMethods = [
    { value: PreferredContactMethod._1, labelKey: 'parent.enums.contactMethod.phone' },
    { value: PreferredContactMethod._2, labelKey: 'parent.enums.contactMethod.whatsApp' },
    { value: PreferredContactMethod._3, labelKey: 'parent.enums.contactMethod.email' },
  ] as const;

  protected readonly languages = [
    { value: 'ar', labelKey: 'parent.enums.language.ar' },
    { value: 'en', labelKey: 'parent.enums.language.en' },
  ] as const;

  protected readonly form = this.formBuilder.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: ['', Validators.required],
    alternatePhone: [''],
    addressLine: [''],
    cityId: [''],
    districtId: [''],
    preferredContactMethod: [PreferredContactMethod._1 as PreferredContactMethod, Validators.required],
    preferredLanguage: ['ar', Validators.required],
  });

  ngOnInit(): void {
    this.taxonomiesApi
      .getCities()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.cities.set(result.data);
        }
      });

    this.form.controls.cityId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((cityId) => {
        this.form.controls.districtId.setValue('');
        this.districts.set([]);
        if (!cityId) {
          return;
        }
        this.taxonomiesApi.getDistrictsByCity(cityId).subscribe((result) => {
          if (result.succeeded && result.data) {
            this.districts.set(result.data);
          }
        });
      });

    this.api
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        const profile = result.data;
        this.email.set(profile.email ?? '');
        this.form.patchValue({
          firstName: profile.firstName ?? '',
          lastName: profile.lastName ?? '',
          phone: profile.phone ?? '',
          alternatePhone: profile.alternatePhone ?? '',
          addressLine: profile.addressLine ?? '',
          cityId: profile.cityId ?? '',
          districtId: profile.districtId ?? '',
          preferredContactMethod: profile.preferredContactMethod ?? PreferredContactMethod._1,
          preferredLanguage: profile.preferredLanguage ?? 'ar',
        });

        if (profile.cityId) {
          this.taxonomiesApi.getDistrictsByCity(profile.cityId).subscribe((districtResult) => {
            if (districtResult.succeeded && districtResult.data) {
              this.districts.set(districtResult.data);
              this.form.controls.districtId.setValue(profile.districtId ?? '');
            }
          });
        }

        this.form.markAsPristine();
      });
  }

  protected save(): void {
    this.summaryErrors.set([]);
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set(this.collectClientSummaryErrors());
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();

    this.api
      .updateProfile({
        firstName: raw.firstName.trim(),
        lastName: raw.lastName.trim(),
        phone: raw.phone.trim(),
        alternatePhone: raw.alternatePhone.trim() || undefined,
        addressLine: raw.addressLine.trim() || undefined,
        cityId: raw.cityId || undefined,
        districtId: raw.districtId || undefined,
        preferredContactMethod: raw.preferredContactMethod,
        preferredLanguage: resolveSchooleraLang(raw.preferredLanguage),
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.toast.success(this.transloco.translate('parent.profile.saved'));
          this.form.markAsPristine();
          const returnUrl = sanitizeReturnUrl(
            this.route.snapshot.queryParamMap.get('returnUrl'),
            '',
          );
          if (returnUrl) {
            void this.router.navigateByUrl(returnUrl);
          }
          return;
        }
        this.summaryErrors.set(mapParentErrorSummaryItems(this.transloco, result.errorCodes));
      });
  }

  protected fieldError(controlName: keyof typeof this.form.controls): string | undefined {
    const control = this.form.controls[controlName];
    if (!control.touched || !control.errors) {
      return undefined;
    }
    if (control.errors['required']) {
      return this.transloco.translate('parent.profile.validation.required');
    }
    return undefined;
  }

  protected taxonomyName(item: TaxonomyItemDto): string {
    return item.name ?? '';
  }

  private collectClientSummaryErrors(): FormErrorSummaryItem[] {
    const items: FormErrorSummaryItem[] = [];
    if (this.form.controls.firstName.invalid) {
      items.push({
        message: this.transloco.translate('parent.profile.validation.firstNameRequired'),
        fieldId: 'parent-firstName',
      });
    }
    if (this.form.controls.lastName.invalid) {
      items.push({
        message: this.transloco.translate('parent.profile.validation.lastNameRequired'),
        fieldId: 'parent-lastName',
      });
    }
    if (this.form.controls.phone.invalid) {
      items.push({
        message: this.transloco.translate('parent.profile.validation.phoneRequired'),
        fieldId: 'parent-phone',
      });
    }
    return items;
  }
}
