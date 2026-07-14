// ── Côté CLIENT (clientfinance/education*) ──────────────────────────────
// L'API sérialise en PascalCase (Program.cs : PropertyNamingPolicy = null).
// Ces interfaces correspondent exactement aux ViewModels client
// (EducationArticleSummaryViewModel / EducationArticleDetailViewModel).
export interface EducationArticleSummary {
  Slug: string;
  ProductType: string;
  Title: string;
  Summary: string;
  DisplayOrder: number;
}

export interface EducationArticleContent {
  Slug: string;
  ProductType: string;
  Title: string;
  Summary: string;
  BodyMarkdown: string;
}

// ── Côté ADMIN (admin/education*) — PascalCase, EducationArticleAdminViewModel.
// Une seule shape (liste et détail renvoient le même ViewModel admin).
export interface EducationArticleAdmin {
  Id: string;
  Slug: string;
  ProductType: string;
  Title: string;
  Summary: string;
  BodyMarkdown: string;
  DisplayOrder: number;
  IsPublished: boolean;
}

export interface EducationUpsertRequest {
  Slug: string;
  ProductType: string;
  Title: string;
  Summary: string;
  BodyMarkdown: string;
  DisplayOrder: number;
  IsPublished: boolean;
}
