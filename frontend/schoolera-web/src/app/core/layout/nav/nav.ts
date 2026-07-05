import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'se-nav',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.scss',
})
export class Nav {
  protected readonly links = [
    { label: 'Dashboard', route: '/dashboard' },
    { label: 'Schools', route: '/schools' },
    { label: 'Students', route: '/students' },
    { label: 'Staff', route: '/staff' },
    { label: 'Classes', route: '/classes' },
  ] as const;
}
