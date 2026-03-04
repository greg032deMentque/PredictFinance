import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PatternProbability, PredictionResult } from '../Models/prediction.model';

@Injectable({ providedIn: 'root' })
export class PredictionService {
  constructor(private readonly http: HttpClient) {}

  predict(symbol: string): Observable<PredictionResult> {
    const normalizedSymbol = symbol.trim().toUpperCase();

    return this.http
      .get<Record<string, unknown>>(
        `${environment.apiUrl}Trading/predict/${encodeURIComponent(normalizedSymbol)}`
      )
      .pipe(map((payload) => this.normalizePrediction(payload, normalizedSymbol)));
  }

  private normalizePrediction(payload: Record<string, unknown>, fallbackSymbol: string): PredictionResult {
    const symbol = this.readString(payload, ['symbol', 'Symbol', 'ticker']) ?? fallbackSymbol;
    const pattern = this.readString(payload, ['pattern', 'Pattern']) ?? 'DOUBLE_TOP';
    const predictedAt =
      this.readString(payload, ['predictedAt', 'PredictedAt', 'as_of']) ?? new Date().toISOString();

    const lastProbability = this.clampProbability(
      this.readNumber(payload, ['lastProbability', 'LastProbability', 'last_prob']) ?? 0
    );
    const meanProbability = this.clampProbability(
      this.readNumber(payload, ['meanProbability', 'MeanProbability', 'mean_prob']) ?? lastProbability
    );
    const maxProbability = this.clampProbability(
      this.readNumber(payload, ['maxProbability', 'MaxProbability', 'max_prob']) ?? lastProbability
    );

    const probabilityPct = this.readNumber(payload, ['probabilityPct', 'ProbabilityPct']) ?? lastProbability * 100;
    const nWindows = Math.max(0, Math.round(this.readNumber(payload, ['nWindows', 'NWindows', 'n_windows']) ?? 0));

    const suggestedActionRaw =
      this.readString(payload, ['suggestedAction', 'SuggestedAction', 'action', 'Action']) ?? '';
    const suggestedAction = this.normalizeAction(suggestedActionRaw, lastProbability);

    const actionConfidence = this.clampProbability(
      this.readNumber(payload, ['actionConfidence', 'ActionConfidence', 'confidence', 'Confidence']) ??
        this.defaultConfidence(suggestedAction, lastProbability)
    );

    const actionReason =
      this.readString(payload, ['actionReason', 'ActionReason', 'reason', 'Reason']) ??
      `Signal ${pattern} avec une probabilite de ${(lastProbability * 100).toFixed(2)}%`;

    const patterns = this.readPatterns(payload, pattern, lastProbability);

    return new PredictionResult({
      symbol,
      predictedAt,
      pattern,
      lastProbability,
      meanProbability,
      maxProbability,
      probabilityPct,
      suggestedAction,
      actionConfidence,
      actionReason,
      nWindows,
      patterns
    });
  }

  private readPatterns(
    payload: Record<string, unknown>,
    defaultPattern: string,
    defaultProbability: number
  ): PatternProbability[] {
    const rawPatterns = payload['patterns'] ?? payload['Patterns'];
    if (!Array.isArray(rawPatterns)) {
      return [new PatternProbability({ pattern: defaultPattern, probability: defaultProbability })];
    }

    const parsed = rawPatterns
      .map((entry) => {
        if (!entry || typeof entry !== 'object') {
          return null;
        }

        const record = entry as Record<string, unknown>;
        const pattern = this.readString(record, ['pattern', 'Pattern']) ?? defaultPattern;
        const probability = this.clampProbability(
          this.readNumber(record, ['probability', 'Probability']) ?? defaultProbability
        );

        return new PatternProbability({ pattern, probability });
      })
      .filter((item): item is PatternProbability => item !== null);

    return parsed.length > 0
      ? parsed
      : [new PatternProbability({ pattern: defaultPattern, probability: defaultProbability })];
  }

  private readString(source: Record<string, unknown>, keys: string[]): string | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value.trim();
      }
    }

    return null;
  }

  private readNumber(source: Record<string, unknown>, keys: string[]): number | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'number' && Number.isFinite(value)) {
        return value;
      }

      if (typeof value === 'string') {
        const parsed = Number(value);
        if (Number.isFinite(parsed)) {
          return parsed;
        }
      }
    }

    return null;
  }

  private normalizeAction(rawAction: string, probability: number): 'buy' | 'hold' | 'sell' {
    const normalized = rawAction.trim().toLowerCase();
    if (normalized === 'buy' || normalized === 'hold' || normalized === 'sell') {
      return normalized;
    }

    if (probability >= 0.65) {
      return 'sell';
    }

    if (probability <= 0.2) {
      return 'buy';
    }

    return 'hold';
  }

  private defaultConfidence(action: 'buy' | 'hold' | 'sell', probability: number): number {
    if (action === 'sell') {
      return probability;
    }

    if (action === 'buy') {
      return 1 - probability;
    }

    return 1 - Math.abs(probability - 0.5) * 2;
  }

  private clampProbability(value: number): number {
    if (value < 0) {
      return 0;
    }

    if (value > 1) {
      return 1;
    }

    return value;
  }
}
