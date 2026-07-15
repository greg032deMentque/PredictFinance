import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [],
  template: `
    <div [class]="'input-group' + (size ? ' input-group-' + size : '')">
      <span class="input-group-text"><i class="bi bi-search"></i></span>
      <input
        type="text"
        class="form-control"
        [placeholder]="placeholder"
        [value]="value"
        autocomplete="off"
        [attr.aria-label]="placeholder"
        (input)="valueChange.emit($any($event.target).value)" />
      @if (value) {
        <button class="btn btn-outline-secondary" type="button" (click)="valueChange.emit('')">
          <i class="bi bi-x-lg"></i>
        </button>
      }
    </div>
  `
})
export class SearchBarComponent {
  @Input() placeholder = '';
  @Input() value = '';
  @Input() size: 'sm' | '' = '';
  @Output() valueChange = new EventEmitter<string>();
}
