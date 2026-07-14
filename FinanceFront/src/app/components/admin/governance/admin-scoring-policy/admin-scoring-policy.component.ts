import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';

interface AdminScoringPolicy {
  SupportedUniverseId: string;
  ScoringVersion: string;
  EligibilityPolicyVersion: string;
  ProviderId: string;
  AsOfUtcSemantics: string;
  CategoryCodes: string[];
  MetricCodes: string[];
  HigherIsBetterMetricCodes: string[];
  LowerIsBetterMetricCodes: string[];
  MinimumCategoriesRequiredFloor: number;
  MinimumCategoriesRequiredCeiling: number;
  CoveragePenaltySupported: boolean;
}

@Component({
  selector: 'app-admin-scoring-policy',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-scoring-policy.component.html',
  styleUrl: './admin-scoring-policy.component.scss'
})
export class AdminScoringPolicyComponent implements OnInit {
  private readonly http = inject(HttpClient);

  policy: AdminScoringPolicy | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminScoringPolicy>(`${environment.apiUrl}admin/scoring-policy`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.policy = payload;
        },
        error: () => {
          this.policy = null;
          this.error = 'Impossible de charger la policy de scoring.';
        }
      });
  }
}
