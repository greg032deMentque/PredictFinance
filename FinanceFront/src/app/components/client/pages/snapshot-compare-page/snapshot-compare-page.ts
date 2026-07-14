import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { SnapshotComparison } from '../../../../Models/client-finance-models/learn-parameter-models';
import { UserPaths } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-snapshot-compare-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './snapshot-compare-page.html',
  styleUrl: './snapshot-compare-page.scss'
})
export class SnapshotComparePageComponent implements OnInit {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly comparison = signal<SnapshotComparison | null>(null);
  protected readonly loading = signal(false);
  protected readonly missingParams = signal(false);
  protected readonly historyPath = '/' + UserPaths.History;

  ngOnInit(): void {
    const left = this.route.snapshot.queryParamMap.get('left');
    const right = this.route.snapshot.queryParamMap.get('right');

    if (!left || !right) {
      this.missingParams.set(true);
      return;
    }

    this.loadComparison(left, right);
  }

  private loadComparison(left: string, right: string): void {
    this.loading.set(true);
    this.clientFinanceService
      .compareSnapshots(left, right)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.comparison.set(data),
        error: () => this.toastService.error('Impossible de comparer ces deux analyses.')
      });
  }
}
