import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';

interface AdminDataQuality {
  AssetsMissingProfileSyncCount: number;
  AssetsWithoutPeaRegistryCount: number;
  PeaRegistryUnknownStatusCount: number;
  CompletedAnalysisRunsWithoutModelSnapshotCount: number;
  CompletedAnalysisRunsWithoutDecisionSignalCount: number;
  IssueSummaries: string[];
}

@Component({
  selector: 'app-admin-data-quality',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-data-quality.component.html',
  styleUrl: './admin-data-quality.component.scss'
})
export class AdminDataQualityComponent implements OnInit {
  private readonly http = inject(HttpClient);

  payload: AdminDataQuality | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminDataQuality>(`${environment.apiUrl}admin/data-quality`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.payload = payload;
        },
        error: () => {
          this.payload = null;
          this.error = 'Impossible de charger la qualité des données.';
        }
      });
  }
}
