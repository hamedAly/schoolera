import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'success' | 'error' | 'info' | 'warning';

export interface ToastRequest {
  /** Already-translated message (callers translate via Transloco). */
  message: string;
  variant?: ToastVariant;
  /** Auto-dismiss delay in ms. Use 0 to keep until dismissed. */
  durationMs?: number;
}

export interface ToastItem extends Required<Pick<ToastRequest, 'message' | 'variant' | 'durationMs'>> {
  id: number;
}

const DEFAULT_DURATION_MS = 5000;

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private nextId = 1;
  private readonly itemsSignal = signal<ToastItem[]>([]);
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  readonly items = this.itemsSignal.asReadonly();

  show(request: ToastRequest): number {
    const id = this.nextId++;
    const item: ToastItem = {
      id,
      message: request.message.trim(),
      variant: request.variant ?? 'info',
      durationMs: request.durationMs ?? DEFAULT_DURATION_MS,
    };

    if (!item.message) {
      return id;
    }

    this.itemsSignal.update((items) => [...items, item]);

    if (item.durationMs > 0) {
      const timer = setTimeout(() => this.dismiss(id), item.durationMs);
      this.timers.set(id, timer);
    }

    return id;
  }

  success(message: string, durationMs?: number): number {
    return this.show({ message, variant: 'success', durationMs });
  }

  error(message: string, durationMs?: number): number {
    return this.show({ message, variant: 'error', durationMs });
  }

  info(message: string, durationMs?: number): number {
    return this.show({ message, variant: 'info', durationMs });
  }

  warning(message: string, durationMs?: number): number {
    return this.show({ message, variant: 'warning', durationMs });
  }

  dismiss(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }

    this.itemsSignal.update((items) => items.filter((item) => item.id !== id));
  }

  clear(): void {
    for (const timer of this.timers.values()) {
      clearTimeout(timer);
    }

    this.timers.clear();
    this.itemsSignal.set([]);
  }
}
