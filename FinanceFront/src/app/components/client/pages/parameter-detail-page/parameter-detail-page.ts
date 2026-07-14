import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ParameterDetail } from '../../../../Models/client-finance-models/learn-parameter-models';
import { UserPaths } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-parameter-detail-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './parameter-detail-page.html',
  styleUrl: './parameter-detail-page.scss'
})
export class ParameterDetailPageComponent implements OnInit {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly detail = signal<ParameterDetail | null>(null);
  protected readonly loading = signal(false);
  protected readonly missingContext = signal(false);
  protected readonly historyPath = '/' + UserPaths.History;

  ngOnInit(): void {
    const parameterId = this.route.snapshot.paramMap.get('parameterId');
    const analysisId = this.route.snapshot.queryParamMap.get('from');

    if (!analysisId) {
      this.missingContext.set(true);
      return;
    }

    if (!parameterId) return;

    this.loadDetail(analysisId, parameterId);
  }

  private loadDetail(analysisId: string, parameterId: string): void {
    this.loading.set(true);
    this.clientFinanceService
      .getParameterDetail(analysisId, parameterId)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.detail.set(data),
        error: () => this.toastService.error('Impossible de charger le détail du paramètre.')
      });
  }
}
