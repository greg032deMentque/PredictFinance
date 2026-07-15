export interface LearnTopicAdminItem {
  Id: string;
  TopicId: string;
  Title: string;
  Summary: string;
  RoutePath: string;
  DisplayOrder: number;
  IsPublished: boolean;
}

export interface LearnTopicUpsertRequest {
  topicId: string;
  title: string;
  summary: string;
  routePath: string;
  displayOrder: number;
  isPublished: boolean;
}
