import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'se-nav',
  imports: [RouterLink, RouterLinkActive, TranslocoPipe],
  templateUrl: './nav.html',
  styleUrl: './nav.scss',
})
export class Nav {
  protected readonly links = [
    { labelKey: 'ops.dashboard', route: '/dashboard' },
    { labelKey: 'ops.schools', route: '/schools' },
    { labelKey: 'ops.students', route: '/students' },
    { labelKey: 'ops.staff', route: '/staff' },
    { labelKey: 'ops.classes', route: '/classes' },
  ] as const;
}
