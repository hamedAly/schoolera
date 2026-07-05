import { Component } from '@angular/core';

import { AppShell } from './core/layout/app-shell/app-shell';

@Component({
  selector: 'se-root',
  imports: [AppShell],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
