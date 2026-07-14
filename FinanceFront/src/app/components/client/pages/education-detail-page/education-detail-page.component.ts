import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { EducationArticleContent } from '../../../../Models/client-finance-models/education-article.model';
import { EducationService } from '../../../../services/education.service';
import { AppRoutes } from '../../../../Routes/app.routes.constants';

/** Conversion markdown basique vers une seule chaine HTML sûre — sans lib externe.
 *  Seuls les blocs structurels sûrs sont produits : titres, paragraphes, listes
 *  (les puces consecutives sont regroupees dans un vrai <ul>), gras, italique, code.
 *  Pas de balises <script>, <img src=...>, ni attributs d'événement.
 *  Le rendu passe ensuite par le sanitizer Angular via [innerHTML].
 */
function markdownToHtml(md: string): string {
  const out: string[] = [];
  let inList = false;
  const closeList = () => {
    if (inList) {
      out.push('</ul>');
      inList = false;
    }
  };

  for (const line of (md ?? '').split('\n')) {
    // Échapper le HTML d'abord
    const safe = line
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    if (/^### /.test(safe)) { closeList(); out.push(`<h4 class="edu-h4">${applyInline(safe.slice(4))}</h4>`); continue; }
    if (/^## /.test(safe)) { closeList(); out.push(`<h3 class="edu-h3">${applyInline(safe.slice(3))}</h3>`); continue; }
    if (/^# /.test(safe)) { closeList(); out.push(`<h2 class="edu-h2">${applyInline(safe.slice(2))}</h2>`); continue; }

    if (/^[*-] /.test(safe)) {
      if (!inList) { out.push('<ul class="edu-list">'); inList = true; }
      out.push(`<li>${applyInline(safe.slice(2))}</li>`);
      continue;
    }

    if (safe.trim() === '') { closeList(); continue; }

    closeList();
    out.push(`<p>${applyInline(safe)}</p>`);
  }

  closeList();
  return out.join('');
}

function applyInline(text: string): string {
  return text
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>')
    .replace(/`(.+?)`/g, '<code>$1</code>');
}

@Component({
  selector: 'app-education-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './education-detail-page.component.html',
  styleUrl: './education-detail-page.component.scss'
})
export class EducationDetailPageComponent implements OnInit {
  private readonly educationService = inject(EducationService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly detail = signal<EducationArticleContent | null>(null);
  protected readonly bodyHtml = signal<string>('');

  protected readonly backPath = ['/', AppRoutes.ClientRoot, AppRoutes.Education];

  ngOnInit(): void {
    const slug = (this.route.snapshot.paramMap.get('slug') ?? '').trim();
    if (!slug) {
      this.error.set('Article introuvable.');
      return;
    }
    this.load(slug);
  }

  protected retry(): void {
    const slug = (this.route.snapshot.paramMap.get('slug') ?? '').trim();
    if (slug) this.load(slug);
  }

  private load(slug: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.educationService
      .getBySlug(slug)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => {
          this.detail.set(data);
          this.bodyHtml.set(markdownToHtml(data.BodyMarkdown ?? ''));
        },
        error: () => this.error.set('Impossible de charger cet article.')
      });
  }
}
