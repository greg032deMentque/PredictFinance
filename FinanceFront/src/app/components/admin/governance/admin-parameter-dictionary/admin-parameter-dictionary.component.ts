import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface AdminParameterDictionaryItem {
  ParameterId: string;
  CategoryCode: string;
  DisplayLabel: string;
  IsActive: boolean;
  IsPublished: boolean;
}

@Component({
  selector: 'app-admin-parameter-dictionary',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-parameter-dictionary.component.html',
  styleUrl: './admin-parameter-dictionary.component.scss'
})
export class AdminParameterDictionaryComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  items: AdminParameterDictionaryItem[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminParameterDictionaryItem[]>(`${environment.apiUrl}admin/parameter-dictionary`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.items = payload ?? [];
        },
        error: () => {
          this.items = [];
          this.error = 'Impossible de charger le dictionnaire des paramètres.';
        }
      });
  }
}
