import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { PredictionResult } from '../../core/models/prediction.model';
import { PredictionService } from '../../core/services/prediction.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, DecimalPipe],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly predictionService = inject(PredictionService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly prediction = signal<PredictionResult | null>(null);

  readonly tickerChoices = ['AAPL', 'MSFT', 'NVDA', 'AMZN', 'GOOGL', 'META', 'JPM', 'XOM'];

  readonly form = this.formBuilder.nonNullable.group({
    symbol: ['AAPL', [Validators.required, Validators.minLength(1)]]
  });

  submitPrediction(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const symbol = this.form.getRawValue().symbol.trim().toUpperCase();
    if (!symbol) {
      this.error.set('Le ticker est obligatoire.');
      return;
    }

    this.loading.set(true);
    this.predictionService
      .predict(symbol)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => this.prediction.set(result),
        error: () =>
          this.error.set(
            "Impossible de recuperer la prediction. Verifie que l'API .NET est demarree et accessible."
          )
      });
  }

  actionClass(action: PredictionResult['suggestedAction']): string {
    if (action === 'buy') {
      return 'text-bg-success';
    }
    if (action === 'sell') {
      return 'text-bg-danger';
    }
    return 'text-bg-warning';
  }

  actionLabel(action: PredictionResult['suggestedAction']): string {
    if (action === 'buy') {
      return 'BUY';
    }
    if (action === 'sell') {
      return 'SELL';
    }
    return 'HOLD';
  }
}
