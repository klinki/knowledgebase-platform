export interface DashboardItem {
  id: string;
  title: string;
  sourceUrl: string;
  capturedAt: string | null;
  status: string | null;
  tags: string[];
  summary: string | null;
  similarity: number | null;
}

export interface TagSummary {
  id: string;
  name: string;
  count: number;
  lastUsedAt: string | null;
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

export interface SemanticSearchResult {
  id: string;
  title: string;
  summary: string;
  sourceUrl: string;
  similarity: number;
  tags: string[];
}

export interface CaptureListItem {
  id: string;
  sourceUrl: string;
  contentType: string;
  status: string;
  createdAt: string;
  processedAt: string | null;
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
}

export interface CaptureDetail {
  id: string;
  sourceUrl: string;
  contentType: string;
  status: string;
  createdAt: string;
  processedAt: string | null;
  rawContent: string;
  metadata: string | null;
  tags: string[];
  processedInsight: CaptureProcessedInsight | null;
}
