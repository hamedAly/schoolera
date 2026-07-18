import { Component, input } from '@angular/core';

export interface DataTableColumn {
  key: string;
  label: string;
}

@Component({
  selector: 'se-data-table',
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
})
export class DataTable {
  readonly columns = input.required<ReadonlyArray<DataTableColumn>>();
  readonly rows = input<ReadonlyArray<Record<string, unknown>>>([]);
}
