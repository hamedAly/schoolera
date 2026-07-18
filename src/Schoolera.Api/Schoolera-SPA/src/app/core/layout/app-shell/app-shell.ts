import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { Nav } from '../nav/nav';

/** Temporary operations shell for internal placeholder modules. Public pages use PublicLayout. */
@Component({
  selector: 'se-app-shell',
  imports: [Nav, RouterOutlet, TranslocoPipe],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {}
