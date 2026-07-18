import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'emptyValue',
})
export class EmptyValuePipe implements PipeTransform {
  transform(value: string | number | null | undefined): string | number {
    return value ?? '-';
  }
}
