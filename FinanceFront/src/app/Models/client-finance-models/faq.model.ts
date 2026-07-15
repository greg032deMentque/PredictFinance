export interface FaqItem {
  Category: string;
  Question: string;
  Answer: string;
  DisplayOrder: number;
}

export interface FaqAdminItem {
  Id: string;
  Category: string;
  Question: string;
  Answer: string;
  DisplayOrder: number;
  IsPublished: boolean;
}

export interface FaqUpsertRequest {
  Category: string;
  Question: string;
  Answer: string;
  DisplayOrder: number;
  IsPublished: boolean;
}
