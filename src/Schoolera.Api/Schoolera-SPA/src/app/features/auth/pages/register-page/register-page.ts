import { Component } from '@angular/core';

import { PublicPlaceholder } from '../../../../shared/ui/public-placeholder/public-placeholder';
import { PublicPlaceholderContentKeys } from '../../../../shared/ui/public-placeholder/public-placeholder.models';

@Component({
  selector: 'se-register-page',
  imports: [PublicPlaceholder],
  template: `<se-public-placeholder [content]="content" />`,
})
export class RegisterPage {
  protected readonly content: PublicPlaceholderContentKeys = {
    titleKey: 'auth.register.title',
    descriptionKey: 'auth.register.description',
    breadcrumbKeys: [
      { labelKey: 'common.home', route: '/' },
      { labelKey: 'auth.register.title' },
    ],
    primaryAction: { labelKey: 'auth.register.chooseType', route: '/auth/account-type' },
    secondaryAction: { labelKey: 'nav.login', route: '/auth/login' },
  };
}
