export interface LegalCardItem {
  Icon: string;
  Title: string;
  Description: string;
  UpdatedAt: string | null;
  RouterLink: string | null;
  DisplayOrder: number;
}

export interface LegalCardAdminItem {
  Id: string;
  Key: string;
  Icon: string;
  Title: string;
  Description: string;
  EffectiveDate: string | null;
  TargetRoute: string | null;
  DisplayOrder: number;
  IsPublished: boolean;
}

export interface LegalCardUpsertRequest {
  Key: string;
  Icon: string;
  Title: string;
  Description: string;
  EffectiveDate: string | null;
  TargetRoute: string | null;
  DisplayOrder: number;
  IsPublished: boolean;
}
