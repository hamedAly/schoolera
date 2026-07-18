import { Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { ToastService } from './toast.service';

@Component({
  selector: 'se-toast-host',
  imports: [TranslocoPipe],
  templateUrl: './toast-host.html',
  styleUrl: './toast-host.scss',
})
export class ToastHost {
  protected readonly toast = inject(ToastService);

  protected dismiss(id: number): void {
    this.toast.dismiss(id);
  }
}
