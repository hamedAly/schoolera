import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { PublicFooter } from '../../core/layout/public-footer/public-footer';
import { PublicHeader } from '../../core/layout/public-header/public-header';
import { PublicPageContainer } from '../../shared/ui/public-page-container/public-page-container';

@Component({
  selector: 'se-not-found-page',
  imports: [PublicHeader, PublicFooter, PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './not-found-page.html',
  styleUrl: './not-found-page.scss',
})
export class NotFoundPage {}
