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
