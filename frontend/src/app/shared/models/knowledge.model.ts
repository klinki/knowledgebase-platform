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

export interface CaptureListItem {
  id: string;
  sourceUrl: string;
  contentType: string;
  status: string;
  createdAt: string;
  processedAt: string | null;
  failureReason: string | null;
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
