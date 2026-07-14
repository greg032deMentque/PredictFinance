import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface WordingPublicationState {
  IsActive: boolean;
  ActivatedAtUtc?: string | null;
  RecommendationPolicyVersion: string;
  ExplanationPolicyVersion: string;
  AffectedDomains: string[];
}

interface AdminWordingVersionListItem {
  WordingVersionId: string;
  DisplayName: string;
  IsActive: boolean;
  ActivatedAtUtc?: string | null;
  ScenarioCount: number;
  PublicationState: WordingPublicationState;
}

@Component({
  selector: 'app-admin-wording-versions',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './admin-wording-versions.component.html',
  styleUrl: './admin-wording-versions.component.scss'
})
export class AdminWordingVersionsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  items: AdminWordingVersionListItem[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminWordingVersionListItem[]>(`${environment.apiUrl}admin/wording-versions`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.items = payload ?? [];
        },
        error: () => {
          this.items = [];
          this.error = 'Impossible de charger les versions de wording.';
        }
      });
  }
}
