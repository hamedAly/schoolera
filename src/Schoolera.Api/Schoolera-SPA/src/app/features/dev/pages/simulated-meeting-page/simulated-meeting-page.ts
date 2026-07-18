import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'se-simulated-meeting-page',
  imports: [RouterLink, TranslocoPipe],
  template: `
    <main class="simulated-meeting" aria-labelledby="simulated-meeting-title">
      <p class="simulated-meeting__badge">{{ 'common.simulatedMeeting.badge' | transloco }}</p>
      <h1 id="simulated-meeting-title">{{ 'common.simulatedMeeting.title' | transloco }}</h1>
      <p>{{ 'common.simulatedMeeting.description' | transloco }}</p>
      <p>{{ 'common.simulatedMeeting.noMedia' | transloco }}</p>
      <p class="simulated-meeting__role">
        {{ 'common.simulatedMeeting.role' | transloco }}:
        {{ role === 'host' ? ('common.simulatedMeeting.host' | transloco) : ('common.simulatedMeeting.parent' | transloco) }}
      </p>
      <a routerLink="/">{{ 'common.simulatedMeeting.leave' | transloco }}</a>
    </main>
  `,
  styles: `
    .simulated-meeting { max-width: 44rem; margin: 4rem auto; padding: 2rem; border: 1px solid #cbd5e1; border-radius: 1rem; text-align: center; }
    .simulated-meeting__badge { display: inline-block; padding: .35rem .75rem; border-radius: 999px; color: #7c2d12; background: #ffedd5; font-weight: 700; }
    .simulated-meeting__role { font-weight: 600; }
  `,
})
export class SimulatedMeetingPage {
  private readonly route = inject(ActivatedRoute);
  protected readonly role = this.route.snapshot.paramMap.get('role');
}
