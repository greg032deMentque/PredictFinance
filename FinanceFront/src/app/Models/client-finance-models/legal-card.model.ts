export interface LegalCardItem {
  icon: string;
  title: string;
  description: string;
  updatedAt: string | null;
  routerLink: string | null;
  displayOrder: number;
}

export interface LegalCardAdminItem {
  id: number;
  key: string;
  icon: string;
  title: string;
  description: string;
  effectiveDate: string | null;
  targetRoute: string | null;
  displayOrder: number;
  isPublished: boolean;
}

export interface LegalCardUpsertRequest {
  key: string;
  icon: string;
  title: string;
  description: string;
  effectiveDate: string | null;
  targetRoute: string | null;
  displayOrder: number;
  isPublished: boolean;
}
