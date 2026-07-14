import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { GlossaryTermDirective } from '../../../../core/directives/glossary-term.directive';
import { ClientAnalysisDetail } from '../../../../Models/client-finance-models/client-finance-models';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({ selector: 'app-analysis-detail-page', standalone: true, imports: [CommonModule, RouterLink, DatePipe, GlossaryTermDirective], templateUrl: './analysis-detail-page.component.html', styleUrl: './analysis-detail-page.component.scss' })
export class AnalysisDetailPageComponent {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  analysis: ClientAnalysisDetail | null = null;
  loading = false;
  constructor() { const analysisId = this.route.snapshot.paramMap.get('analysisId'); if (analysisId) this.loadAnalysis(analysisId); }
  private loadAnalysis(analysisId: string): void {
    this.loading = true;
    this.clientFinanceService.getAnalysisDetail(analysisId).pipe(finalize(() => (this.loading = false))).subscribe({ next: (payload) => (this.analysis = payload), error: () => { this.analysis = null; this.toastService.error('Lecture du détail d’analyse impossible.'); } });
  }
}
