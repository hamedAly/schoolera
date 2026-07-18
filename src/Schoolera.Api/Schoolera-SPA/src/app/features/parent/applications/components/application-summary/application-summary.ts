import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'se-application-summary',
  imports: [TranslocoPipe],
  templateUrl: './application-summary.html',
  styleUrl: './application-summary.scss',
})
export class ApplicationSummary {
  readonly applicationNumber = input<string | null | undefined>();
  readonly childName = input<string | null | undefined>();
  readonly schoolName = input<string | null | undefined>();
  readonly branchName = input<string | null | undefined>();
  readonly stageName = input<string | null | undefined>();
  readonly gradeName = input<string | null | undefined>();
  readonly academicYearName = input<string | null | undefined>();
  readonly parentNotes = input<string | null | undefined>();
  readonly attachmentCount = input(0);
}
