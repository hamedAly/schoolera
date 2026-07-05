import { computed, inject, Injectable, signal } from '@angular/core';

import { SchoolsApi } from './schools.api';
import { SchoolListItem } from './schools.models';

interface SchoolsState {
  schools: ReadonlyArray<SchoolListItem>;
  loading: boolean;
  errors: string[];
}

@Injectable({
  providedIn: 'root',
})
export class SchoolsStore {
  private readonly schoolsApi = inject(SchoolsApi);
  private readonly state = signal<SchoolsState>({
    schools: [],
    loading: false,
    errors: [],
  });

  readonly schools = computed(() => this.state().schools);
  readonly loading = computed(() => this.state().loading);
  readonly errors = computed(() => this.state().errors);

  load(): void {
    this.patch({ loading: true, errors: [] });

    this.schoolsApi.getSchools().subscribe({
      next: (result) => {
        this.patch({
          schools: result.succeeded ? result.data ?? [] : [],
          loading: false,
          errors: result.succeeded ? [] : result.errors,
        });
      },
      error: () => {
        this.patch({
          loading: false,
          errors: ['Unable to load schools.'],
        });
      },
    });
  }

  private patch(partial: Partial<SchoolsState>): void {
    this.state.update((current) => ({ ...current, ...partial }));
  }
}
