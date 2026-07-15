export interface PatternDefinitionAdminItem {
  PatternId: string;
  DisplayName: string;
  Family: string;
  FamilyLabel: string;
  Direction: string;
  DirectionLabel: string;
  Description: string;
  AnalysisNarrative: string;
  Reliability: number;
  ReliabilityLabel: string;
}

export interface PatternDefinitionUpdateRequest {
  displayName: string;
  family: string;
  familyLabel: string;
  direction: string;
  directionLabel: string;
  description: string;
  analysisNarrative: string;
  reliability: number;
  reliabilityLabel: string;
}

export interface AnalysisConceptAdminItem {
  Code: string;
  Label: string;
  Explanation: string;
}

export interface AnalysisConceptCreateRequest {
  code: string;
  label: string;
  explanation: string;
}

export interface AnalysisConceptUpdateRequest {
  label: string;
  explanation: string;
}
