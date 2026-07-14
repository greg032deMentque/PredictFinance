import { Component, EventEmitter, Output, signal } from '@angular/core';

@Component({
  selector: 'app-legal-investment-warning',
  standalone: true,
  imports: [],
  templateUrl: './legal-investment-warning.html',
  styleUrl: './legal-investment-warning.scss'
})
export class LegalInvestmentWarningComponent {
  @Output() readonly confirmed = new EventEmitter<void>();
  @Output() readonly dismissed = new EventEmitter<void>();

  protected readonly acknowledged = signal(false);

  protected confirm(): void {
    this.acknowledged.set(true);
    this.confirmed.emit();
  }

  protected dismiss(): void {
    this.dismissed.emit();
  }
}
