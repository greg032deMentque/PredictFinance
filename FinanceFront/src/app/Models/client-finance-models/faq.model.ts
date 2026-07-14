export interface FaqItem {
  category: string;
  question: string;
  answer: string;
  displayOrder: number;
}

export interface FaqAdminItem {
  id: number;
  category: string;
  question: string;
  answer: string;
  displayOrder: number;
  isPublished: boolean;
}

export interface FaqUpsertRequest {
  category: string;
  question: string;
  answer: string;
  displayOrder: number;
  isPublished: boolean;
}
