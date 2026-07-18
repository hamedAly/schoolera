import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthService } from './core/auth/auth.service';
import { DocumentLanguageService } from './core/i18n/document-language.service';
import { ToastHost } from './shared/ui/toast/toast-host';

@Component({
  selector: 'se-root',
  imports: [RouterOutlet, ToastHost],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // Keeps html lang/dir synchronized for the application lifetime.
  private readonly documentLanguage = inject(DocumentLanguageService);

  constructor() {
    inject(AuthService).initSession().subscribe();
  }
}
