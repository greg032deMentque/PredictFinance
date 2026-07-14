import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';

interface AdminPeaRegistryItem {
  EntryId: string;
  Symbol: string;
  DisplayName: string;
  UniverseId: string;
  EligibilityStatus: number | string;
  SourceType: number | string;
  SourceReference: string;
  CheckedUtc?: string | null;
  PolicyVersion: string;
  ReviewerNote: string;
}

@Component({
  selector: 'app-admin-pea-registry',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './admin-pea-registry.component.html',
  styleUrl: './admin-pea-registry.component.scss'
})
export class AdminPeaRegistryComponent implements OnInit {
  private readonly http = inject(HttpClient);

  items: AdminPeaRegistryItem[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminPeaRegistryItem[]>(`${environment.apiUrl}admin/pea-registry`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.items = payload ?? [];
        },
        error: () => {
          this.items = [];
          this.error = 'Impossible de charger le registre PEA.';
        }
      });
  }
}
