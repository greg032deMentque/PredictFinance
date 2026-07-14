export interface LearnTopicAdminItem {
  id: number;
  topicId: string;
  title: string;
  summary: string;
  routePath: string;
  displayOrder: number;
  isPublished: boolean;
}

export interface LearnTopicUpsertRequest {
  topicId: string;
  title: string;
  summary: string;
  routePath: string;
  displayOrder: number;
  isPublished: boolean;
}
