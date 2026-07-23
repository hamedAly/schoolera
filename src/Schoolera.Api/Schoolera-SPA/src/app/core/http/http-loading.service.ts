import { Injectable, computed, signal } from '@angular/core';

/**
 * Lightweight in-flight HTTP request counter for generic loading UI.
 * Uses ref-counting so a canceled stale request cannot clear loading while a newer request is active.
 */
@Injectable({
  providedIn: 'root',
})
export class HttpLoadingService {
  private readonly activeCount = signal(0);

  /** True while one or more tracked HTTP requests are in flight. */
  readonly isLoading = computed(() => this.activeCount() > 0);

  /** Current in-flight count (for tests / diagnostics). */
  readonly activeRequests = this.activeCount.asReadonly();

  begin(): void {
    this.activeCount.update((count) => count + 1);
  }

  end(): void {
    this.activeCount.update((count) => Math.max(0, count - 1));
  }

  /** Test helper — resets the counter without affecting in-flight subscriptions. */
  reset(): void {
    this.activeCount.set(0);
  }
}
