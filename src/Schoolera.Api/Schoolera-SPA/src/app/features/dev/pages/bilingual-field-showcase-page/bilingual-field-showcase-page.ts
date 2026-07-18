import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';

import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';

/**
 * Development-only showcase for bilingual management fields (no persistence / migration).
 */
@Component({
  selector: 'se-bilingual-field-showcase-page',
  imports: [BilingualFieldGroup, PublicPageContainer, ReactiveFormsModule, TranslocoPipe],
  templateUrl: './bilingual-field-showcase-page.html',
  styleUrl: './bilingual-field-showcase-page.scss',
})
export class BilingualFieldShowcasePage {
  private readonly formBuilder = new FormBuilder();

  protected readonly form = this.formBuilder.nonNullable.group({
    nameAr: ['', Validators.required],
    nameEn: ['', Validators.required],
    descriptionAr: [''],
    descriptionEn: [''],
  });
}
