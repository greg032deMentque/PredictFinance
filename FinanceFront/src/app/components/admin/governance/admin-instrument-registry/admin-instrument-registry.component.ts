import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';

interface AdminInstrumentRegistryItem {
  AssetId: string;
  Symbol: string;
  ProviderSymbol: string;
  DisplayName: string;
  Exchange: string;
  Currency: string;
  AssetType: string;
  Country?: string | null;
  LastProfileSyncUtc?: string | null;
  ActiveUniverseIds: string[];
  HasConfirmedPeaEligibility: boolean;
  HasAnalysisHistory: boolean;
}

@Component({
  selector: 'app-admin-instrument-registry',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './admin-instrument-registry.component.html',
  styleUrl: './admin-instrument-registry.component.scss'
})
export class AdminInstrumentRegistryComponent implements OnInit {
  private readonly http = inject(HttpClient);

  items: AdminInstrumentRegistryItem[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminInstrumentRegistryItem[]>(`${environment.apiUrl}admin/instruments`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.items = payload ?? [];
        },
        error: () => {
          this.items = [];
          this.error = 'Impossible de charger le registre des instruments.';
        }
      });
  }
}
