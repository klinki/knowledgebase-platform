export interface DashboardItem {
  id: string;
  title: string;
  sourceUrl: string;
  capturedAt: string | null;
  status: string | null;
  tags: string[];
  labels: LabelAssignment[];
  summary: string | null;
  similarity: number | null;
}

export interface LabelAssignment {
  category: string;
  value: string;
}

export interface TopicClusterLink {
  id: string;
  title: string;
  description: string | null;
  suggestedLabel: LabelAssignment;
}

export interface TopicClusterRepresentativeInsight {
  captureId: string;
  processedInsightId: string;
  title: string;
  summary: string;
  sourceUrl: string;
}

export interface TopicClusterSummary {
  id: string;
  title: string;
  description: string | null;
  keywords: string[];
  memberCount: number;
  updatedAt: string;
  representativeInsights: TopicClusterRepresentativeInsight[];
  suggestedLabel: LabelAssignment;
}

export interface TopicClusterMember {
  captureId: string;
  processedInsightId: string;
  title: string;
  summary: string;
  sourceUrl: string;
  rank: number;
  similarityToCentroid: number;
  tags: string[];
  labels: LabelAssignment[];
}

export interface TopicClusterDetail {
  id: string;
  title: string;
  description: string | null;
  keywords: string[];
  memberCount: number;
  updatedAt: string;
  suggestedLabel: LabelAssignment;
  members: TopicClusterMember[];
}

export interface TagSummary {
  id: string;
  name: string;
  count: number;
  lastUsedAt: string | null;
}

export interface LabelValueSummary {
  id: string;
  value: string;
  count: number;
  lastUsedAt: string | null;
}

export interface LabelCategorySummary {
  id: string;
  name: string;
  count: number;
  lastUsedAt: string | null;
  values: LabelValueSummary[];
}

export interface LabelSearchResult {
  id: string;
  title: string;
  summary: string | null;
  sourceUrl: string;
  processedAt: string | null;
  tags: string[];
  labels: LabelAssignment[];
}

export interface DashboardStats {
  totalCaptures: number;
  activeTags: number;
}

export interface DashboardOverview {
  recentCaptures: DashboardItem[];
  topTags: TagSummary[];
  topicClusters: TopicClusterSummary[];
  stats: DashboardStats;
}

export interface CaptureProcessingCaptureCounts {
  pending: number;
  processing: number;
  completed: number;
  failed: number;
}

export interface CaptureProcessingJobCounts {
  enqueued: number;
  scheduled: number;
  processing: number;
  failed: number;
}

export interface CaptureProcessingAdminOverview {
  isPaused: boolean;
  changedAt: string | null;
  changedByDisplayName: string | null;
  captureCounts: CaptureProcessingCaptureCounts;
  jobCounts: CaptureProcessingJobCounts;
  recentCaptures: DashboardItem[];
}

export interface SemanticSearchResult {
  id: string;
  title: string;
  summary: string;
  sourceUrl: string;
  similarity: number;
  tags: string[];
  labels: LabelAssignment[];
}

export interface SearchResult {
  id: string;
  title: string;
  summary: string | null;
  sourceUrl: string;
  processedAt: string | null;
  tags: string[];
  labels: LabelAssignment[];
  similarity: number | null;
}

export interface CaptureListItem {
  id: string;
  sourceUrl: string;
  contentType: string;
  status: string;
  createdAt: string;
  processedAt: string | null;
  failureReason: string | null;
}

export interface CaptureListPage {
  items: CaptureListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CaptureProcessedInsight {
  id: string;
  title: string;
  summary: string;
  keyInsights: string | null;
  actionItems: string | null;
  sourceTitle: string | null;
  author: string | null;
  processedAt: string;
  tags: string[];
  labels: LabelAssignment[];
  cluster: TopicClusterLink | null;
}

export interface CaptureDetail {
  id: string;
  sourceUrl: string;
  contentType: string;
  status: string;
  createdAt: string;
  processedAt: string | null;
  failureReason: string | null;
  rawContent: string;
  metadata: string | null;
  tags: string[];
  labels: LabelAssignment[];
  processedInsight: CaptureProcessedInsight | null;
}

export interface CaptureCreateRequest {
  sourceUrl: string;
  contentType: string;
  rawContent: string;
  tags: string[];
  labels?: LabelAssignment[];
}

export interface CaptureAccepted {
  id: string;
  message: string;
}
