import { WritableSignal } from '@angular/core';
import {
  from,
  ObservableInput,
  ObservedValueOf,
  OperatorFunction,
  finalize,
  switchMap,
} from 'rxjs';

/**
 * Like `switchMap`, but drives a loading signal for the latest projected request only.
 * Unsubscribing a stale inner request does not clear loading once a newer request has begun.
 */
export function switchMapLatestLoading<T, O extends ObservableInput<unknown>>(
  loading: WritableSignal<boolean>,
  project: (value: T, index: number) => O,
): OperatorFunction<T, ObservedValueOf<O>> {
  let generation = 0;

  return switchMap((value, index) => {
    const current = ++generation;
    loading.set(true);

    return from(project(value, index)).pipe(
      finalize(() => {
        if (current === generation) {
          loading.set(false);
        }
      }),
    );
  });
}
