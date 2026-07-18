import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'se-portal-loading-skeleton',
  imports: [TranslocoPipe],
  templateUrl: './portal-loading-skeleton.html',
  styleUrl: './portal-loading-skeleton.scss',
})
export class PortalLoadingSkeleton {
  readonly rows = input(3);

  protected readonly rowArray = computed(() => Array.from({ length: this.rows() }));
}
