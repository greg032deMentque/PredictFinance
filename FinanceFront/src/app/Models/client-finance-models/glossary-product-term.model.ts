// Côté CLIENT (clientfinance/glossary-terms) — PascalCase, correspond à
// GlossaryProductTermViewModel { Term, Definition, Category }.
export interface GlossaryTerm {
  Term: string;
  Definition: string;
  Category: string;
}

// Côté ADMIN (admin/glossary-terms) — PascalCase, GlossaryTermAdminViewModel.
export interface GlossaryTermAdmin {
  Id: string;
  Term: string;
  Definition: string;
  Category: string;
  IsPublished: boolean;
}

export interface GlossaryTermUpsertRequest {
  Term: string;
  Definition: string;
  Category: string;
  IsPublished: boolean;
}
