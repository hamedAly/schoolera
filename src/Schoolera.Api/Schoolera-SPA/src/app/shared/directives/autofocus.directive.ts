import { AfterViewInit, Directive, ElementRef, inject } from '@angular/core';

@Directive({
  selector: '[seAutofocus]',
})
export class AutofocusDirective implements AfterViewInit {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  ngAfterViewInit(): void {
    this.elementRef.nativeElement.focus();
  }
}
