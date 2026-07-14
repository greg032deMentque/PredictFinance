import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface AdminParameterDictionaryDetail {
  ParameterId: string;
  CategoryCode: string;
  DisplayLabel: string;
  RoleInCategory: string;
  SimpleDefinition: string;
  HowToRead: string;
  WhyItMatters: string;
  LimitsOfInterpretation: string;
  WhatItSupports: string;
  WhatItDoesNotProve: string;
  ImplicationWithoutPosition: string;
  ImplicationWithPosition: string;
  IsActive: boolean;
  IsPublished: boolean;
}

@Component({
  selector: 'app-admin-parameter-dictionary-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-parameter-dictionary-detail.component.html',
  styleUrl: './admin-parameter-dictionary-detail.component.scss'
})
export class AdminParameterDictionaryDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  detail: AdminParameterDictionaryDetail | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const parameterId = (this.route.snapshot.paramMap.get('parameterId') ?? '').trim();
    if (!parameterId) {
      this.error = 'Identifiant de paramètre manquant.';
      return;
    }

    this.load(parameterId);
  }

  private load(parameterId: string): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminParameterDictionaryDetail>(`${environment.apiUrl}admin/parameter-dictionary/${encodeURIComponent(parameterId)}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.detail = payload;
        },
        error: () => {
          this.detail = null;
          this.error = 'Impossible de charger le détail du paramètre.';
        }
      });
  }
}
