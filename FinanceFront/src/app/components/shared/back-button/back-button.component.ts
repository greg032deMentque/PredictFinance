import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-back-button',
  standalone: true,
  imports: [],
  template: `
    <button
      class="btn btn-outline-secondary btn-sm rounded-pill d-inline-flex align-items-center gap-1"
      type="button"
      [disabled]="disabled"
      (click)="back.emit()">
      <i class="bi bi-arrow-left"></i>
      {{ label }}
    </button>
  `
})
export class BackButtonComponent {
  @Input() label = 'Retour';
  @Input() disabled = false;
  @Output() back = new EventEmitter<void>();
}
