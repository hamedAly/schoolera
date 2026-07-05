import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Nav } from '../nav/nav';

@Component({
  selector: 'se-app-shell',
  imports: [Nav, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {}
