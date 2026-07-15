import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { GlossaryTerm } from '../../../../Models/client-finance-models/glossary-product-term.model';
import { GlossaryTermsService } from '../../../../services/glossary-terms.service';
import { SearchBarComponent } from '../../../shared/search-bar/search-bar.component';

@Component({
  selector: 'app-glossary-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SearchBarComponent],
  templateUrl: './glossary-page.component.html',
  styleUrl: './glossary-page.component.scss'
})
export class GlossaryPageComponent implements OnInit {
  private readonly glossaryTermsService = inject(GlossaryTermsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly terms = signal<GlossaryTerm[]>([]);

  protected readonly searchControl = this.fb.nonNullable.control('');

  ngOnInit(): void {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((query) => this.load(query));

    this.load('');
  }

  private load(query: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.glossaryTermsService
      .search(query)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.terms.set(data),
        error: () => this.error.set('Impossible de charger le glossaire.')
      });
  }
}
